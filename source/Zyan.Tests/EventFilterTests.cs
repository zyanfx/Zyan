using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Delegates;
using Zyan.Communication.Protocols.Ipc;

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

		/// <summary>
		/// Sample server interface
		/// </summary>
		public interface ISampleServer
		{
			event EventHandler TestEvent;

			void RaiseTestEvent();

			event EventHandler<SessionEventArgs> SessionBoundEvent;

			void RaiseSessionBoundEvent();

			event EventHandler<CustomEventArgs> CustomSessionBoundEvent;

			void RaiseCustomSessionBoundEvent(int value);
		}

		/// <summary>
		/// Sample server implementation
		/// </summary>
		public class SampleServer : ISampleServer
		{
			public override int GetHashCode()
			{
				throw new AssertFailedException("GetHashCode() method is called remotely.");
			}

			public override bool Equals(object obj)
			{
				throw new AssertFailedException("Equals() method is called remotely.");
			}

			public override string ToString()
			{
				throw new AssertFailedException("ToString() method is called remotely.");
			}

			public event EventHandler TestEvent;

			public void RaiseTestEvent()
			{
				if (TestEvent != null)
				{
					TestEvent(null, EventArgs.Empty);
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
		}

		[Serializable]
		public class CustomEventArgs : SessionEventArgs
		{
			public int Value { get; set; }
		}

		#endregion

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
			var serverSetup = new IpcBinaryServerProtocolSetup("EventFilterTest");
			ZyanHost = new ZyanComponentHost("EventFilterServer", serverSetup);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer>(ActivationType.Singleton);

			var clientSetup = new IpcBinaryClientProtocolSetup();
			ZyanConnection = new ZyanConnection("ipc://EventFilterTest/EventFilterServer", clientSetup);
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		[TestMethod]
		public void SubscriptionUnsubscription_RegressionTest()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.TestEvent += TestEventHandler;
			EventHandled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(EventHandled);

			proxy.TestEvent -= TestEventHandler; // unsubscription #1
			EventHandled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(EventHandled);

			proxy.TestEvent += TestEventHandler;
			EventHandled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(EventHandled);

			proxy.TestEvent -= TestEventHandler; // unsubscription #2
			EventHandled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(EventHandled);
		}

		bool EventHandled { get; set; }

		void TestEventHandler(object sender, EventArgs e)
		{
			EventHandled = true;
		}

		[TestMethod]
		public void SessionBoundEvents_AreBoundToSessions()
		{
			using (var conn = new ZyanConnection(ZyanConnection.ServerUrl, new IpcBinaryClientProtocolSetup()))
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
			using (var conn = new ZyanConnection(ZyanConnection.ServerUrl, new IpcBinaryClientProtocolSetup()))
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
	}
}
