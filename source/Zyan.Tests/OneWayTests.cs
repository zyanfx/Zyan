using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Zyan.Communication;
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
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for one-way methods.
	///</summary>
	[TestClass]
	public class OneWayTests
	{
		#region Interfaces and components

		/// <summary>
		/// Sample server interface
		/// </summary>
		public interface ISampleServer
		{
			void OneWayVoidMethod(Action beforeSleep, Action afterSleep);

			string NonOneWayMethod(Action callback);

			void NonOneWayMethod(ref int value, Action callback);

			void OneWayMethodWithException(Action callback);

			void CheckServerSession(Action<Guid> callback);
		}

		/// <summary>
		/// Sample server implementation
		/// </summary>
		public class SampleServer : ISampleServer
		{
			[OneWay]
			public void OneWayVoidMethod(Action beforeSleep, Action afterSleep)
			{
				beforeSleep();
				Thread.Sleep(300);
				afterSleep();
			}

			[OneWay] // OneWay attribute should be ignored because of the non-void return type
			public string NonOneWayMethod(Action callback)
			{
				callback();
				return "Non-empty string";
			}

			[OneWay] // OneWay attribute should be ignored because of the ref parameter
			public void NonOneWayMethod(ref int value, Action callback)
			{
				callback();
			}

			[OneWay]
			public void OneWayMethodWithException(Action callback)
			{
				try
				{
					checked
					{
						var mul = int.MaxValue;
						mul *= mul;
					}
				}
				catch (Exception ex)
				{
					callback();
					throw new ApplicationException("Something bad happened", ex);
				}
			}

			[OneWay]
			public void CheckServerSession(Action<Guid> callback)
			{
				var id = ServerSession.CurrentSession == null ? Guid.Empty : ServerSession.CurrentSession.SessionID;
				callback(id);
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
			var serverSetup = new IpcBinaryServerProtocolSetup("OneWayTest");
			ZyanHost = new ZyanComponentHost("OneWayServer", serverSetup);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer>();
			ZyanConnection = new ZyanConnection("ipc://OneWayTest/OneWayServer");
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		#endregion

#if !FX3
		[TestMethod]
		public void OneWayMethodCallPreservesCurrentSession()
		{
			// check server session several times to make sure everything is ok
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			using (var countdownEvent = new CountdownEvent(100))
			{
				var success = true;

				// execute one-way method and compare current session id
				for (int i = 0; i < countdownEvent.InitialCount; i++)
				{
					proxy.CheckServerSession(id =>
					{
						success = success && id == ZyanConnection.SessionID;
						countdownEvent.Signal();
					});
				}

				// wait a bit
				Assert.IsTrue(countdownEvent.Wait(TimeSpan.FromSeconds(0.5)));
				Assert.IsTrue(success);
			}
		}
#endif

		[TestMethod]
		public void OneWayMethodCallShouldReturnImmediatelly()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			using (var mre = new ManualResetEvent(false))
			{
				var callbackExecuted = false;

				// this should return immediatelly
				proxy.OneWayVoidMethod(() =>
				{
					mre.Set();
				},
				() =>
				{
					callbackExecuted = true;
					mre.Set();
				});

				// check if the method is still running
				Assert.IsTrue(mre.WaitOne(5000));
				Assert.IsFalse(callbackExecuted);

				// wait for the method to finish
				mre.Reset();
				Assert.IsTrue(mre.WaitOne(5000));
				Assert.IsTrue(callbackExecuted);
			}
		}

		[TestMethod]
		public void OneWayAttributeShouldBeIgnoredOnNonVoidMethodTest()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var callbackExecuted = false;
			var result = String.Empty;

			// this method should be called synchronously
			result = proxy.NonOneWayMethod(() =>
			{
				callbackExecuted = true;
			});

			// make sure method call was completed synchronously
			Assert.IsFalse(String.IsNullOrEmpty(result));
			Assert.IsTrue(callbackExecuted);
		}

		[TestMethod]
		public void OneWayAttributeShouldBeIgnoredIfRefParametersArePresent()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var callbackExecuted = false;
			var refArg = 0;

			// this method should be called synchronously
			proxy.NonOneWayMethod(ref refArg, () =>
			{
				callbackExecuted = true;
			});

			// make sure method call was completed synchronously
			Assert.IsTrue(callbackExecuted);
		}

		[TestMethod]
		public void OneWayMethodDoesntThrowExceptions()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var callbackExecuted = false;
			using (var mre = new ManualResetEvent(false))
			{
				// this should return immediatelly
				proxy.OneWayMethodWithException(() =>
				{
					callbackExecuted = true;
					mre.Set();
				});

				// check if exception was caught
				Assert.IsTrue(mre.WaitOne(5000));
				Assert.IsTrue(callbackExecuted);
			}
		}
	}
}
