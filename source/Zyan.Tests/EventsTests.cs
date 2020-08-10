using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Delegates;
using Zyan.Communication.Protocols.Null;
using Zyan.Communication.SessionMgmt;

namespace Zyan.Tests
{
	#region Unit testing platform abstraction layer
#if NUNIT
	using NUnit.Framework;
	using TestClass = NUnit.Framework.TestFixtureAttribute;
	using TestMethod = NUnit.Framework.TestAttribute;
	using ClassInitializeNonStatic = NUnit.Framework.TestFixtureSetUpAttribute;
	using ClassInitialize = DummyAttribute;
	using ClassCleanupNonStatic = NUnit.Framework.TestFixtureTearDownAttribute;
	using ClassCleanup = DummyAttribute;
	using TestContext = System.Object;
	using AssertFailedException = NUnit.Framework.AssertionException;
	using MyIgnoreAttribute = DummyAttribute;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
	using MyIgnoreAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for events on singleton and single-call components.
	///</summary>
	[TestClass]
	public class EventsTests
	{
		#region Interfaces and components

		public delegate void CustomEventType(int firstArgument, string secondArgument);

		/// <summary>
		/// Sample server interface
		/// </summary>
		public interface ISampleServer
		{
			event EventHandler TestEvent;

			void RaiseTestEvent(EventArgs args = null);

			event EventHandler StaticEvent;

			void RaiseStaticEvent(EventArgs args = null);
		}

		/// <summary>
		/// Sample server implementation
		/// </summary>
		/// <typeparam name="T">Dummy type parameter allows separating static fields.</typeparam>
		public class SampleServer<T> : ISampleServer
		{
			public event EventHandler TestEvent;

			public void RaiseTestEvent(EventArgs args)
			{
				if (TestEvent != null)
				{
					TestEvent(null, args);
				}
			}

			public event EventHandler StaticEvent
			{
 				add { eventStorage.AddHandler(value); }
				remove { eventStorage.RemoveHandler(value); }
			}

			private static StaticEventStorage<EventHandler> eventStorage = new StaticEventStorage<EventHandler>();

			public void RaiseStaticEvent(EventArgs args)
			{
				var invoke = eventStorage.Invoke;
				invoke(null, args);
			}
		}

		/// <summary>
		/// Helper class that protects against several subsequent EventStub.WireTo(object) calls.
		/// </summary>
		/// <typeparam name="TDelegate">The type of the delegate.</typeparam>
		private class StaticEventStorage<TDelegate>
		{
			private object padlock = new object();

			private bool eventStubAttached;

			private TDelegate eventStorage = EmptyDelegateFactory.CreateEmptyDelegate<TDelegate>();

			public TDelegate Invoke { get { return eventStorage; } }

			public void AddHandler(TDelegate eventHandler)
			{
				lock (padlock)
				{
					var handler = (Delegate)(object)eventHandler;
					if (handler.Target is EventStub.IDelegateHolder)
					{
						if (eventStubAttached)
						{
							return;
						}

						eventStubAttached = true;
					}

					eventStorage = (TDelegate)(object)Delegate.Combine((Delegate)(object)eventStorage, handler);
				}
			}

			public void RemoveHandler(TDelegate eventHandler)
			{
				lock (padlock)
				{
					var handler = (Delegate)(object)eventHandler;
					if (handler.Target is EventStub.IDelegateHolder)
					{
						return;
					}

					eventStorage = (TDelegate)(object)Delegate.Remove((Delegate)(object)eventStorage, handler);
				}
			}
		}

		#endregion

		#region Initialization and cleanup

		public TestContext TestContext { get; set; }

		static ZyanComponentHost ZyanHost { get; set; }

		static ZyanConnection ZyanConnection { get; set; }

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			StartServer(null);
		}

		[ClassCleanupNonStatic]
		public void Cleanup()
		{
		}

		[ClassInitialize]
		public static void StartServer(TestContext ctx)
		{
			ZyanHost = CreateZyanHost(2345, "EventsServer");
			ZyanConnection = CreateZyanConnection(2345, "EventsServer");
		}

		private static ISessionManager DummySessionManager { get; } = new InProcSessionManager();

