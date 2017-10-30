using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Delegates;
using Zyan.Communication.Protocols.Null;

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
			ZyanSettings.LegacyBlockingEvents = true;
			ZyanSettings.LegacyBlockingSubscriptions = true;
			ZyanSettings.LegacyUnprotectedEventHandlers = true;

			var serverSetup = new NullServerProtocolSetup(2345);
			ZyanHost = new ZyanComponentHost("EventsServer", serverSetup);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer<int>>("Singleton", ActivationType.Singleton);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer<short>>("Singleton2", ActivationType.Singleton);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer<long>>("Singleton3", ActivationType.Singleton);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer<byte>>("SingleCall", ActivationType.SingleCall);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer<char>>("SingletonExternal", new SampleServer<char>());

			ZyanConnection = new ZyanConnection("null://NullChannel:2345/EventsServer");
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

		[TestMethod, MyIgnore] // note: this test fails on Appveyor
		public void ExceptionInEventHandlerCancelsSubscription()
		{
			var handled = false;
			var eventHandler = new EventHandler((s, e) =>
			{
				handled = true;
				throw new Exception();
			});

			var proxy = ZyanConnection.CreateProxy<ISampleServer>("Singleton2");
			proxy.TestEvent += eventHandler;

			// raise an event, catch exception and unsubscribe automatically
			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			handled = false;
			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);
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
	}
}
