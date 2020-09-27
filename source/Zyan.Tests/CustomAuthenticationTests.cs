using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security;
using Zyan.Communication;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols.Tcp;

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
	/// Test class for custom authentication classes.
	/// </summary>
	[TestClass]
	public class CustomAuthenticationTests
	{
		#region Sample component classes and interfaces

		// Dummy authentication protocol:
		// client -> server: "Hello"
		// server <- client: "World"
		// client -> server: "Icanhaz"
		// server <- client: "Cheezburger"

		/// <summary>
		/// Server-side: authentication provider.
		/// </summary>
		public class CustomAuthenticationProvider : IAuthenticationProvider
		{
			public AuthResponseMessage Authenticate(AuthRequestMessage authRequest)
			{
				if (authRequest.Credentials == null)
					return Error("No credentials specified");

				var stepNumber = 0;
				if (authRequest.Credentials.ContainsKey("#"))
					stepNumber = (int)(authRequest.Credentials["#"]);

				var payload = string.Empty;
				if (authRequest.Credentials.ContainsKey("@"))
					payload = (string)(authRequest.Credentials["@"]);

				switch (stepNumber)
				{
					case 0: return payload == "Hello" ? Ok(false, "World") : Error("Failure on step 0");
					case 1: return payload == "Icanhaz" ? Ok(true, "Cheezburger") : Error("Failure on step 1");
				}

				return Error("Bad authentication");
			}

			private AuthResponseMessage Error(string message)
			{
				return new AuthResponseMessage { Success = false, ErrorMessage = message };
			}

			private AuthResponseMessage Ok(bool completed, string message)
			{
				return new AuthResponseMessage { Completed = completed, Success = true, ErrorMessage = message };
			}
		}

		/// <summary>
		/// Client-side: authentication client.
		/// </summary>
		public class CustomAuthenticationClient : AuthCredentials
		{
			public CustomAuthenticationClient(string greeting)
			{
				CredentialsHashtable["@"] = greeting;
			}

			public override void Authenticate(Guid sessionId, IZyanDispatcher dispatcher)
			{
				var credentials = (Hashtable)CredentialsHashtable.Clone();
				if (credentials.Count == 0)
				{
					throw new Exception("No credentials specified");
				}

				// step 0
				credentials["#"] = 0;
				var reply = dispatcher.Logon(sessionId, credentials);

				// check the reply
				var payload = reply.ErrorMessage;
				if (reply.ErrorMessage != "World")
				{
					throw new Exception("Bad reply for step 0");
				}

				// step 1
				credentials["#"] = 1;
				credentials["@"] = "Icanhaz";
				reply = dispatcher.Logon(sessionId, credentials);

				// check the reply
				if (reply.ErrorMessage != "Cheezburger")
				{
					throw new Exception("Bad reply for step 1");
				}
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
				var protocol = new TcpDuplexServerProtocolSetup(8088, new CustomAuthenticationProvider(), true);
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
				var protocol = new TcpCustomServerProtocolSetup(8089, new CustomAuthenticationProvider(), true);
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
			var url = "tcpex://localhost:8088/CustomAuthenticationTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);
			var credentials = new CustomAuthenticationClient("Hello");

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

		[TestMethod]
		public void InvalidLoginUsingTcpDuplexChannel_NoAuthClient()
		{
			var url = "tcpex://localhost:8088/CustomAuthenticationTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);
			var credentials = new Hashtable { { "@", "Hello" } };

			Assert.Throws<SecurityException>(() =>
			{
				using (var connection = new ZyanConnection(url, protocol, credentials, true, true))
				{
					var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
					Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
					proxy1 = null;
				}
			});
		}

		[TestMethod]
		public void InvalidLoginUsingTcpDuplexChannel_NoCredentials()
		{
			var url = "tcpex://localhost:8088/CustomAuthenticationTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);

			Assert.Throws<SecurityException>(() =>
			{
				using (var connection = new ZyanConnection(url, protocol))
				{
					var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
					Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
					proxy1 = null;
				}
			});
		}

		[TestMethod]
		public void ValidLoginUsingTcpSimplexChannel()
		{
			var url = "tcp://localhost:8089/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);
			var credentials = new CustomAuthenticationClient("Hello");

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

		[TestMethod]
		public void InvalidLoginUsingTcpSimplexChannel_NoAuthClient()
		{
			var url = "tcp://localhost:8089/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);
			var credentials = new Hashtable { { "@", "Hello" } };

			Assert.Throws<SecurityException>(() =>
			{
				using (var connection = new ZyanConnection(url, protocol, credentials, true, true))
				{
					var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
					Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
					proxy1 = null;
				}
			});
		}

		[TestMethod]
		public void InvalidLoginUsingTcpSimplexChannel_NoCredentials()
		{
			var url = "tcp://localhost:8089/CustomAuthenticationTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);

			Assert.Throws<SecurityException>(() =>
			{
				using (var connection = new ZyanConnection(url, protocol))
				{
					var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
					Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
					proxy1 = null;
				}
			});
		}
	}
}
