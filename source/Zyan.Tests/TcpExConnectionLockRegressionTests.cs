using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// Test class for TcpEx GetConnection lock issue. Issue #85
	/// </summary>
	[TestClass]
	public class TcpExConnectionLockRegressionTests
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

		/// <summary>
		/// Encapsulated server hosting environment; Designed to run in a separate AppDomain.
		/// <remarks>
		/// The TCP Duplex Channel doesn´t support communication with client and server inside the same AppDomain.
		/// </remarks>
		/// </summary>
		public class TcpDuplexServerHostEnvironment : MarshalByRefObject, IDisposable
		{
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

			private ZyanComponentHost _host;

			private TcpDuplexServerHostEnvironment()
			{
				var protocol = new TcpDuplexServerProtocolSetup(8092, new NullAuthenticationProvider(), true);
				_host = new ZyanComponentHost("TcpExConnectionLockRegressionTestHost_TcpDuplex", protocol);
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

		[ClassInitialize]
		public static void StartServers(TestContext ctx)
		{
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
		}

		[ClassCleanup]
		public static void StopServer()
		{
			Trace.WriteLine("** Stopping the server! **");

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
		}

		#endregion

		[TestMethod]
		public void CreateDisposeAndRecreateConnectionUsingTcpDuplexChannel()
		{
			var url = "tcpex://localhost:8092/TcpExConnectionLockRegressionTestHost_TcpDuplex";
			var protocol = new TcpDuplexClientProtocolSetup(true);
			var lessThanTimeout = 10;

			using (var connection = new ZyanConnection(url, protocol))
			{
				var sw = Stopwatch.StartNew();
				var proxy = connection.CreateProxy<ISampleServer>("SampleServer");
				var badConnectionInitiated = false;

				for (var i = 0; i < 100; i++)
				{
					var echo = "Hi" + i;
					Assert.AreEqual(echo, proxy.Echo(echo));
					Thread.Sleep(10);

					// make sure that proxy is already created and runs
					if (i == 3)
					{
						Trace.WriteLine("** Spawning a thread for another tcpex connection **");
						ThreadPool.QueueUserWorkItem(x =>
						{
							badConnectionInitiated = true;

							try
							{
								// this connection should time out in more than lessThanTimeout seconds
								new ZyanConnection(url.Replace("localhost", "example.com"), protocol);
							}
							catch (Exception ex)
							{
								// this exception is expected
								Trace.WriteLine("** Connection failed as expected **:" + ex.Message);
							}
						});
					}
				}

				sw.Stop();

				Assert.IsTrue(badConnectionInitiated);
				Assert.IsTrue(sw.Elapsed.TotalSeconds < lessThanTimeout);
				Trace.WriteLine("** CreateDisposeAndRecreateConnectionUsingTcpDuplexChannel test finished **");
			}
		}
	}
}
