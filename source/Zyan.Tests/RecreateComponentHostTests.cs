using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Null;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;
using Zyan.Communication.SessionMgmt;

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
	public class RecreateComponentHostTests
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

		#endregion

		[TestMethod]
		public void CreateDisposeAndRecreateComponentHostForTcpDuplexChannel()
		{
			var protocol = new TcpDuplexServerProtocolSetup(8086, new NullAuthenticationProvider(), true);

			using (var host = new ZyanComponentHost("RecreateClientConnectionTestHost_TcpDuplex", protocol))
			{
				host.RegisterComponent<ISampleServer, SampleServer>("SampleServer", ActivationType.SingleCall);
			}

			using (var host = new ZyanComponentHost("RecreateClientConnectionTestHost_TcpDuplex", protocol))
			{
				host.RegisterComponent<ISampleServer, SampleServer>("SampleServer", ActivationType.SingleCall);
			}
		}

		[TestMethod]
		public void CreateDisposeAndRecreateComponentHostForTcpSimplexChannel()
		{
			var protocol = new TcpCustomServerProtocolSetup(8087, new NullAuthenticationProvider(), true);

			using (var host = new ZyanComponentHost("RecreateClientConnectionTestHost_TcpSimplex", protocol))
			{
				host.RegisterComponent<ISampleServer, SampleServer>("SampleServer", ActivationType.SingleCall);
			}

			using (var host = new ZyanComponentHost("RecreateClientConnectionTestHost_TcpSimplex", protocol))
			{
				host.RegisterComponent<ISampleServer, SampleServer>("SampleServer", ActivationType.SingleCall);
			}
		}

		[TestMethod]
		public void InvalidArgumentTests()
		{
			Assert.Throws<ArgumentException>(() => new ZyanComponentHost(null, 123, null));
			Assert.Throws<ArgumentNullException>(() => new ZyanComponentHost("A", null, default(ISessionManager)));
			Assert.Throws<ArgumentNullException>(() => new ZyanComponentHost("A", new TcpBinaryServerProtocolSetup(123), default(ISessionManager)));
			Assert.Throws<ArgumentNullException>(() => new ZyanComponentHost("A", new TcpBinaryServerProtocolSetup(123), new InProcSessionManager(), null));
		}
	}
}
