using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Http;
using Zyan.Communication.Protocols.Ipc;
using Zyan.Communication.Protocols.Null;
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
	using AssertFailedException = NUnit.Framework.AssertionException;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for the default client protocols.
	///</summary>
	[TestClass]
	public class DefaultProtocolTests
	{
		[TestMethod]
		public void HttpUrlResolvesToHttpCustomProtocolSetup()
		{
			var protocol = ClientProtocolSetup.GetClientProtocol("http://some/url");
			Assert.AreEqual(typeof(HttpCustomClientProtocolSetup), protocol.GetType());
		}

		[TestMethod]
		public void IpcUrlResolvesToIpcBinaryProtocolSetup()
		{
			var protocol = ClientProtocolSetup.GetClientProtocol("ipc://some/url");
			Assert.AreEqual(typeof(IpcBinaryClientProtocolSetup), protocol.GetType());
		}

		[TestMethod]
		public void NullUrlResolvesToNullClientProtocolSetup()
		{
			var protocol = ClientProtocolSetup.GetClientProtocol("null://NullChannel:1234/svc");
			Assert.AreEqual(typeof(NullClientProtocolSetup), protocol.GetType());
		}

		[TestMethod]
		public void TcpUrlResolvesToTcpCustomProtocolSetup()
		{
			var protocol = ClientProtocolSetup.GetClientProtocol("tcp://localhost:1234/svc");
			Assert.AreEqual(typeof(TcpCustomClientProtocolSetup), protocol.GetType());
		}

		[TestMethod]
		public void TcpexUrlResolvesToTcpDuplexProtocolSetup()
		{
			var protocol = ClientProtocolSetup.GetClientProtocol("tcpex://localhost:1234/svc");
			Assert.AreEqual(typeof(TcpDuplexClientProtocolSetup), protocol.GetType());
		}
	}
}
