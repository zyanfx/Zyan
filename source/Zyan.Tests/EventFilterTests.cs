using System;
using System.Collections.Generic;
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
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for event filters.
	///</summary>
	[TestClass]
	public class EventFilterTests
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

			event EventHandler<SessionEventArgs> SessionBoundEvent;

			void RaiseSessionBoundEvent();

			event EventHandler<CustomEventArgs> CustomSessionBoundEvent;

			void RaiseCustomSessionBoundEvent(int value);

			event EventHandler<SampleEventArgs> SampleEvent;

			void RaiseSampleEvent(int value);

			event CustomEventType CustomEvent;

			void RaiseCustomEvent(int first = default(int), string second = default(string));
		}

		/// <summary>
		/// Sample server implementation
		/// </summary>
		public class SampleServer : ISampleServer
		{
			public event EventHandler TestEvent;

			public void RaiseTestEvent(EventArgs args)
			{
				if (TestEvent != null)
				{
					TestEvent(null, args);
				}
			}

			public event EventHandler<SessionEventArgs> SessionBoundEvent;

			public void RaiseSessionBoundEvent()
			{
				if (SessionBoundEvent != null)
				{
					SessionBoundEvent(null, new SessionEventArgs());
				}
			}

			public event EventHandler<CustomEventArgs> CustomSessionBoundEvent;

			public void RaiseCustomSessionBoundEvent(int value)
			{
				if (CustomSessionBoundEvent != null)
				{
					CustomSessionBoundEvent(null, new CustomEventArgs { Value = value });
				}
			}

			public event EventHandler<SampleEventArgs> SampleEvent;

			public void RaiseSampleEvent(int value)
			{
				if (SampleEvent != null)
				{
					SampleEvent(null, new SampleEventArgs { Value = value });
				}
			}

			public event CustomEventType CustomEvent;

			public void RaiseCustomEvent(int first = default(int), string second = default(string))
			{
				if (CustomEvent != null)
				{
					CustomEvent(first, second);
				}
			}
		}

		/// <summary>
		/// Custom session-bound event arguments.
		/// </summary>
		[Serializable]
		public class CustomEventArgs : SessionEventArgs
		{
			public int Value { get; set; }
		}

		/// <summary>
		/// Custom event argument class for event filtering.
		/// </summary>
		[Serializable]
		public class SampleEventArgs : EventArgs
		{
			public int Value { get; set; }
		}

		/// <summary>
		/// Event filter for the SampleEventArgs class.
		/// </summary>
		[Serializable]
		public class SampleEventFilter : EventFilterBase<SampleEventArgs>
		{
			public SampleEventFilter(params int[] values)
			{
				Values = new HashSet<int>(values);
			}

			public HashSet<int> Values { get; private set; }

			protected override bool AllowInvocation(object sender, SampleEventArgs args)
			{
				return Values.Contains(args.Value);
			}
		}

		/// <summary>
		/// Event filter for EventArgs class.
		/// </summary>
		[Serializable]
		public class TestEventFilter : EventFilterBase<EventArgs>
		{
			protected override bool AllowInvocation(object sender, EventArgs args)
			{
				return args == null;
			}
		}

		/// <summary>
		/// Event filter for custom session-bound event.
		/// </summary>
		[Serializable]
		public class CustomSessionBoundEventFilter : EventFilterBase<CustomEventArgs>
		{
			public CustomSessionBoundEventFilter(params int[] values)
			{
				Values = new HashSet<int>(values);
			}

			private ISet<int> Values { get; set; }

			protected override bool AllowInvocation(object sender, CustomEventArgs args)
			{
				return Values.Contains(args.Value);
			}
		}

		/// <summary>
		/// Event filter for CustomEventType.
		/// </summary>
		[Serializable]
		public class CustomEventFilter : IEventFilter
		{
			public CustomEventFilter(params string[] templates)
			{
				Templates = new HashSet<string>(templates);
			}

			private ISet<string> Templates { get; set; }

			public bool AllowInvocation(params object[] parameters)
			{
				var text = string.Format("{0}{1}", parameters);
				return Templates.Contains(text);
			}

			public bool Contains<TEventFilter>() where TEventFilter : IEventFilter
			{
				return this is TEventFilter;
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

			var serverSetup = new NullServerProtocolSetup(4567);
			ZyanHost = new ZyanComponentHost("EventFilterServer", serverSetup);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer>(ActivationType.Singleton);

			ZyanConnection = new ZyanConnection("null://NullChannel:4567/EventFilterServer");
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		#endregion

		[TestMethod]
		public void SubscriptionUnsubscription_RegressionTest()
		{
			var handled = false;
			var eventHandler = new EventHandler((s, e) => handled = true);

			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
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
		public void SubscriptionUnsubscriptionWithEventFilters_RegressionTest1()
		{
			var handled = false;
			var sampleEventHandler = new EventHandler<SampleEventArgs>((s, e) => handled = true);
			var filteredEventHandler = sampleEventHandler.AddFilter(new SampleEventFilter(1, 2, 3));

			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.SampleEvent += filteredEventHandler;
			handled = false;

			proxy.RaiseSampleEvent(1);
			Assert.IsTrue(handled);

			proxy.SampleEvent -= sampleEventHandler; // unsubscription without filter
			handled = false;

			proxy.RaiseSampleEvent(2);
			Assert.IsFalse(handled);

			proxy.SampleEvent += filteredEventHandler;
			handled = false;

			proxy.RaiseSampleEvent(3);
			Assert.IsTrue(handled);

			proxy.SampleEvent -= filteredEventHandler; // unsubscription with filter
			handled = false;

			proxy.RaiseSampleEvent(2);
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void SubscriptionUnsubscriptionWithEventFilters_RegressionTest2()
		{
			var handled = false;
			var testEventHandler = new EventHandler((s, e) => handled = true);
			var filteredEventHandler = testEventHandler.AddFilter(new TestEventFilter());

			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.TestEvent += filteredEventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= filteredEventHandler; // unsubscription with filter
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);

			proxy.TestEvent += filteredEventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= testEventHandler; // unsubscription without filter
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);
		}

		/* Session-bound events */

		[TestMethod]
		public void SessionBoundEvents_AreBoundToSessions()
		{
			// start a new session
			using (var conn = new ZyanConnection(ZyanConnection.ServerUrl, new NullClientProtocolSetup()))
			{
				var proxy1 = ZyanConnection.CreateProxy<ISampleServer>();
				var proxy2 = conn.CreateProxy<ISampleServer>();

				var handled1 = false;
				var handled2 = false;

				proxy1.SessionBoundEvent += (s, args) => handled1 = true;
				proxy2.SessionBoundEvent += (s, args) => handled2 = true;

				proxy1.RaiseSessionBoundEvent();
				Assert.IsTrue(handled1);
				Assert.IsFalse(handled2);

				handled1 = false;

				proxy2.RaiseSessionBoundEvent();
				Assert.IsFalse(handled1);
				Assert.IsTrue(handled2);
			}
		}

		[TestMethod]
		public void EventsWithArgumentsDerivedFromSessionBoundEvents_AreBoundToSessions()
		{
			// start a new session
			using (var conn = new ZyanConnection(ZyanConnection.ServerUrl, new NullClientProtocolSetup()))
			{
				var proxy1 = ZyanConnection.CreateProxy<ISampleServer>();
				var proxy2 = conn.CreateProxy<ISampleServer>();

				var handled1 = 0;
				var handled2 = 0;

				proxy1.CustomSessionBoundEvent += (s, args) => handled1 = args.Value;
				proxy2.CustomSessionBoundEvent += (s, args) => handled2 = args.Value;

				proxy1.RaiseCustomSessionBoundEvent(123);
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(0, handled2);

				proxy2.RaiseCustomSessionBoundEvent(321);
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(321, handled2);
			}
		}

		[TestMethod]
		public void EventsWithArgumentsDerivedFromSessionBoundEvents_CanListenToOtherSessions()
		{
			var nullProtocol = new NullClientProtocolSetup();

			// start two new sessions
			using (var conn2 = new ZyanConnection(ZyanConnection.ServerUrl, nullProtocol))
			using (var conn3 = new ZyanConnection(ZyanConnection.ServerUrl, nullProtocol))
			{
				var proxy1 = ZyanConnection.CreateProxy<ISampleServer>();
				var proxy2 = conn2.CreateProxy<ISampleServer>();
				var proxy3 = conn3.CreateProxy<ISampleServer>();
				var sessions13 = new[] { ZyanConnection.SessionID, conn3.SessionID }; // session2 is not included

				var handled1 = 0;
				var handled2 = 0;
				var handled3 = 0;

				proxy1.CustomSessionBoundEvent += (s, args) => handled1 = args.Value;
				proxy2.CustomSessionBoundEvent += FilteredEventHandler.Create<CustomEventArgs>((s, args) => handled2 = args.Value, new SessionEventFilter(sessions13));
				proxy3.CustomSessionBoundEvent += (s, args) => handled3 = args.Value;

				proxy1.RaiseCustomSessionBoundEvent(123);
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(123, handled2); // proxy2 receives event from session1
				Assert.AreEqual(0, handled3);

				proxy2.RaiseCustomSessionBoundEvent(321);
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(123, handled2); // proxy2 doesn't receive events from session2
				Assert.AreEqual(0, handled3);

				proxy3.RaiseCustomSessionBoundEvent(111);
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(111, handled2); // proxy2 receives event from session3
				Assert.AreEqual(111, handled3);
			}
		}

		[TestMethod]
		public void EventsWithArgumentsDerivedFromSessionBoundEvents_AreBoundToSessionsAndCanBeFiltered()
		{
			// start a new session
			using (var conn = new ZyanConnection(ZyanConnection.ServerUrl, new NullClientProtocolSetup()))
			{
				var proxy1 = ZyanConnection.CreateProxy<ISampleServer>();
				var proxy2 = conn.CreateProxy<ISampleServer>();

				var handled1 = 0;
				var handled2 = 0;

				proxy1.CustomSessionBoundEvent += FilteredEventHandler.Create((object s, CustomEventArgs args) => handled1 = args.Value, new CustomSessionBoundEventFilter(123, 321));
				proxy2.CustomSessionBoundEvent += (s, args) => handled2 = args.Value;

				proxy1.RaiseCustomSessionBoundEvent(123);
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(0, handled2);

				proxy2.RaiseCustomSessionBoundEvent(321);
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(321, handled2);

				proxy1.RaiseCustomSessionBoundEvent(111); // filtered out
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(321, handled2);
			}
		}

		/* Client-side (local) event filters */

		[TestMethod]
		public void FilteredEventHandlerUsingFactorySyntax_FiltersEventsLocally()
		{
			// prepare event handler, attach event filter
			var handledValue = 0;
			var sample = new SampleServer();
			sample.SampleEvent += FilteredEventHandler.Create((object sender, SampleEventArgs args) => handledValue = args.Value, new SampleEventFilter(321));

			// raise events, check results
			sample.RaiseSampleEvent(123); // filtered out
			Assert.AreEqual(0, handledValue);

			sample.RaiseSampleEvent(321);
			Assert.AreEqual(321, handledValue);

			handledValue = 111;
			sample.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingFluentSyntax_FiltersEventsLocally()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach client-side event filter
			var sample = new SampleServer();
			sample.SampleEvent += handler.AddFilter(new SampleEventFilter(321));

			// raise events, check results
			sample.RaiseSampleEvent(123); // filtered out
			Assert.AreEqual(0, handledValue);

			sample.RaiseSampleEvent(321);
			Assert.AreEqual(321, handledValue);

			handledValue = 111;
			sample.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingCombinedFilter_FiltersEventsLocally()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach client-side event filter
			var sample = new SampleServer();
			sample.SampleEvent += handler
				.AddFilter(new SampleEventFilter(321, 123, 111))
				.AddFilter(new SampleEventFilter(333, 123, 222)); // 123 will pass both filters

			// raise events, check results
			sample.RaiseSampleEvent(321); // filtered out
			Assert.AreEqual(0, handledValue);

			sample.RaiseSampleEvent(123);
			Assert.AreEqual(123, handledValue);

			handledValue = 111;
			sample.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingFlexibleFilter_FiltersEventsLocally()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach client-side event filter
			var sample = new SampleServer();
			sample.SampleEvent += handler.AddFilter((f, args) => args.Value == 123);

			// raise events, check results
			sample.RaiseSampleEvent(321); // filtered out
			Assert.AreEqual(0, handledValue);

			sample.RaiseSampleEvent(123);
			Assert.AreEqual(123, handledValue);

			handledValue = 111;
			sample.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerOfTypeEventHandler_FiltersEventsLocally()
		{
			// prepare event handler
			var handled = false;
			var handler = new EventHandler((sender, args) => handled = true);

			// attach client-side event filter
			var sample = new SampleServer();
			sample.TestEvent += handler.AddFilter(new TestEventFilter());

			// raise events, check results
			sample.RaiseTestEvent(EventArgs.Empty); // filtered out
			Assert.IsFalse(handled);

			sample.RaiseTestEvent(null);
			Assert.IsTrue(handled);

			handled = false;
			sample.RaiseTestEvent(new EventArgs()); // filtered out
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void FilteredEventHandlerOfCustomType_FiltersEventsLocally()
		{
			// prepare event handler
			var firstArgument = 0;
			var secondArgument = string.Empty;
			var handled = false;
			var handler = new CustomEventType((first, second) =>
			{
				firstArgument = first;
				secondArgument = second;
				handled = true;
			});

			// attach client-side event filter
			var sample = new SampleServer();
			sample.CustomEvent += FilteredEventHandler.Create(handler, new CustomEventFilter("3.14"));

			// raise events, check results
			sample.RaiseCustomEvent(1, string.Empty); // filtered out
			Assert.IsFalse(handled);

			sample.RaiseCustomEvent(3, ".14");
			Assert.IsTrue(handled);
			Assert.AreEqual(3, firstArgument);
			Assert.AreEqual(".14", secondArgument);

			handled = false;
			sample.RaiseCustomEvent(); // filtered out
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void FilteredCustomHandlerUsingCombinedFilter_FiltersEventsLocally()
		{
			// prepare event handler
			var firstArgument = 0;
			var secondArgument = string.Empty;
			var handled = false;
			var handler = new CustomEventType((first, second) =>
			{
				firstArgument = first;
				secondArgument = second;
				handled = true;
			});

			// initialize event filter
			handler = FilteredEventHandler.Create(handler, new CustomEventFilter("2.718", "1.618"));
			handler = FilteredEventHandler.Create(handler, new CustomEventFilter("3.14", "2.718"));

			// attach client-side event filter
			var sample = new SampleServer();
			sample.CustomEvent += handler;

			// raise events, check results
			sample.RaiseCustomEvent(3, ".14"); // filtered out
			Assert.IsFalse(handled);

			sample.RaiseCustomEvent(2, ".718");
			Assert.IsTrue(handled);
			Assert.AreEqual(2, firstArgument);
			Assert.AreEqual(".718", secondArgument);

			handled = false;
			sample.RaiseCustomEvent(1, ".618"); // filtered out
			Assert.IsFalse(handled);
		}

		/* Server-side (remote) event filters */

		[TestMethod]
		public void FilteredEventHandlerUsingFactorySyntax_FiltersEventsRemotely()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.SampleEvent += FilteredEventHandler.Create(handler, new SampleEventFilter(123), false);

			// raise events
			proxy.RaiseSampleEvent(111); // filtered out
			Assert.AreEqual(0, handledValue);

			proxy.RaiseSampleEvent(123);
			Assert.AreEqual(123, handledValue);

			handledValue = 222;
			proxy.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(222, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingFluentSyntax_FiltersEventsRemotely()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.SampleEvent += handler.AddFilter(new SampleEventFilter(123), false);

			// raise events
			proxy.RaiseSampleEvent(111); // filtered out
			Assert.AreEqual(0, handledValue);

			proxy.RaiseSampleEvent(123);
			Assert.AreEqual(123, handledValue);

			handledValue = 222;
			proxy.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(222, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingCombinedFilter_FiltersEventsRemotely()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.SampleEvent += handler
				.AddFilter(new SampleEventFilter(3, 5, 7), false)
				.AddFilter(new SampleEventFilter(9, 6, 3), false)
				.AddFilter(new SampleEventFilter(1, 2, 3), false)
				.AddFilter(new SampleEventFilter(5, 4, 3), false); // 3 will pass all filters

			// raise events, check results
			proxy.RaiseSampleEvent(321); // filtered out
			Assert.AreEqual(0, handledValue);

			proxy.RaiseSampleEvent(3);
			Assert.AreEqual(3, handledValue);

			handledValue = 111;
			proxy.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingFlexibleFilter_FiltersEventsRemotely()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach client-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.SampleEvent += handler.AddFilter((sender, args) => args.Value == 123, false);

			// raise events, check results
			proxy.RaiseSampleEvent(321); // filtered out
			Assert.AreEqual(0, handledValue);

			proxy.RaiseSampleEvent(123);
			Assert.AreEqual(123, handledValue);

			handledValue = 111;
			proxy.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerOfTypeEventHandler_FiltersEventsRemotely()
		{
			// prepare event handler
			var handled = false;
			var handler = new EventHandler((sender, args) => handled = true);

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.TestEvent += handler.AddFilter(new TestEventFilter(), false);

			// raise events, check results
			proxy.RaiseTestEvent(EventArgs.Empty); // filtered out
			Assert.IsFalse(handled);

			proxy.RaiseTestEvent(null);
			Assert.IsTrue(handled);

			handled = false;
			proxy.RaiseTestEvent(new EventArgs()); // filtered out
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void FilteredEventHandlerOfCustomType_FiltersEventsRemotely()
		{
			// prepare event handler
			var firstArgument = 0;
			var secondArgument = string.Empty;
			var handled = false;
			var handler = new CustomEventType((first, second) =>
			{
				firstArgument = first;
				secondArgument = second;
				handled = true;
			});

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.CustomEvent += FilteredEventHandler.Create(handler, new CustomEventFilter("2.71828"), false);

			// raise events, check results
			proxy.RaiseCustomEvent(1, string.Empty); // filtered out
			Assert.IsFalse(handled);

			proxy.RaiseCustomEvent(2, ".71828");
			Assert.IsTrue(handled);
			Assert.AreEqual(2, firstArgument);
			Assert.AreEqual(".71828", secondArgument);

			handled = false;
			proxy.RaiseCustomEvent(); // filtered out
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void FilteredCustomHandlerUsingCombinedFilter_FiltersEventsRemotely()
		{
			// prepare event handler
			var firstArgument = 0;
			var secondArgument = string.Empty;
			var handled = false;
			var handler = new CustomEventType((first, second) =>
			{
				firstArgument = first;
				secondArgument = second;
				handled = true;
			});

			// initialize event filter
			handler = FilteredEventHandler.Create(handler, new CustomEventFilter("2.718", "1.618"), false);
			handler = FilteredEventHandler.Create(handler, new CustomEventFilter("3.14", "2.718"), false);

			// attach client-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.CustomEvent += handler;

			// raise events, check results
			proxy.RaiseCustomEvent(3, ".14"); // filtered out
			Assert.IsFalse(handled);

			proxy.RaiseCustomEvent(2, ".718");
			Assert.IsTrue(handled);
			Assert.AreEqual(2, firstArgument);
			Assert.AreEqual(".718", secondArgument);

			handled = false;
			proxy.RaiseCustomEvent(1, ".618"); // filtered out
			Assert.IsFalse(handled);
		}

		/* Syntax checks */

		private void SyntaxChecks()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();

			// Create for EventHandler type — no generic parameters
			proxy.TestEvent += FilteredEventHandler.Create(TestEventHandler, new TestEventFilter());

			// Create for EventHandler<TEventArgs> type — single generic parameter for EventArgs type
			proxy.SampleEvent += FilteredEventHandler.Create<SampleEventArgs>(SampleEventHandler, new SampleEventFilter());

			// Create for custom event type — single generic parameter for delegate type
			proxy.CustomEvent += FilteredEventHandler.Create<CustomEventType>(CustomEventHandler, new CustomEventFilter());

			// AddFilter for EventHandler type — no generic parameters
			var testEventHandler = new EventHandler(TestEventHandler);
			testEventHandler = testEventHandler.AddFilter(new TestEventFilter());

			// AddFilter for EventHandler<TEventArgs> type — no generic parameters
			var sampleEventHandler = new EventHandler<SampleEventArgs>(SampleEventHandler);
			sampleEventHandler = sampleEventHandler.AddFilter(new SampleEventFilter());

			// AddFilter for EventHandler<TEventArgs> using Linq expression — no generic parameters
			sampleEventHandler = sampleEventHandler.AddFilter((sender, args) => args.Value != 1);
		}

		private void CustomEventHandler(int a, string b)
		{
			throw new NotImplementedException();
		}

		private void SampleEventHandler(object sender, SampleEventArgs args)
		{
			throw new NotImplementedException();
		}

		private void TestEventHandler(object sender, EventArgs args)
		{
			throw new NotImplementedException();
		}
	}
}
