using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;
using Zyan.Communication.Security.SecureRemotePassword;

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
	/// Test class for SRP-6a authentication protocol classes (just a stub yet).
	/// </summary>
	[TestClass]
	public class SrpAuthenticationTests
	{
		#region Sample component classes and interfaces

		private const string UserName = "bozo";
		private const string Password = "h4ck3r";

		// custom SRP-6a parameters: prime number and generator values taken from the Clipperz.Crypto.SRP library
		private static SrpParameters CustomSrpParameters = SrpParameters.Create<SHA384>("115b8b692e0e045692cf280b436735c77a5a9e8a9e7ed56c965f87db5b2a2ece3", "05");

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
				{ SrpProtocolConstants.SRP_SESSION_ID, sessionId.ToString() },
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

			// step1 is not performed because the user is unknown
			Assert.IsFalse(authProvider.PendingAuthentications.Any());

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
		public void ValidLoginUsingTcpDuplexChannel()
		{
			var url = "tcpex://localhost:8090/CustomAuthenticationTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);
			var credentials = new SrpAuthCredentials(UserName, Password);

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
			var credentials = new SrpAuthCredentials(UserName, Password, CustomSrpParameters);

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
	}
}
