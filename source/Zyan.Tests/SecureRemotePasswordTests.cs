using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using Zyan.Communication.Toolbox.Compression;
using Zyan.Communication.ChannelSinks.Compression;
using Zyan.Communication.Protocols.Wrapper;
using Zyan.Communication.Security.SecureRemotePassword;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	/// Test class for SecureRemotePassword-related classes.
	///</summary>
	[TestClass]
	public class SecureRemotePasswordTests
	{
		[TestMethod]
		public void SrpIntegerToString()
		{
			var si = new SrpInteger("2");
			Assert.AreEqual("<SrpInteger: 2>", si.ToString());

			// 512-bit prime number
			si = new SrpInteger("D4C7F8A2B32C11B8FBA9581EC4BA4F1B04215642EF7355E37C0FC0443EF756EA2C6B8EEB755A1C723027663CAA265EF785B8FF6A9B35227A52D86633DBDFCA43");
			Assert.AreEqual("<SrpInteger: 0D4C7F8A2B32C11B...>", si.ToString());
		}

		[TestMethod]
		public void SrpIntegerFromHexToHex()
		{
			var si = SrpInteger.FromHex("02");
			Assert.AreEqual("02", si.ToHex());

			// 512-bit prime number
			si = SrpInteger.FromHex("D4C7F8A2B32C11B8FBA9581EC4BA4F1B04215642EF7355E37C0FC0443EF756EA2C6B8EEB755A1C723027663CAA265EF785B8FF6A9B35227A52D86633DBDFCA43");
			Assert.AreEqual("D4C7F8A2B32C11B8FBA9581EC4BA4F1B04215642EF7355E37C0FC0443EF756EA2C6B8EEB755A1C723027663CAA265EF785B8FF6A9B35227A52D86633DBDFCA43", si.ToHex());

			// should keep padding when going back and forth
			Assert.AreEqual("A", SrpInteger.FromHex("A").ToHex());
			Assert.AreEqual("0A", SrpInteger.FromHex("0A").ToHex());
			Assert.AreEqual("00A", SrpInteger.FromHex("00A").ToHex());
			Assert.AreEqual("000A", SrpInteger.FromHex("000A").ToHex());
			Assert.AreEqual("0000A", SrpInteger.FromHex("0000A").ToHex());
			Assert.AreEqual("00000A", SrpInteger.FromHex("00000A").ToHex());
		}

		[TestMethod]
		public void SrpIntegerImplicitStringConversion()
		{
			var si = SrpInteger.FromHex("02");
			string sistr = si;
			Assert.AreEqual(sistr, "02");

			si = SrpInteger.FromHex("000000000000");
			sistr = si;
			Assert.AreEqual(sistr, "000000000000");
		}

		[TestMethod]
		public void RandomIntegerReturnsAnIntegerOfTheGivenSize()
		{
			var rnd = SrpInteger.RandomInteger(1);
			Assert.AreEqual(2, rnd.ToHex().Length);
			Assert.AreNotEqual("00", rnd.ToHex());

			rnd = SrpInteger.RandomInteger(8);
			Assert.AreEqual(16, rnd.ToHex().Length);
			Assert.AreNotEqual("0000000000000000", rnd.ToHex());
		}

		[TestMethod]
		public void SrpHashComputesValidHashes()
		{
			var parts = new[] { "D4C7F8A2B32", "C11B8FBA9581EC4BA4F1B0421", "", "5642EF7355E37C0FC0443EF7", "56EA2C6B8EEB755A1C72302", "7663CAA265EF785B8FF6A9B35227A52D86633DBDFCA43" };
			var sample = string.Concat(parts);
			var srpint = SrpInteger.FromHex(sample);

			var md5 = SrpHash<MD5>.HashFunction;
			var hashmd5 = "34ADA39BBABFA6E663F1AAD3D7814121";
			Assert.AreEqual(hashmd5, md5(srpint));
			Assert.AreEqual(hashmd5, md5(sample));
			Assert.AreEqual(hashmd5, md5(parts));
			Assert.AreEqual(16, SrpHash<MD5>.HashSizeBytes);

			var sha256 = SrpHash<SHA256>.HashFunction;
			var hash256 = "1767FE8C94508AD3514B8332493FAB5396757FE347023FC9D1FEF6D26C3A70D3";
			Assert.AreEqual(hash256, sha256(srpint));
			Assert.AreEqual(hash256, sha256(sample));
			Assert.AreEqual(hash256, sha256(parts));
			Assert.AreEqual(256 / 8, SrpHash<SHA256>.HashSizeBytes);

			var sha512 = SrpHash<SHA512>.HashFunction;
			var hash512 = "F2406FD4B33B15A6B47FF78CCAC7CD80EEC7944092425B640D740E7DC695FDD42F583A9B4A4B98FFA5409680181999BFE319F2A3B50DDB111E8405019A8C552A";
			Assert.AreEqual(hash512, sha512(srpint));
			Assert.AreEqual(hash512, sha512(sample));
			Assert.AreEqual(hash512, sha512(parts));
			Assert.AreEqual(512 / 8, SrpHash<SHA512>.HashSizeBytes);
		}
	}
}