		private static ZyanComponentHost CreateZyanHost(int port, string name)
		{
			ZyanSettings.LegacyBlockingEvents = true;
			ZyanSettings.LegacyBlockingSubscriptions = true;
			ZyanSettings.LegacyUnprotectedEventHandlers = true;

			var serverSetup = new NullServerProtocolSetup(port);
			var zyanHost = new ZyanComponentHost(name, serverSetup); //, DummySessionManager);
			zyanHost.RegisterComponent<ISampleServer, SampleServer<int>>("Singleton", ActivationType.Singleton);
			zyanHost.RegisterComponent<ISampleServer, SampleServer<short>>("Singleton2", ActivationType.Singleton);
			zyanHost.RegisterComponent<ISampleServer, SampleServer<long>>("Singleton3", ActivationType.Singleton);
			zyanHost.RegisterComponent<ISampleServer, SampleServer<byte>>("SingleCall", ActivationType.SingleCall);
			zyanHost.RegisterComponent<ISampleServer, SampleServer<char>>("SingletonExternal", new SampleServer<char>());
			return zyanHost;
		}

		private static ZyanConnection CreateZyanConnection(int port, string name, TimeSpan? debounceInterval = null)
		{
			ZyanSettings.ReconnectRemoteEventsDebounceInterval = debounceInterval ?? TimeSpan.Zero;
			return new ZyanConnection("null://NullChannel:" + port + "/" + name, true);
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		#endregion

		[TestMethod]
		public void ZyanHostSubscriptionRelatedEventsAreRaised()
		{
			Trace.WriteLine("ZyanHostSubscriptionRelatedEventsAreRaised");
			ZyanSettings.LegacyBlockingEvents = true;

			// set up server-side event handlers
			var subscriptionAdded = false;
			ZyanHost.SubscriptionAdded += (s, e) => subscriptionAdded = true;

			var subscriptionRemoved = false;
			ZyanHost.SubscriptionRemoved += (s, e) => subscriptionRemoved = true;

			var subscriptionCanceled = false;
			var clientSideException = default(Exception);
			var canceledHandler = new EventHandler<SubscriptionEventArgs>((s, e) =>
			{
				subscriptionCanceled = true;
				clientSideException = e.Exception;
			});
			ZyanHost.SubscriptionCanceled += canceledHandler;

			// set up client event handler
			var handled = false;
			var message = "Secret message";
			var eventHandler = new EventHandler((s, e) =>
			{
				if (handled)
				{
					handled = false;
					throw new Exception(message);
				}

				handled = true;
			});

			// create proxy, attach event handler
			var proxy = ZyanConnection.CreateProxy<ISampleServer>("Singleton");
			proxy.TestEvent += eventHandler;
			Assert.IsTrue(subscriptionAdded);

			// raise the event
			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			// detach event handler
			proxy.TestEvent -= eventHandler;
			Assert.IsTrue(subscriptionRemoved);

			// reattach event handler, raise an event, catch the exception and unsubscribe automatically
			proxy.TestEvent += eventHandler;
			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);
			Assert.IsTrue(subscriptionCanceled);
			Assert.IsNotNull(clientSideException);
			Assert.AreEqual(message, clientSideException.Message);

			// detach event handler
			ZyanHost.SubscriptionCanceled -= canceledHandler;
		}

		[TestMethod]
		public void ExceptionInEventHandlerCancelsTheSubscription()
		{
			ZyanSettings.LegacyBlockingEvents = true;
			ZyanSettings.LegacyBlockingSubscriptions = true;
			ZyanSettings.LegacyUnprotectedEventHandlers = true;

			var host = CreateZyanHost(5432, "ThirdServer");
			var conn = CreateZyanConnection(5432, "ThirdServer", TimeSpan.FromSeconds(1));
			var proxy = conn.CreateProxy<ISampleServer>("Singleton2");

			var handled = false;
			var eventHandler = new EventHandler((s, e) =>
			{
				handled = true;
				throw new Exception();
			});

			proxy.TestEvent += eventHandler;

			var subscriptionCanceled = false;
			var clientSideException = default(Exception);
			host.SubscriptionCanceled += (s, e) =>
			{
				subscriptionCanceled = true;
				clientSideException = e.Exception;
			};

			var subscriptionsRestored = false;
			var restoredHandler = new EventHandler((s, e) => subscriptionsRestored = true);
			host.SubscriptionsRestored += restoredHandler;

			// raise an event, catch exception and unsubscribe automatically
			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);
			Assert.IsTrue(subscriptionCanceled);
			Assert.IsFalse(subscriptionsRestored);

