using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Http;
using Zyan.Communication.Protocols.Ipc;
using Zyan.Communication.Protocols.Null;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Protocols.Websocket;

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
		public void HttpUrlResolvesToWebsocketClientProtocolSetup()
		{
			var protocol = ClientProtocolSetup.GetClientProtocol("http://some/url");
			Assert.AreEqual(typeof(WebsocketClientProtocolSetup), protocol.GetType());
		}

		//TODO: Implement fake communication
		// [TestMethod]
		// public void NullUrlResolvesToNullClientProtocolSetup()
		// {
		// 	var protocol = ClientProtocolSetup.GetClientProtocol("null://NullChannel:1234/svc");
		// 	Assert.AreEqual(typeof(NullClientProtocolSetup), protocol.GetType());
		// }
	}
}
