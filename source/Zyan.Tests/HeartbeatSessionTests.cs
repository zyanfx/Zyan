using System;
using System.Security.Principal;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols.Null;

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
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Regression test for heartbeat session. Issue #1808
	/// </summary>
	[TestClass]
	public class HeartbeatSessionTests
	{
		#region Setup test environment and cleanup

		public class JohnGaltIdentity : IIdentity
		{
			public const string DefaultName = "John Galt";

			public string AuthenticationType { get { return string.Empty; } }

			public bool IsAuthenticated { get { return true; } }

			public string Name { get { return DefaultName; } }
		}

		public class JohnGaltAuthenticationProvider : IAuthenticationProvider
		{
			public AuthResponseMessage Authenticate(AuthRequestMessage authRequest)
			{
				return new AuthResponseMessage()
				{
					ErrorMessage = string.Empty,
					Success = true,
					AuthenticatedIdentity = new JohnGaltIdentity()
				};
			}
		}

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			StartServer(null);
		}

		[ClassCleanupNonStatic]
		public void Cleanup()
		{
			StopServer();
		}

		[ClassInitialize]
		public static void StartServer(TestContext ctx)
		{
			ZyanHost = new ZyanComponentHost("HeartbeatServer", new NullServerProtocolSetup(5678)
			{
				AuthenticationProvider = new JohnGaltAuthenticationProvider()
			});
		}

		[ClassCleanup]
		public static void StopServer()
		{
			if (ZyanHost != null)
			{
				ZyanHost.Dispose();
				ZyanHost = null;
			}
		}

		private static ZyanComponentHost ZyanHost { get; set; }

		#endregion

		[TestMethod]
		public void HeartbeatSessionShouldBeValid()
		{
			var heartbeatsReceived = 0;
			var nullSession = false;
			var userIdentity = default(IIdentity);

			// set up heartbeat event handler
			ZyanHost.PollingEventTracingEnabled = true;
			ZyanHost.ClientHeartbeatReceived += (s, e) =>
			{
				heartbeatsReceived++;

				if (ServerSession.CurrentSession != null)
				{
					userIdentity = ServerSession.CurrentSession.Identity;
				}
				else
				{
					nullSession = true;
				}
			};

			// set up the connection
			using (var conn = new ZyanConnection("null://NullChannel:5678/HeartbeatServer"))
			{
				// the code below uses actual heartbeat timer to send heartbeats:
				//conn.PollingInterval = TimeSpan.FromMilliseconds(5);
				//conn.PollingEnabled = true;

				// use the internal method to avoid using timer in unit tests
				conn.SendHeartbeat(null);
				Thread.Sleep(500);
			}

			// validate heartbeat
			Assert.IsTrue(heartbeatsReceived > 0);
			Assert.IsFalse(nullSession);
			Assert.AreEqual(JohnGaltIdentity.DefaultName, userIdentity.Name);
		}
	}
}
