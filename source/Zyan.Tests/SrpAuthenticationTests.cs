using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using SecureRemotePassword;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;
using Zyan.Communication.Security.SecureRemotePassword;
using Hashtable = System.Collections.Hashtable;

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
	using ClassCleanupNonStatic = DummyAttribute;
	using ClassInitializeNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for the SRP-6a authentication protocol classes.
	/// </summary>
	[TestClass]
	public class SrpAuthenticationTests
	{
		#region Sample component classes and interfaces

		private const string UserName = "bozo";
		private const string Password = "h4ck3r";

		// custom SRP-6a parameters: prime number and generator values taken from RFC5054 (3072-bit group)
		private static SrpParameters CustomSrpParameters = SrpParameters.Create<SHA384>(@"
			FFFFFFFF FFFFFFFF C90FDAA2 2168C234 C4C6628B 80DC1CD1 29024E08
			8A67CC74 020BBEA6 3B139B22 514A0879 8E3404DD EF9519B3 CD3A431B
			302B0A6D F25F1437 4FE1356D 6D51C245 E485B576 625E7EC6 F44C42E9
			A637ED6B 0BFF5CB6 F406B7ED EE386BFB 5A899FA5 AE9F2411 7C4B1FE6
			49286651 ECE45B3D C2007CB8 A163BF05 98DA4836 1C55D39A 69163FA8
			FD24CF5F 83655D23 DCA3AD96 1C62F356 208552BB 9ED52907 7096966D
			670C354E 4ABC9804 F1746C08 CA18217C 32905E46 2E36CE3B E39E772C
			180E8603 9B2783A2 EC07A28F B5C55DF0 6F4C52C9 DE2BCBF6 95581718
			3995497C EA956AE5 15D22618 98FA0510 15728E5A 8AAAC42D AD33170D
			04507A33 A85521AB DF1CBA64 ECFB8504 58DBEF0A 8AEA7157 5D060C7D
			B3970F85 A6E1E4C7 ABF5AE8C DB0933D7 1E8C94E0 4A25619D CEE3D226
			1AD2EE6B F12FFA06 D98A0864 D8760273 3EC86A64 521F2B18 177B200C
			BBE11757 7A615D6C 770988C0 BAD946E2 08E24FA0 74E5AB31 43DB5BFC
			E0FD108E 4B82D120 A93AD2CA FFFFFFFF FFFFFFFF", "05");

		public class SampleAccountRepository : ISrpAccountRepository
		{
			public SampleAccountRepository(SrpParameters parameters = null)
			{
				// create sample user account
				var srpClient = new SrpClient(parameters);
				var salt = srpClient.GenerateSalt();
				var privateKey = srpClient.DerivePrivateKey(salt, UserName, Password);
				var verifier = srpClient.DeriveVerifier(privateKey);
				SampleAccount = new SrpAccount
				{
					UserName = UserName,
					Salt = salt,
					Verifier = verifier,
				};
			}

			private SrpAccount SampleAccount { get; set; }

			public ISrpAccount FindByName(string userName)
			{
				if (SampleAccount.UserName == userName)
				{
					return SampleAccount;
				}

				return null;
			}

			public IIdentity GetIdentity(ISrpAccount account)
			{
				return new GenericIdentity(account.UserName);
			}

			private class SrpAccount : ISrpAccount
			{
				public string UserName { get; set; }
				public string Salt { get; set; }
				public string Verifier { get; set; }
			}
		}

		/// <summary>
		/// Sample server interface.
		/// </summary>
		public interface ISampleServer
		{
			/// <summary>
			/// Returns a copy of the specified message.
			/// </summary>
			/// <param name="message">Message</param>
			/// <returns>Copy of message</returns>
			string Echo(string message);
		}

		/// <summary>
		/// Sample server implementation
		/// </summary>
		public class SampleServer : ISampleServer
		{
			/// <summary>
			/// Returns a copy of the specified message.
			/// </summary>
			/// <param name="message">Message</param>
			/// <returns>Copy of message</returns>
			public string Echo(string message)
			{
				return message;
			}
		}

		#region TCP Duplex

		/// <summary>
		/// Encapsulated server hosting environment; Designed to run in a seperate AppDomain.
		/// <remarks>
		/// The TCP Duplex Channel doesn´t support communication with client and server inside the same AppDomain.
		/// </remarks>
		/// </summary>
		public class TcpDuplexServerHostEnvironment : MarshalByRefObject, IDisposable
		{
			#region Singleton implementation

			private static TcpDuplexServerHostEnvironment _instance = null;

			public static TcpDuplexServerHostEnvironment Instance
			{
				get
				{
					if (_instance == null)
						_instance = new TcpDuplexServerHostEnvironment();

					return _instance;
				}
			}

			#endregion

			private ZyanComponentHost _host;

			private TcpDuplexServerHostEnvironment()
			{
				// use default SRP-6a parameters
				var accounts = new SampleAccountRepository();
				var provider = new SrpAuthenticationProvider(accounts);
				var protocol = new TcpDuplexServerProtocolSetup(8090, provider, true);
				_host = new ZyanComponentHost("CustomAuthenticationTestHost_TcpDuplex", protocol);
				_host.RegisterComponent<ISampleServer, SampleServer>("SampleServer", ActivationType.SingleCall);
			}

			public void Dispose()
			{
				if (_host != null)
				{
					_host.Dispose();
					_host = null;
				}
			}
		}

		/// <summary>
		/// Component for locating the singleton instance of TcpDuplexServerHostEnvironment from another AppDomain.
		/// </summary>
		public class TcpDuplexServerHostEnvironmentLocator : MarshalByRefObject
		{
			public TcpDuplexServerHostEnvironment GetServerHostEnvironment()
			{
				return TcpDuplexServerHostEnvironment.Instance;
			}
		}

		#endregion

		#region TCP Simplex

		/// <summary>
		/// Encapsulated server hosting environment; Designed to run in a seperate AppDomain.
		/// <remarks>
		/// The TCP Simplex Channel doesn´t support communication with client and server inside the same AppDomain.
		/// </remarks>
		/// </summary>
		public class TcpSimplexServerHostEnvironment : MarshalByRefObject, IDisposable
		{
			#region Singleton implementation

			private static TcpSimplexServerHostEnvironment _instance = null;

			public static TcpSimplexServerHostEnvironment Instance
			{
				get
				{
					if (_instance == null)
						_instance = new TcpSimplexServerHostEnvironment();

					return _instance;
				}
			}

			#endregion

			private ZyanComponentHost _host;

			private TcpSimplexServerHostEnvironment()
			{
				// use custom SRP parameters
				var accounts = new SampleAccountRepository(CustomSrpParameters);
				var provider = new SrpAuthenticationProvider(accounts, CustomSrpParameters);
				var protocol = new TcpCustomServerProtocolSetup(8091, provider, true);
				_host = new ZyanComponentHost("CustomAuthenticationTestHost_TcpSimplex", protocol);
				_host.RegisterComponent<ISampleServer, SampleServer>("SampleServer", ActivationType.SingleCall);
			}

			public void Dispose()
			{
				if (_host != null)
				{
					_host.Dispose();
					_host = null;
				}
			}
		}

		/// <summary>
		/// Component for locating the singleton instance of TcpSimplexServerHostEnvironment from another AppDomain.
		/// </summary>
		public class TcpSimplexServerHostEnvironmentLocator : MarshalByRefObject
		{
			public TcpSimplexServerHostEnvironment GetServerHostEnvironment()
			{
				return TcpSimplexServerHostEnvironment.Instance;
			}
		}

		#endregion

		#endregion

		#region Setup test environment and cleanup

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			StartServers(null);
		}

		[ClassCleanupNonStatic]
		public void Cleanup()
		{
			StopServer();
		}

		// Application domain for TCP Duplex test environment Zyan host
		private static AppDomain _tcpDuplexServerAppDomain = null;

		// Application domain for TCP Simplex test environment Zyan host
		private static AppDomain _tcpSimplexServerAppDomain = null;

		[ClassInitialize]
		public static void StartServers(TestContext ctx)
		{
			#region TCP Duplex

			// Setup TCP Duplex Server AppDomain
			AppDomainSetup tcpDuplexAppDomainSetup = new AppDomainSetup() { ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) };
			_tcpDuplexServerAppDomain = AppDomain.CreateDomain("RecreateClientConnectionTests_Server", null, tcpDuplexAppDomainSetup);
			_tcpDuplexServerAppDomain.Load(typeof(ZyanConnection).Assembly.GetName());

			// Start Zyan host inside the TCP Duplex Server AppDomain
			var tcpDuplexServerWork = new CrossAppDomainDelegate(() =>
			{
				var server = TcpDuplexServerHostEnvironment.Instance;

				if (server != null)
				{
					Console.WriteLine("TCP Duplex Server running.");
				}
			});
			_tcpDuplexServerAppDomain.DoCallBack(tcpDuplexServerWork);

			#endregion

			#region TCP Simplex

			// Setup TCP Simplex Server AppDomain
			AppDomainSetup tcpSimplexAppDomainSetup = new AppDomainSetup() { ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) };
			_tcpSimplexServerAppDomain = AppDomain.CreateDomain("RecreateClientConnectionTests_Server", null, tcpSimplexAppDomainSetup);
			_tcpSimplexServerAppDomain.Load(typeof(ZyanConnection).Assembly.GetName());

			// Start Zyan host inside the TCP Simplex Server AppDomain
			var tcpSimplexServerWork = new CrossAppDomainDelegate(() =>
			{
				var server = TcpSimplexServerHostEnvironment.Instance;

				if (server != null)
				{
					Console.WriteLine("TCP Simplex Server running.");
				}
			});
			_tcpSimplexServerAppDomain.DoCallBack(tcpSimplexServerWork);

			#endregion
		}

		[ClassCleanup]
		public static void StopServer()
		{
			#region TCP Duplex

			try
			{
				CrossAppDomainDelegate serverWork = new CrossAppDomainDelegate(() =>
				{
					TcpDuplexServerHostEnvironment.Instance.Dispose();
				});
				_tcpDuplexServerAppDomain.DoCallBack(serverWork);
			}
			finally
			{
				AppDomain.Unload(_tcpDuplexServerAppDomain);
			}

			#endregion

			#region TCP Simplex

			try
			{
				CrossAppDomainDelegate serverWork = new CrossAppDomainDelegate(() =>
				{
					TcpSimplexServerHostEnvironment.Instance.Dispose();
				});
				_tcpSimplexServerAppDomain.DoCallBack(serverWork);
			}
			finally
			{
				AppDomain.Unload(_tcpSimplexServerAppDomain);
			}

			#endregion
		}

		#endregion

		[TestMethod]
		public void UnknownUsernameReturnsSameSaltAndNewEphemeralOnEachRequest()
		{
			var accounts = new SampleAccountRepository();
			var authProvider = new SrpAuthenticationProvider(accounts);
			var srpClient = new SrpClient();
			var sessionId = Guid.NewGuid();
			var request = new Hashtable
			{
				{ SrpProtocolConstants.SRP_STEP_NUMBER, 1 },
				{ SrpProtocolConstants.SRP_USERNAME, "UnknownUser" },
				{ SrpProtocolConstants.SRP_CLIENT_PUBLIC_EPHEMERAL, srpClient.GenerateEphemeral().Public },
			};

			var authRequest = new AuthRequestMessage
			{
				Credentials = request,
				ClientAddress = "localhost",
				SessionID = sessionId
			};

			var response = authProvider.Authenticate(authRequest);
			Assert.IsNotNull(response);
			Assert.IsNotNull(response.Parameters);
			Assert.IsNotNull(response.Parameters[SrpProtocolConstants.SRP_SALT]);
			Assert.IsNotNull(response.Parameters[SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL]);

			Assert.IsTrue(authProvider.PendingAuthentications.Any());

			// server returns salt even for the unknown user name so we can't tell whether the user exists or not
			var salt1 = (string)response.Parameters[SrpProtocolConstants.SRP_SALT];
			var ephemeral1 = (string)response.Parameters[SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL];

			response = authProvider.Authenticate(authRequest);
			Assert.IsNotNull(response);
			Assert.IsNotNull(response.Parameters);
			Assert.IsNotNull(response.Parameters[SrpProtocolConstants.SRP_SALT]);
			Assert.IsNotNull(response.Parameters[SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL]);
			var salt2 = (string)response.Parameters[SrpProtocolConstants.SRP_SALT];
			var ephemeral2 = (string)response.Parameters[SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL];

			// server returns the same salt for every unknown username, but new ephemeral on each request
			Assert.AreEqual(salt1, salt2);
			Assert.AreNotEqual(ephemeral1, ephemeral2);
		}

		[TestMethod]
		public void AuthenticationProviderSetsAuthenticatedIdentity()
		{
			var accounts = new SampleAccountRepository();
			var authProvider = new SrpAuthenticationProvider(accounts);
			var srpClient = new SrpClient();
			var sessionId = Guid.NewGuid();
			var clientEphemeral = srpClient.GenerateEphemeral();
			var request = new Hashtable
			{
				{ SrpProtocolConstants.SRP_STEP_NUMBER, 1 },
				{ SrpProtocolConstants.SRP_USERNAME, UserName },
				{ SrpProtocolConstants.SRP_CLIENT_PUBLIC_EPHEMERAL, clientEphemeral.Public },
			};

			var authRequest = new AuthRequestMessage
			{
				Credentials = request,
				ClientAddress = "localhost",
				SessionID = sessionId
			};

			var response = authProvider.Authenticate(authRequest);
			Assert.IsNotNull(response);
			Assert.IsNotNull(response.Parameters);
			Assert.IsNotNull(response.Parameters[SrpProtocolConstants.SRP_SALT]);
			Assert.IsNotNull(response.Parameters[SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL]);

			// step1 is performed
			Assert.IsTrue(authProvider.PendingAuthentications.Any());

			// server returns salt even for the unknown user name so we can't tell whether the user exists or not
			var salt = (string)response.Parameters[SrpProtocolConstants.SRP_SALT];
			var serverPublicEphemeral = (string)response.Parameters[SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL];
			var privateKey = srpClient.DerivePrivateKey(salt, UserName, Password);
			var clientSession = srpClient.DeriveSession(clientEphemeral.Secret, serverPublicEphemeral, salt, UserName, privateKey);

			// perform step2
			authRequest.Credentials[SrpProtocolConstants.SRP_STEP_NUMBER] = 2;
			authRequest.Credentials[SrpProtocolConstants.SRP_CLIENT_SESSION_PROOF] = clientSession.Proof;

			// make sure that identity is set
			response = authProvider.Authenticate(authRequest);
			Assert.IsNotNull(response);
			Assert.IsNotNull(response.Parameters);
			Assert.IsNotNull(response.Parameters[SrpProtocolConstants.SRP_SERVER_SESSION_PROOF]);
			Assert.IsTrue(response.Completed);
			Assert.IsTrue(response.Success);
			Assert.IsNotNull(response.AuthenticatedIdentity);
			Assert.IsNotNull(response.AuthenticatedIdentity.IsAuthenticated);
			Assert.AreEqual(response.AuthenticatedIdentity.Name, UserName);

			var serverProof = (string)response.Parameters[SrpProtocolConstants.SRP_SERVER_SESSION_PROOF];
			srpClient.VerifySession(clientEphemeral.Public, clientSession, serverProof);
		}

		[TestMethod]
		public void ValidLoginUsingTcpDuplexChannel()
		{
			var url = "tcpex://localhost:8090/CustomAuthenticationTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);
			var credentials = new SrpCredentials(UserName, Password);

			using (var connection = new ZyanConnection(url, protocol, credentials, true, true))
			{
				var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
				proxy1 = null;
			}

			// reconnect using the same credentials
			using (var connection = new ZyanConnection(url, protocol, credentials, true, true))
			{
				var proxy2 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy2.Echo("Hallo"));
			}
		}

		[TestMethod, ExpectedException(typeof(SecurityException))]
		public void InvalidLoginUsingTcpDuplexChannel_NoAuthClient()
		{
			var url = "tcpex://localhost:8090/CustomAuthenticationTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);
			var credentials = new AuthCredentials(UserName, Password);

			using (var connection = new ZyanConnection(url, protocol, credentials, true, true))
			{
				var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
				proxy1 = null;
			}
		}

		[TestMethod, ExpectedException(typeof(SecurityException))]
		public void InvalidLoginUsingTcpDuplexChannel_NoCredentials()
		{
			var url = "tcpex://localhost:8090/CustomAuthenticationTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);

			using (var connection = new ZyanConnection(url, protocol))
			{
				var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
				proxy1 = null;
			}
		}

		[TestMethod]
		public void ValidLoginUsingTcpSimplexChannel()
		{
			var url = "tcp://localhost:8091/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);
			var credentials = new SrpCredentials(UserName, Password, CustomSrpParameters);

			using (var connection = new ZyanConnection(url, protocol, credentials, true, true))
			{
				var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
				proxy1 = null;
			}

			// reconnect
			using (var connection = new ZyanConnection(url, protocol, credentials, true, true))
			{
				var proxy2 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy2.Echo("Hallo"));
			}
		}

		[TestMethod]
		public void InvalidLoginUsingTcpSimplexChannel()
		{
			var url = "tcp://localhost:8091/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);
			var credentials = new SrpCredentials(UserName + "1", Password, CustomSrpParameters);

			try
			{
				new ZyanConnection(url, protocol, credentials, true, true);
			}
			catch (SecurityException ex)
			{
				Assert.AreEqual("Authentication failed: bad password or user name", ex.Message);
			}
		}

		[TestMethod]
		public void InvalidPasswordUsingTcpSimplexChannel()
		{
			var url = "tcp://localhost:8091/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);
			var credentials = new SrpCredentials(UserName, Password + "1", CustomSrpParameters);

			try
			{
				new ZyanConnection(url, protocol, credentials, true, true);
			}
			catch (SecurityException ex)
			{
				Assert.AreEqual("Authentication failed: bad password or user name", ex.Message);
			}
		}

		[TestMethod, ExpectedException(typeof(SecurityException))]
		public void InvalidLoginUsingTcpSimplexChannel_NoAuthClient()
		{
			var url = "tcp://localhost:8091/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);
			var credentials = new AuthCredentials(UserName, Password);

			using (var connection = new ZyanConnection(url, protocol, credentials, true, true))
			{
				var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
				proxy1 = null;
			}
		}

		[TestMethod, ExpectedException(typeof(SecurityException))]
		public void InvalidLoginUsingTcpSimplexChannel_NoCredentials()
		{
			var url = "tcp://localhost:8091/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);

			using (var connection = new ZyanConnection(url, protocol))
			{
				var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
				proxy1 = null;
			}
		}

		private class BrokenSrpCredentials : AuthCredentials
		{
			public override void Authenticate(Guid sessionId, IZyanDispatcher dispatcher)
			{
				dispatcher.Logon(sessionId, new Hashtable
				{
					{ SrpProtocolConstants.SRP_STEP_NUMBER, 2 },
					{ SrpProtocolConstants.SRP_CLIENT_SESSION_PROOF, "woof" },
				});
			}
		}

		[TestMethod, ExpectedException(typeof(SecurityException))]
		public void AuthenticationStep1WasNotPerformed()
		{
			var url = "tcp://localhost:8091/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);
			var credentials = new BrokenSrpCredentials();
			var conn = new ZyanConnection(url, protocol, credentials, true, true);
		}

		private class AnotherBrokenSrpCredentials : AuthCredentials
		{
			public override void Authenticate(Guid sessionId, IZyanDispatcher dispatcher)
			{
				dispatcher.Logon(sessionId, new Hashtable
				{
					{ SrpProtocolConstants.SRP_STEP_NUMBER, 3 },
				});
			}
		}

		[TestMethod, ExpectedException(typeof(SecurityException))]
		public void UnknownAuthenticationStep()
		{
			var url = "tcp://localhost:8091/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);
			var credentials = new AnotherBrokenSrpCredentials();
			var conn = new ZyanConnection(url, protocol, credentials, true, true);
		}
	}
}
