using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Zyan.Communication;
using Zyan.Communication.Delegates;

namespace Zyan.Tests
{
	#region Unit testing platform abstraction layer
#if NUNIT
	using NUnit.Framework;
	using TestClass = NUnit.Framework.TestFixtureAttribute;
	using TestMethod = NUnit.Framework.TestAttribute;
	using ClassInitializeNonStatic = NUnit.Framework.OneTimeSetUpAttribute;
	using ClassInitialize = DummyAttribute;
	using ClassCleanupNonStatic = NUnit.Framework.OneTimeTearDownAttribute;
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
	/// Test class for event stub.
	///</summary>
	[TestClass]
	public class EventStubTests
	{
		#region Sample interfaces and services, etc.

		public interface ISampleInterface
		{
			string FireHandlers(int argument);

			event EventHandler SimpleEvent;

			event EventHandler<CancelEventArgs> CancelEvent;

			Action ActionDelegate { get; set; }

			Func<int, string> FuncDelegate { get; set; }

			int SimpleEventHandlerCount { get; }
		}

		public interface ISampleDescendant1 : ISampleInterface
		{
			event EventHandler NewEvent;

			Action NewDelegate { get; set; }
		}

		public interface ISampleDescendant2 : ISampleDescendant1, ISampleInterface
		{
			event EventHandler<CancelEventArgs> NewCancelEvent;
		}

		public class SampleService : ISampleInterface
		{
			public string FireHandlers(int argument)
			{
				if (SimpleEvent != null)
				{
					SimpleEvent(this, EventArgs.Empty);
				}

				if (CancelEvent != null)
				{
					CancelEvent(this, new CancelEventArgs());
				}

				if (ActionDelegate != null)
				{
					ActionDelegate();
				}

				if (FuncDelegate != null)
				{
					return FuncDelegate(argument);
				}

				return null;
			}

			public event EventHandler SimpleEvent;

			public event EventHandler<CancelEventArgs> CancelEvent;

			public Action ActionDelegate { get; set; }

			public Func<int, string> FuncDelegate { get; set; }

			public int SimpleEventHandlerCount
			{
				get { return EventStub.GetHandlerCount(SimpleEvent); }
			}
		}

		public void AssertType<T>(object instance)
		{
#if NUNIT
			Assert.IsInstanceOf<T>(instance);
#else
			Assert.IsInstanceOfType(instance, typeof(T));
#endif
		}

		#endregion

		#region Initialization and cleanup

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			StartServer(null);
		}

		[ClassInitialize]
		public static void StartServer(TestContext ctx)
		{
			ZyanSettings.LegacyBlockingEvents = true;
		}

		#endregion

		[TestMethod]
		public void EventStubContainsEventsAndDelegates()
		{
			var eventStub = new EventStub(typeof(ISampleInterface));
			Assert.IsNotNull(eventStub["SimpleEvent"]);
			Assert.IsNotNull(eventStub["CancelEvent"]);
			Assert.IsNotNull(eventStub["ActionDelegate"]);
			Assert.IsNotNull(eventStub["FuncDelegate"]);
		}

		[TestMethod]
		public void EventStubContainsInheritedEventsAndDelegates()
		{
			var eventStub = new EventStub(typeof(ISampleDescendant2));
			Assert.IsNotNull(eventStub["NewCancelEvent"]);
			Assert.IsNotNull(eventStub["NewEvent"]);
			Assert.IsNotNull(eventStub["NewDelegate"]);
			Assert.IsNotNull(eventStub["SimpleEvent"]);
			Assert.IsNotNull(eventStub["CancelEvent"]);
			Assert.IsNotNull(eventStub["ActionDelegate"]);
			Assert.IsNotNull(eventStub["FuncDelegate"]);
		}

		[TestMethod]
		public void EventStubDelegatesHaveSameTypesAsTheirOriginals()
		{
			var eventStub = new EventStub(typeof(ISampleInterface));
			AssertType<EventHandler>(eventStub["SimpleEvent"]);
			AssertType<EventHandler<CancelEventArgs>>(eventStub["CancelEvent"]);
			AssertType<Action>(eventStub["ActionDelegate"]);
			AssertType<Func<int, string>>(eventStub["FuncDelegate"]);
		}

		[TestMethod]
		public void EventStub_SimpleHandleTests()
		{
			// add the first handler
			var eventStub = new EventStub(typeof(ISampleInterface));
			var fired = false;
			eventStub.AddHandler("SimpleEvent", new EventHandler((sender, args) => fired = true));

			// check if it is called
			var handler = (EventHandler)eventStub["SimpleEvent"];
			handler(this, EventArgs.Empty);
			Assert.IsTrue(fired);

			// add the second handler
			fired = false;
			var firedAgain = false;
			var tempHandler = new EventHandler((sender, args) => firedAgain = true);
			eventStub.AddHandler("SimpleEvent", tempHandler);

			// check if it is called
			handler(this, EventArgs.Empty);
			Assert.IsTrue(fired);
			Assert.IsTrue(firedAgain);

			// remove the second handler
			fired = false;
			firedAgain = false;
			eventStub.RemoveHandler("SimpleEvent", tempHandler);

			// check if it is not called
			handler(this, EventArgs.Empty);
			Assert.IsTrue(fired);
			Assert.IsFalse(firedAgain);
		}

