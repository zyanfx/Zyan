using System;
using Zyan.Communication;
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
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;

#endif
	#endregion

	/// <summary>
	/// Test class for protocol URL formatting and validation.
	///</summary>
	[TestClass]
	public class ProtocolUrlTests
	{
		[TestMethod]
		public void FormatUrlTests()
		{
			Assert.AreEqual("http://sharp-shooter.ru:80/zyan", new HttpCustomClientProtocolSetup().FormatUrl("sharp-shooter.ru", 80, "zyan"));
			Assert.AreEqual("tcpex://localhost:12356/CoolService", new TcpDuplexClientProtocolSetup().FormatUrl("localhost", 12356, "CoolService"));
			Assert.AreEqual("tcp://example.com:88/ExampleService", new TcpCustomClientProtocolSetup().FormatUrl("example.com", 88, "ExampleService"));
			Assert.AreEqual("tcp://zyan.com.de:21/AnotherService", new TcpBinaryClientProtocolSetup().FormatUrl("zyan.com.de", 21, "AnotherService"));
			Assert.AreEqual("ipc://AnyValidFilename/ServiceName", new IpcBinaryClientProtocolSetup().FormatUrl("AnyValidFilename", "ServiceName"));
			Assert.AreEqual("null://NullChannel:1234/NullServer", new NullClientProtocolSetup().FormatUrl(1234, "NullServer"));
		}

		[TestMethod]
		public void HttpUrlValidation()
		{
			var protocol = new HttpCustomClientProtocolSetup();
			Assert.IsTrue(protocol.IsUrlValid("http://localhost:123/server"));
			Assert.IsTrue(protocol.IsUrlValid("http://www.example.com:8080/server"));
			Assert.IsTrue(protocol.IsUrlValid("http://127.0.0.1:8888/index"));
			Assert.IsTrue(protocol.IsUrlValid("http://[FEDC:BA98:7654:3210:FEDC:BA98:7654:3210]:80/index"));
			Assert.IsFalse(protocol.IsUrlValid(null));
			Assert.IsFalse(protocol.IsUrlValid(string.Empty));
			Assert.IsFalse(protocol.IsUrlValid("http://"));
			Assert.IsFalse(protocol.IsUrlValid("http://host/server"));
		}

		[TestMethod]
		public void TcpDuplexUrlValidation()
		{
			var protocol = new TcpDuplexClientProtocolSetup();
			Assert.IsTrue(protocol.IsUrlValid("tcpex://localhost:123/server"));
			Assert.IsTrue(protocol.IsUrlValid("tcpex://www.example.com:8080/server"));
			Assert.IsTrue(protocol.IsUrlValid("tcpex://127.0.0.1:8888/index"));
			Assert.IsTrue(protocol.IsUrlValid("tcpex://[FEDC:BA98:7654:3210:FEDC:BA98:7654:3210]:80/index"));
			Assert.IsFalse(protocol.IsUrlValid(null));
			Assert.IsFalse(protocol.IsUrlValid(string.Empty));
			Assert.IsFalse(protocol.IsUrlValid("tcpex://"));
			Assert.IsFalse(protocol.IsUrlValid("tcpex://host/server"));
		}

		[TestMethod]
		public void TcpBinaryUrlValidation()
		{
			var protocol = new TcpBinaryClientProtocolSetup();
			Assert.IsTrue(protocol.IsUrlValid("tcp://localhost:123/server"));
			Assert.IsTrue(protocol.IsUrlValid("tcp://www.example.com:8080/server"));
			Assert.IsTrue(protocol.IsUrlValid("tcp://127.0.0.1:8888/index"));
			Assert.IsTrue(protocol.IsUrlValid("tcp://[FEDC:BA98:7654:3210:FEDC:BA98:7654:3210]:80/index"));
			Assert.IsFalse(protocol.IsUrlValid(null));
			Assert.IsFalse(protocol.IsUrlValid(string.Empty));
			Assert.IsFalse(protocol.IsUrlValid("tcp://"));
			Assert.IsFalse(protocol.IsUrlValid("tcp://host/server"));
		}

		[TestMethod]
		public void TcpCustomUrlValidation()
		{
			var protocol = new TcpCustomClientProtocolSetup();
			Assert.IsTrue(protocol.IsUrlValid("tcp://localhost:123/server"));
			Assert.IsTrue(protocol.IsUrlValid("tcp://www.example.com:8080/server"));
			Assert.IsTrue(protocol.IsUrlValid("tcp://127.0.0.1:8888/index"));
			Assert.IsTrue(protocol.IsUrlValid("tcp://[FEDC:BA98:7654:3210:FEDC:BA98:7654:3210]:80/index"));
			Assert.IsFalse(protocol.IsUrlValid(null));
			Assert.IsFalse(protocol.IsUrlValid(string.Empty));
			Assert.IsFalse(protocol.IsUrlValid("tcp://"));
			Assert.IsFalse(protocol.IsUrlValid("tcp://host/server"));
		}

		[TestMethod]
		public void IpcUrlValidation()
		{
			var protocol = new IpcBinaryClientProtocolSetup();
			Assert.IsTrue(protocol.IsUrlValid("ipc://portName/serviceName"));
			Assert.IsFalse(protocol.IsUrlValid(null));
			Assert.IsFalse(protocol.IsUrlValid(string.Empty));
			Assert.IsFalse(protocol.IsUrlValid("ipc://"));
		}

		[TestMethod]
		public void NullUrlValidation()
		{
			var protocol = new NullClientProtocolSetup();
			Assert.IsTrue(protocol.IsUrlValid("null://NullChannel:1234/server"));
			Assert.IsFalse(protocol.IsUrlValid(null));
			Assert.IsFalse(protocol.IsUrlValid(string.Empty));
			Assert.IsFalse(protocol.IsUrlValid("null://"));
			Assert.IsFalse(protocol.IsUrlValid("null://NullChannel:/server"));
		}

		[TestMethod, ExpectedException(typeof(ArgumentException))]
		public void ZyanConnectionDoesntAcceptMalformedUrl()
		{
			using (new ZyanConnection("hello", new IpcBinaryClientProtocolSetup()))
			{
				Assert.Fail("'hello' is not a valid url for the Ipc protocol.");
			}
		}
	}
}