			// raise an event and check if it's ignored because reconnection isn't called yet
			// due to the large debounce interval used in ZyanConnection
			handled = false;
			subscriptionsRestored = false;
			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);
			Assert.IsFalse(subscriptionsRestored);

			// Note: dispose connection before the host
			// so that it can unsubscribe from all remove events
			conn.Dispose();
			host.Dispose();
		}

		// The subscription gets restored on the next remote call to RaiseTestEvent:
		// local subscription checksum doesn't match remote checksum => re-subscribe => ok!
		[TestMethod]
		public void ExceptionInEventHandlerCancelsSubscriptionButTheConnetionResubscribesOnTheNextRemoteCall()
		{
			ZyanSettings.LegacyBlockingEvents = true;
			ZyanSettings.LegacyBlockingSubscriptions = true;
			ZyanSettings.LegacyUnprotectedEventHandlers = true;

			var handled = false;
			var eventHandler = new EventHandler((s, e) =>
			{
				handled = true;
				throw new Exception();
			});

			var subscriptionCanceled = false;
			var clientSideException = default(Exception);
			var canceledHandler = new EventHandler<SubscriptionEventArgs>((s, e) =>
			{
				subscriptionCanceled = true;
				clientSideException = e.Exception;
			});
			ZyanHost.SubscriptionCanceled += canceledHandler;

			var subscriptionsRestored = false;
			var restoredHandler = new EventHandler((s, e) => subscriptionsRestored = true);
			ZyanHost.SubscriptionsRestored += restoredHandler;

			var proxy = ZyanConnection.CreateProxy<ISampleServer>("Singleton2");
			proxy.TestEvent += eventHandler;

			// raise an event, catch exception and unsubscribe automatically
			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);
			Assert.IsTrue(subscriptionCanceled);
			Assert.IsTrue(subscriptionsRestored);

			// There is no need to wait because the client re-subscribes synchronously
			handled = false;
			subscriptionsRestored = false;
			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);
			Assert.IsTrue(subscriptionsRestored);

			// detach event handler
			ZyanHost.SubscriptionCanceled -= canceledHandler;
			ZyanHost.SubscriptionsRestored -= restoredHandler;
		}

		[TestMethod]
		public void ZyanConnectionResubscribesAfterServerRestart()
		{
			ZyanSettings.LegacyBlockingEvents = true;
			ZyanSettings.LegacyBlockingSubscriptions = true;
			ZyanSettings.LegacyUnprotectedEventHandlers = true;

			var host = CreateZyanHost(5432, "SecondServer");
			var conn = CreateZyanConnection(5432, "SecondServer");
			var proxy = conn.CreateProxy<ISampleServer>("Singleton2");

			var handled = false;
			var eventHandler = new EventHandler((s, e) => handled = true);
			proxy.TestEvent += eventHandler;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			// re-create host from scratch (discard all server-side subscriptions)
			host.Dispose();
			host = CreateZyanHost(5432, "SecondServer");
			var subscriptionsRestored = false;
			host.SubscriptionsRestored += (s, e) => subscriptionsRestored = true;

			// the first call: event handlers are restored on reconnect
			handled = false;
			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);
			Assert.IsTrue(subscriptionsRestored);

			// the second call: event handlers are not restored again
			subscriptionsRestored = false;
			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);
			Assert.IsFalse(subscriptionsRestored);

			// Note: dispose connection before the host
			// so that it can unsubscribe from all remove events
			conn.Dispose();
			host.Dispose();
		}

		[TestMethod]
		public void SubscriptionUnsubscription_SingletonComponent()
		{
			var handled = false;
			var eventHandler = new EventHandler((s, e) => handled = true);

			var proxy = ZyanConnection.CreateProxy<ISampleServer>("Singleton3");
			proxy.TestEvent += eventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= eventHandler; // unsubscription #1
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);

			proxy.TestEvent += eventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= eventHandler; // unsubscription #2
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void SubscriptionUnsubscription_SingletonExternal()
		{
			var handled = false;
			var eventHandler = new EventHandler((s, e) => handled = true);

			var proxy = ZyanConnection.CreateProxy<ISampleServer>("SingletonExternal");
			proxy.TestEvent += eventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= eventHandler; // unsubscription #1
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);

			proxy.TestEvent += eventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= eventHandler; // unsubscription #2
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void SubscriptionUnsubscription_SingleCallComponent()
		{
			var handled = false;
			var eventHandler = new EventHandler((s, e) => handled = true);

			var proxy = ZyanConnection.CreateProxy<ISampleServer>("SingleCall");
			proxy.TestEvent += eventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= eventHandler; // unsubscription #1
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);

			proxy.TestEvent += eventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= eventHandler; // unsubscription #2
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void EventsOnSingletonComponentsWorkGlobally()
		{
			var nullProtocol = new NullClientProtocolSetup();

			// start two new sessions
			using (var conn2 = new ZyanConnection(ZyanConnection.ServerUrl, nullProtocol))
			using (var conn3 = new ZyanConnection(ZyanConnection.ServerUrl, nullProtocol))
			{
				var proxy1 = ZyanConnection.CreateProxy<ISampleServer>("Singleton");
				var proxy2 = conn2.CreateProxy<ISampleServer>("Singleton");
				var proxy3 = conn3.CreateProxy<ISampleServer>("Singleton");

				var proxy1handled = false;
				var handler1 = new EventHandler((sender, args) => proxy1handled = true);
				proxy1.TestEvent += handler1;

				var proxy2handled = false;
				var handler2 = new EventHandler((sender, args) => proxy2handled = true);
				proxy2.TestEvent += handler2;

				var proxy3handled = false;
				var handler3 = new EventHandler((sender, args) => { proxy3handled = true; throw new Exception(); });
				proxy3.TestEvent += handler3;

				proxy1.RaiseTestEvent();
				Assert.IsTrue(proxy1handled);
				Assert.IsTrue(proxy2handled);
				Assert.IsTrue(proxy3handled);

				proxy1handled = false;
				proxy2handled = false;
				proxy3handled = false;

				proxy2.RaiseTestEvent();
				Assert.IsTrue(proxy1handled);
				Assert.IsTrue(proxy2handled);
				Assert.IsFalse(proxy3handled);

				proxy1handled = false;
				proxy2handled = false;

				proxy3.RaiseTestEvent();
				Assert.IsTrue(proxy1handled);
				Assert.IsTrue(proxy2handled);
				Assert.IsFalse(proxy3handled);
			}
		}

		[TestMethod]
		public void SubscriptionUnsubscription_UsingStaticEvents()
		{
			var counter = 0;
			var eventHandler = new EventHandler((s, e) => counter++);

			var proxy = ZyanConnection.CreateProxy<ISampleServer>("SingleCall");
			proxy.StaticEvent += eventHandler;
			Assert.AreEqual(0, counter);

			proxy.RaiseStaticEvent();
			Assert.AreEqual(1, counter);

			proxy.StaticEvent -= eventHandler; // unsubscription #1
			proxy.RaiseStaticEvent();
			Assert.AreEqual(1, counter);

			proxy.StaticEvent += eventHandler;
			proxy.RaiseStaticEvent();
			Assert.AreEqual(2, counter);

			proxy.StaticEvent -= eventHandler; // unsubscription #2
			proxy.RaiseStaticEvent();
			Assert.AreEqual(2, counter);
		}

		[TestMethod]
		public void EventsOnExternalSingletonComponentsWorkGlobally()
		{
			var nullProtocol = new NullClientProtocolSetup();

			// start two new sessions
			using (var conn2 = new ZyanConnection(ZyanConnection.ServerUrl, nullProtocol))
			using (var conn3 = new ZyanConnection(ZyanConnection.ServerUrl, nullProtocol))
			{
				var proxy1 = ZyanConnection.CreateProxy<ISampleServer>("SingletonExternal");
				var proxy2 = conn2.CreateProxy<ISampleServer>("SingletonExternal");
				var proxy3 = conn3.CreateProxy<ISampleServer>("SingletonExternal");

				var proxy1handled = false;
				var handler1 = new EventHandler((sender, args) => proxy1handled = true);
				proxy1.TestEvent += handler1;

				var proxy2handled = false;
				var handler2 = new EventHandler((sender, args) => proxy2handled = true);
				proxy2.TestEvent += handler2;

				var proxy3handled = false;
				var handler3 = new EventHandler((sender, args) => { proxy3handled = true; throw new Exception(); });
				proxy3.TestEvent += handler3;

				proxy1.RaiseTestEvent();
				Assert.IsTrue(proxy1handled);
				Assert.IsTrue(proxy2handled);
				Assert.IsTrue(proxy3handled);

				proxy1handled = false;
				proxy2handled = false;
				proxy3handled = false;

				proxy2.RaiseTestEvent();
				Assert.IsTrue(proxy1handled);
				Assert.IsTrue(proxy2handled);
				Assert.IsFalse(proxy3handled);

				proxy1handled = false;
				proxy2handled = false;

				proxy3.RaiseTestEvent();
				Assert.IsTrue(proxy1handled);
				Assert.IsTrue(proxy2handled);
				Assert.IsFalse(proxy3handled);
			}
		}

		[TestMethod]
		public void EventsOnSingleCallComponentsWorkGlobally()
		{
			var nullProtocol = new NullClientProtocolSetup();

			// start two new sessions
			using (var conn2 = new ZyanConnection(ZyanConnection.ServerUrl, nullProtocol))
			using (var conn3 = new ZyanConnection(ZyanConnection.ServerUrl, nullProtocol))
			{
				var proxy1 = ZyanConnection.CreateProxy<ISampleServer>("SingleCall");
				var proxy2 = conn2.CreateProxy<ISampleServer>("SingleCall");
				var proxy3 = conn3.CreateProxy<ISampleServer>("SingleCall");

				var proxy1handled = false;
				var handler1 = new EventHandler((sender, args) => proxy1handled = true);
				proxy1.TestEvent += handler1;

				var proxy2handled = false;
				var handler2 = new EventHandler((sender, args) => proxy2handled = true);
				proxy2.TestEvent += handler2;

				var proxy3handled = false;
				var handler3 = new EventHandler((sender, args) => { proxy3handled = true; throw new Exception(); });
				proxy3.TestEvent += handler3;

				proxy1.RaiseTestEvent();
				Assert.IsTrue(proxy1handled);
				Assert.IsTrue(proxy2handled);
				Assert.IsTrue(proxy3handled);

				proxy1handled = false;
				proxy2handled = false;
				proxy3handled = false;

				proxy2.RaiseTestEvent();
				Assert.IsTrue(proxy1handled);
				Assert.IsTrue(proxy2handled);
				Assert.IsFalse(proxy3handled);

				proxy1handled = false;
				proxy2handled = false;

				proxy3.RaiseTestEvent();
				Assert.IsTrue(proxy1handled);
				Assert.IsTrue(proxy2handled);
				Assert.IsFalse(proxy3handled);
			}
		}

		[TestMethod]
		public void SessionEventsAreRoutedByTheirOwnThreads()
		{
			var host = CreateZyanHost(5432, "SecondServer");
			var conn1 = CreateZyanConnection(5432, "SecondServer");
			var proxy1 = conn1.CreateProxy<ISampleServer>("Singleton2");
			var conn2 = CreateZyanConnection(5432, "SecondServer");
			var proxy2 = conn2.CreateProxy<ISampleServer>("Singleton2");

			var handledCounter = 0;
			EventHandler getEventHandler(Guid sessionId) => (s, e) =>
			{
				handledCounter++;

				// note: this stuff works only on NullChannel because the client
				// thread where the event is raised is the same server thread
				var threadName = Thread.CurrentThread.Name;
				Assert.IsTrue(threadName.Contains(sessionId.ToString()));
			};

			proxy1.TestEvent += getEventHandler(conn1.SessionID);
			proxy2.TestEvent += getEventHandler(conn2.SessionID);

			ZyanSettings.LegacyBlockingEvents = false;
			proxy1.RaiseTestEvent();
			proxy2.RaiseTestEvent();
			Thread.Sleep(300);

			ZyanSettings.LegacyBlockingEvents = true;
			Assert.AreEqual(4, handledCounter); // each proxy handles 2 events, so 2 * 2 = 4

			// Note: dispose connection before the host
			// so that it can unsubscribe from all remove events
			conn1.Dispose();
			conn2.Dispose();
			host.Dispose();
		}
	}
}