		[TestMethod]
		public void EventStub_CancelEventTests()
		{
			// add the first handler
			var eventStub = new EventStub(typeof(ISampleInterface));
			var fired = false;
			eventStub.AddHandler("CancelEvent", new EventHandler<CancelEventArgs>((sender, args) => fired = true));

			// check if it is called
			var handler = (EventHandler<CancelEventArgs>)eventStub["CancelEvent"];
			handler(this, new CancelEventArgs());
			Assert.IsTrue(fired);

			// add the second handler
			fired = false;
			var firedAgain = false;
			var tempHandler = new EventHandler<CancelEventArgs>((sender, args) => firedAgain = true);
			eventStub.AddHandler("CancelEvent", tempHandler);

			// check if it is called
			handler(this, new CancelEventArgs());
			Assert.IsTrue(fired);
			Assert.IsTrue(firedAgain);

			// remove the second handler
			fired = false;
			firedAgain = false;
			eventStub.RemoveHandler("CancelEvent", tempHandler);

			// check if it is not called
			handler(this, new CancelEventArgs());
			Assert.IsTrue(fired);
			Assert.IsFalse(firedAgain);
		}

		[TestMethod]
		public void EventStub_ActionDelegateTests()
		{
			// add the first handler
			var eventStub = new EventStub(typeof(ISampleInterface));
			var fired = false;
			eventStub.AddHandler("ActionDelegate", new Action(() => fired = true));

			// check if it is called
			var handler = (Action)eventStub["ActionDelegate"];
			handler();
			Assert.IsTrue(fired);

			// add the second handler
			fired = false;
			var firedAgain = false;
			var tempHandler = new Action(() => firedAgain = true);
			eventStub.AddHandler("ActionDelegate", tempHandler);

			// check if it is called
			handler();
			Assert.IsTrue(fired);
			Assert.IsTrue(firedAgain);

			// remove the second handler
			fired = false;
			firedAgain = false;
			eventStub.RemoveHandler("ActionDelegate", tempHandler);

			// check if it is not called
			handler();
			Assert.IsTrue(fired);
			Assert.IsFalse(firedAgain);
		}

		[TestMethod]
		public void EventStub_FuncDelegateTests()
		{
			// add the first handler
			var eventStub = new EventStub(typeof(ISampleInterface));
			var fired = false;
			eventStub.AddHandler("FuncDelegate", new Func<int, string>(a => { fired = true; return a.ToString(); }));

			// check if it is called
			var handler = (Func<int, string>)eventStub["FuncDelegate"];
			var result = handler(123);
			Assert.IsTrue(fired);
			Assert.AreEqual("123", result);

			// add the second handler
			fired = false;
			var firedAgain = false;
			var tempHandler = new Func<int, string>(a => { firedAgain = true; return a.ToString(); });
			eventStub.AddHandler("FuncDelegate", tempHandler);

			// check if it is called
			result = handler(321);
			Assert.IsTrue(fired);
			Assert.IsTrue(firedAgain);
			Assert.AreEqual("321", result);

			// remove the second handler
			fired = false;
			firedAgain = false;
			eventStub.RemoveHandler("FuncDelegate", tempHandler);

			// check if it is not called
			result = handler(0);
			Assert.IsTrue(fired);
			Assert.IsFalse(firedAgain);
			Assert.AreEqual("0", result);
		}

		[TestMethod]
		public void EventStub_WireUnwireTests()
		{
			var eventStub = new EventStub(typeof(ISampleInterface));
			var simpleEventFired = false;
			var cancelEventFired = false;
			var actionFired = false;
			var funcFired = false;

			// add event handlers
			eventStub.AddHandler("SimpleEvent", new EventHandler((sender, args) => simpleEventFired = true));
			eventStub.AddHandler("CancelEvent", new EventHandler<CancelEventArgs>((sender, args) => cancelEventFired = true));
			eventStub.AddHandler("ActionDelegate", new Action(() => actionFired = true));
			eventStub.AddHandler("FuncDelegate", new Func<int, string>(a => { funcFired = true; return a.ToString(); }));
			eventStub.AddHandler("FuncDelegate", new Func<int, string>(a => { return (a * 2).ToString(); }));

			// wire up events
			var component = new SampleService();
			eventStub.WireTo(component);

			// test if it works
			var result = component.FireHandlers(102030);
			Assert.AreEqual("204060", result);
			Assert.IsTrue(simpleEventFired);
			Assert.IsTrue(cancelEventFired);
			Assert.IsTrue(actionFired);
			Assert.IsTrue(funcFired);

			// unwire
			simpleEventFired = false;
			cancelEventFired = false;
			actionFired = false;
			funcFired = false;
			eventStub.UnwireFrom(component);

			// test if it works
			result = component.FireHandlers(123);
			Assert.IsNull(result);
			Assert.IsFalse(simpleEventFired);
			Assert.IsFalse(cancelEventFired);
			Assert.IsFalse(actionFired);
			Assert.IsFalse(funcFired);
		}

		[TestMethod]
		public void EventStubHandlerCountTests()
		{
			var eventStub = new EventStub(typeof(ISampleInterface));
			var sampleService = new SampleService();

			eventStub.WireTo(sampleService);
			Assert.AreEqual(0, sampleService.SimpleEventHandlerCount);

			eventStub.AddHandler("SimpleEvent", new EventHandler((s, e) => { }));
			Assert.AreEqual(1, sampleService.SimpleEventHandlerCount);

			eventStub.AddHandler("SimpleEvent", new EventHandler((s, e) => { }));
			Assert.AreEqual(2, sampleService.SimpleEventHandlerCount);
		}
	}
}
