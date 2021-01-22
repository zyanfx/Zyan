using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Null;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Toolbox;

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
	/// Test class for recreate client connection problem. Issue #1514
	/// </summary>
	[TestClass]
	public class RecreateClientConnectionTests
	{
		#region Sample component classes and interfaces

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
				var protocol = new TcpDuplexServerProtocolSetup(8084, new NullAuthenticationProvider(), true);
				_host = new ZyanComponentHost("RecreateClientConnectionTestHost_TcpDuplex", protocol);
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
				var protocol = new TcpCustomServerProtocolSetup(8085, new NullAuthenticationProvider(), true);
				_host = new ZyanComponentHost("RecreateClientConnectionTestHost_TcpSimplex", protocol);
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

		#region Test methods

		[TestMethod]
		public void CreateDisposeAndRecreateConnectionUsingTcpDuplexChannel()
		{
			string url = "tcpex://localhost:8084/RecreateClientConnectionTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);

			using (var connection = new ZyanConnection(url, protocol))
			{
				var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
				proxy1 = null;
			}

			using (var connection = new ZyanConnection(url, protocol))
			{
				var proxy2 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy2.Echo("Hallo"));
			}
		}

		[TestMethod]
		public void CreateDisposeAndRecreateConnectionUsingTcpSimplexChannel()
		{
			string url = "tcp://localhost:8085/RecreateClientConnectionTestHost_TcpSimplex";
			var protocol = new TcpCustomClientProtocolSetup(true);

			using (var connection = new ZyanConnection(url, protocol))
			{
				var proxy1 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy1.Echo("Hallo"));
				proxy1 = null;
			}

			using (var connection = new ZyanConnection(url, protocol))
			{
				var proxy2 = connection.CreateProxy<ISampleServer>("SampleServer");
				Assert.AreEqual("Hallo", proxy2.Echo("Hallo"));
			}
		}

		[TestMethod]
		public void RefreshRegisteredComponentsTest()
		{
			using (var host = new ZyanComponentHost("RefreshTest", new NullServerProtocolSetup(123)))
			using (var conn = new ZyanConnection("null://NullChannel:123/RefreshTest"))
			{
				// this component is registered after connection is established
				var componentName = Guid.NewGuid().ToString();
				host.RegisterComponent<ISampleServer, SampleServer>(componentName);

				try
				{
					// proxy cannot be created because connection doesn't know about the component
					var proxy1 = conn.CreateProxy<ISampleServer>(componentName);
					Assert.Fail("Component is not yet known for ZyanConnection.");
				}
				catch (ApplicationException)
				{
				}

				// refresh the list of registered components and create a proxy
				conn.RefreshRegisteredComponents();
				var proxy2 = conn.CreateProxy<ISampleServer>(componentName);

				var echoString = "Hello there";
				var result = proxy2.Echo(echoString);
				Assert.AreEqual(echoString, result);
			}
		}

		[TestMethod, Explicit]
		public void ChannelRegistrationRaceConditionTest()
		{
			var url = "tcpex://localhost:8084/RecreateClientConnectionTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);
			var errors = new ConcurrentDictionary<Exception, Exception>();

			for (var i = 0; i < 10; i++)
			{
				ThreadPool.QueueUserWorkItem(x =>
				{
					try
					{
						using (var conn = new ZyanConnection(url, protocol))
						{
						}
					}
					catch (Exception ex)
					{
						errors[ex] = ex;
					}
				});
			}

			Thread.Sleep(100);

			Assert.DoesNotThrow(() =>
			{
				if (errors.Any())
				{
#if FX3
					throw errors.Values.First();
#else
					throw new AggregateException(errors.Values);
#endif
				}
			});
		}

#endregion
	}
}
