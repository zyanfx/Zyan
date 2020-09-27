using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters.Binary;
using Zyan.Communication.Toolbox.Compression;
using Zyan.Communication.ChannelSinks.Compression;
using Zyan.Communication.Protocols.Tcp.DuplexChannel;

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
	using ClassCleanupNonStatic = DummyAttribute;
	using ClassInitializeNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for transport header wrapper.
	///</summary>
	[TestClass]
	public class TransportHeaderWrapperTests
	{
		[TestMethod]
		public void TransportHeadersSimpleTypesSerialization()
		{
			var th = new TransportHeaders();
			th["Hehe"] = 123;
			th["Hoho"] = DateTime.Now;
			th["Haha"] = "localhost";

			var ms = TransportHeaderWrapper.Serialize(th);
			Assert.IsNotNull(ms);

			ms.Position = 0;
			var th2 = TransportHeaderWrapper.Deserialize(ms);
			foreach (System.Collections.DictionaryEntry pair in th)
			{
				Assert.AreEqual(th[pair.Key], th2[pair.Key]);
			}
		}

		[TestMethod]
		public void TransportHeadersIPAddressSerialization()
		{
			var ip = IPAddress.Parse("192.168.254.104");
			var th = new TransportHeaders();
			th[CommonTransportKeys.IPAddress] = ip;
			th[CommonTransportKeys.RequestUri] = "localhost";

			var ms = TransportHeaderWrapper.Serialize(th);
			Assert.IsNotNull(ms);

			ms.Position = 0;
			var th2 = TransportHeaderWrapper.Deserialize(ms);
			foreach (System.Collections.DictionaryEntry pair in th)
			{
				Assert.AreEqual(th[pair.Key], th2[pair.Key]);
			}
		}
	}
}
