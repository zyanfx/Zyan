using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Discovery.Metadata;

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
	using TestInitialize = NUnit.Framework.SetUpAttribute;
	using TestCleanup = NUnit.Framework.TearDownAttribute;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for strong-typed call interceptor builder.
	/// </summary>
	[TestClass]
	public class DiscoveryMetadataTests
	{
		[TestMethod]
		public void EscapeUnescapeTests()
		{
			var source = @"Something\ wonderful| is h@ppening";
			var escaped = DiscoveryMetadataHelper.Escape(source);
			Assert.AreEqual(@"Something\s wonderful\p is h\appening", escaped);

			var unescaped = DiscoveryMetadataHelper.Unescape(escaped);
			Assert.AreEqual(source, unescaped);
		}

		[TestMethod]
		public void EscapeUnescapeNulls()
		{
			var escaped = DiscoveryMetadataHelper.Escape(null);
			Assert.AreEqual("@", escaped);

			var unescaped = DiscoveryMetadataHelper.Unescape("@");
			Assert.IsNull(unescaped);

			unescaped = DiscoveryMetadataHelper.Unescape(null);
			Assert.IsNull(unescaped);
		}

		[TestMethod]
		public void EscapeUnescapeAnotherTest()
		{
			var source = @"\|\\d|a|\\s\s\\p|@@\fd|\pp||\\\p\p";
			var escaped = DiscoveryMetadataHelper.Escape(source);
			Assert.AreNotEqual(source, escaped);

			var unescaped = DiscoveryMetadataHelper.Unescape(escaped);
			Assert.AreEqual(source, unescaped);
		}

		private class SamplePacket : DiscoveryMetadata
		{
			public SamplePacket()
				: base("Sample")
			{
				Version = null;
				ServiceUri = null;
			}

			public string Version
			{
				get { return Properties["{D398DFB5-2B0E-4A82-8114-704A97F2CF04}"]; }
				set { Properties["{D398DFB5-2B0E-4A82-8114-704A97F2CF04}"] = value; }
			}

			public string ServiceUri
			{
				get { return Properties["{252954A7-D0A4-45BA-9D24-21C416D07321}"]; }
				set { Properties["{252954A7-D0A4-45BA-9D24-21C416D07321}"] = value; }
			}
		}

		[TestMethod]
		public void EncodeSampleDiscoveryPacket()
		{
			var dp = new SamplePacket();
			dp.Version = "1.0";
			dp.Properties["Name"] = "Dummy";

			var packet = DiscoveryMetadataHelper.Encode(dp);
			Assert.IsNotNull(packet);
			Assert.IsTrue(packet.Length == 110);
		}

		[TestMethod]
		public void DecodeSampleDiscoveryPacket()
		{
			var dp = new SamplePacket();
			dp.Version = "EDC35F23-60AD-4730-A9AC-23D57B666DC1";
			dp.ServiceUri = @"tcp://msdn.microsoft.com/en-us/library/tst0kwb1(v=vs.110).aspx|Something=Else|#5235@#$%\a\p\|@@";
			dp.Properties["ExtraProperty"] = @"#%@$%45654!@#$\4214\|Shame=Value";
			DiscoveryMetadataHelper.RegisterDiscoveryMetadataFactory(dp.Signature, () => new SamplePacket());

			var packet = DiscoveryMetadataHelper.Encode(dp);
			var decoded = DiscoveryMetadataHelper.Decode(packet);

			Assert.IsNotNull(decoded);
			Assert.AreEqual(decoded.GetType(), typeof(SamplePacket));
			Assert.AreEqual(dp, decoded);
		}

		[TestMethod]
		public void EncodeDecodeDiscoveryRequest()
		{
			var request = new DiscoveryRequest("SomeService");
			var encoded = DiscoveryMetadataHelper.Encode(request);
			var decoded = DiscoveryMetadataHelper.Decode(encoded);

			Assert.IsNotNull(decoded);
			Assert.AreEqual(request, decoded);
		}

		[TestMethod]
		public void EncodeDecodeDiscoveryResponse()
		{
			var response = new DiscoveryResponse("tcpex://some:123/SomeService", "1.0");
			var encoded = DiscoveryMetadataHelper.Encode(response);
			var decoded = DiscoveryMetadataHelper.Decode(encoded);

			Assert.IsNotNull(decoded);
			Assert.AreEqual(response, decoded);
		}

		[TestMethod]
		public void RequestResponseMatch()
		{
			var req = new DiscoveryRequest("UltimaService");
			var rsp = new DiscoveryResponse("tcp://localhost:123/UltimaService");
			Assert.IsTrue(rsp.Matches(req));

			req = new DiscoveryRequest("tcp://.*/Ultima.*$");
			Assert.IsTrue(rsp.Matches(req));

			rsp = new DiscoveryResponse("tcpex://localhost:321/Ultima");
			Assert.IsFalse(rsp.Matches(req));

			// invalid regular expression patterns
			req = new DiscoveryRequest(@"\c");
			Assert.IsFalse(rsp.Matches(req));

			req = new DiscoveryRequest(@")");
			Assert.IsFalse(rsp.Matches(req));
		}
	}
}
