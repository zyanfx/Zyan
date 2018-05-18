using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security;
using Zyan.Communication;
using Zyan.Communication.Security;
using Zyan.Communication.Security.SecureRemotePassword;
using Zyan.Communication.Protocols.Tcp;

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
	/// Test class for SRP-6a authentication protocol classes (just a stub yet).
	/// </summary>
	[TestClass]
	public class SrpAuthenticationTests
	{
		#region Sample component classes and interfaces

		private const string UserName = "bozo";
		private const string Password = "h4ck3r";

		public class SampleAccountRepository : ISrpAccountRepository
		{
			public SampleAccountRepository()
			{
				// create sample user account
				var srpClient = new SrpClient();
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

			private class SrpAccount : ISrpAccount
			{
				public string UserName { get; set; }
				public string Salt { get; set; }
				public string Verifier { get; set; }
			}
		}

		/// <summary>
		/// Server-side: authentication provider.
		/// </summary>
		public class SrpAuthenticationProvider : IAuthenticationProvider
		{
			public SrpAuthenticationProvider(ISrpAccountRepository repository)
			{
				AuthRepository = repository;
			}

			private ISrpAccountRepository AuthRepository { get; set; }

			private SrpServer SrpServer { get; set; } = new SrpServer();

			// step1 variables
			private string UserName { get; set; }

			private string Salt { get; set; }

			private string Verifier { get; set; }

			private string ClientEphemeralPublic { get; set; }

			private SrpEphemeral ServerEphemeral { get; set; }

			public AuthResponseMessage Authenticate(AuthRequestMessage authRequest)
			{
				if (authRequest.Credentials == null)
					return Error("No credentials specified");

				if (!authRequest.Credentials.ContainsKey(SrpProtocolConstants.SRP_STEP_NUMBER))
					return Error("Authentication step not specified");

				// step number
				var step = Convert.ToInt32(authRequest.Credentials[SrpProtocolConstants.SRP_STEP_NUMBER]);

				// first step never fails: User -> Host: I, A = g^a (identifies self, a = random number)
				if (step == 1)
				{
					UserName = (string)authRequest.Credentials[SrpProtocolConstants.SRP_USERNAME];
					ClientEphemeralPublic = (string)authRequest.Credentials[SrpProtocolConstants.SRP_CLIENT_PUBLIC_EPHEMERAL];
					var account = AuthRepository.FindByName(UserName);
					if (account != null)
					{
						// Host -> User: s, B = kv + g^b (sends salt, b = random number)
						Salt = account.Salt;
						Verifier = account.Verifier;
						ServerEphemeral = SrpServer.GenerateEphemeral(Verifier);
						return ResponseStep1(Salt, ServerEphemeral.Public);
					}

					// generate fake salt and B values so that attacker cannot tell whether the given user exists or not
					var fakeSalt = new SrpParameters().H(UserName).ToHex();
					var fakeEphemeral = SrpServer.GenerateEphemeral(fakeSalt);
					return ResponseStep1(fakeSalt, fakeEphemeral.Public);
				}

				// second step may fail: User -> Host: M = H(H(N) xor H(g), H(I), s, A, B, K)
				if (step == 2)
				{
					try
					{
						// Host -> User: H(A, M, K)
						var clientSessionProof = (string)authRequest.Credentials[SrpProtocolConstants.SRP_CLIENT_SESSION_PROOF];
						var serverSession = SrpServer.DeriveSession(ServerEphemeral.Secret, ClientEphemeralPublic, Salt, UserName, Verifier, clientSessionProof);
						return ResponseStep2(serverSession.Proof);
					}
					catch (SecurityException ex)
					{
						return Error("Authentication failed: " + ex.Message);
					}
				}

				// step number should be either 1 or 2
				return Error("Unknown authentication step");
			}

			private AuthResponseMessage ResponseStep1(string salt, string serverPublicEphemeral)
			{
				var result = new AuthResponseMessage { Completed = false, Success = true };
				result.AddParameter(SrpProtocolConstants.SRP_SALT, salt);
				result.AddParameter(SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL, serverPublicEphemeral);
				return result;
			}

			private AuthResponseMessage ResponseStep2(string serverSessionProof)
			{
				var result = new AuthResponseMessage { Completed = true, Success = true };
				result.AddParameter(SrpProtocolConstants.SRP_SERVER_SESSION_PROOF, serverSessionProof);
				return result;
			}

			private AuthResponseMessage Error(string message) => new AuthResponseMessage
			{
				Success = false,
				Completed = true,
				ErrorMessage = message,
			};
		}

		/// <summary>
		/// Client-side: authentication client.
		/// </summary>
		public class SrpAuthenticationClient : AuthCredentials
		{
			public SrpAuthenticationClient(string userName, string password)
			{
				UserName = userName;
				Password = password;
			}

			private SrpClient SrpClient { get; set; } = new SrpClient();

			public override void Authenticate(Guid sessionId, IZyanDispatcher dispatcher)
			{
				// step1: User -> Host: I, A = g^a (identifies self, a = random number)
				var clientEphemeral = SrpClient.GenerateEphemeral();
				var request1 = new Hashtable
				{
					{ SrpProtocolConstants.SRP_STEP_NUMBER, 1 },
					{ SrpProtocolConstants.SRP_SESSION_ID, sessionId.ToString() },
					{ SrpProtocolConstants.SRP_USERNAME, UserName },
					{ SrpProtocolConstants.SRP_CLIENT_PUBLIC_EPHEMERAL, clientEphemeral.Public },
				};

				// Host -> User: s, B = kv + g^b (sends salt, b = random number)
				var response1 = dispatcher.Logon(sessionId, request1).Parameters;
				var salt = (string)response1[SrpProtocolConstants.SRP_SALT];
				var serverPublicEphemeral = (string)response1[SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL];

				// step2: User -> Host: M = H(H(N) xor H(g), H(I), s, A, B, K)
				var privateKey = SrpClient.DerivePrivateKey(salt, UserName, Password);
				var clientSession = SrpClient.DeriveSession(clientEphemeral.Secret, serverPublicEphemeral, salt, UserName, privateKey);
				var request2 = new Hashtable
				{
					{ SrpProtocolConstants.SRP_STEP_NUMBER, 2 },
					{ SrpProtocolConstants.SRP_SESSION_ID, sessionId.ToString() },
					{ SrpProtocolConstants.SRP_CLIENT_SESSION_PROOF, clientSession.Proof },
				};

				// Host -> User: H(A, M, K)
				var response2 = dispatcher.Logon(sessionId, request2).Parameters;
				var serverSessionProof = (string)response2[SrpProtocolConstants.SRP_SERVER_SESSION_PROOF];
				SrpClient.VerifySession(clientEphemeral.Public, clientSession, serverSessionProof);
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
				var provider = new SrpAuthenticationProvider(new SampleAccountRepository());
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
				var provider = new SrpAuthenticationProvider(new SampleAccountRepository());
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
		public void ValidLoginUsingTcpDuplexChannel()
		{
			var url = "tcpex://localhost:8090/CustomAuthenticationTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);
			var credentials = new SrpAuthenticationClient("bozo", "h4ck3r");

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
			var credentials = new AuthCredentials("bozo", "h4ck3r");

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
			var credentials = new SrpAuthenticationClient("bozo", "h4ck3r");

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

		[TestMethod, ExpectedException(typeof(SecurityException))]
		public void InvalidLoginUsingTcpSimplexChannel_NoAuthClient()
		{
			var url = "tcp://localhost:8091/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);
			var credentials = new AuthCredentials("bozo", "h4ck3r");

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
	}
}
