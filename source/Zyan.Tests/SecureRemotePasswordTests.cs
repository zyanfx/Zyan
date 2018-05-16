using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zyan.Communication.Security.SecureRemotePassword;

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
			Assert.AreEqual("<SrpInteger: 0d4c7f8a2b32c11b...>", si.ToString());
		}

		[TestMethod]
		public void SrpIntegerFromHexToHex()
		{
			var si = SrpInteger.FromHex("02");
			Assert.AreEqual("02", si.ToHex());

			// 512-bit prime number
			si = SrpInteger.FromHex("D4C7F8A2B32C11B8FBA9581EC4BA4F1B04215642EF7355E37C0FC0443EF756EA2C6B8EEB755A1C723027663CAA265EF785B8FF6A9B35227A52D86633DBDFCA43");
			Assert.AreEqual("d4c7f8a2b32c11b8fba9581ec4ba4f1b04215642ef7355e37c0fc0443ef756ea2c6b8eeb755a1c723027663caa265ef785b8ff6a9b35227a52d86633dbdfca43", si.ToHex());

			// should keep padding when going back and forth
			Assert.AreEqual("a", SrpInteger.FromHex("a").ToHex());
			Assert.AreEqual("0a", SrpInteger.FromHex("0a").ToHex());
			Assert.AreEqual("00a", SrpInteger.FromHex("00a").ToHex());
			Assert.AreEqual("000a", SrpInteger.FromHex("000a").ToHex());
			Assert.AreEqual("0000a", SrpInteger.FromHex("0000a").ToHex());
			Assert.AreEqual("00000a", SrpInteger.FromHex("00000a").ToHex());
		}

		[TestMethod]
		public void SrpIntegerAdd()
		{
			var result = SrpInteger.FromHex("353") + SrpInteger.FromHex("181");
			Assert.AreEqual("4d4", result.ToHex());
		}

		[TestMethod]
		public void SrpIntegerSubtract()
		{
			var result = SrpInteger.FromHex("5340") - SrpInteger.FromHex("5181");
			Assert.AreEqual("01bf", result.ToHex());
		}

		[TestMethod]
		public void SrpIntegerMultiply()
		{
			var result = SrpInteger.FromHex("CAFE") * SrpInteger.FromHex("babe");
			Assert.AreEqual("94133484", result.ToHex());
		}

		[TestMethod]
		public void SrpIntegerDivide()
		{
			var result = SrpInteger.FromHex("faced") / SrpInteger.FromHex("BABE");
			Assert.AreEqual("00015", result.ToHex());
		}

		[TestMethod]
		public void SrpIntegerModulo()
		{
			var result = SrpInteger.FromHex("10") % SrpInteger.FromHex("9");
			Assert.AreEqual("07", result.ToHex());
		}

		[TestMethod]
		public void SrpIntegerXor()
		{
			var left = SrpInteger.FromHex("32510bfbacfbb9befd54415da243e1695ecabd58c519cd4bd90f1fa6ea5ba47b01c909ba7696cf606ef40c04afe1ac0aa8148dd066592ded9f8774b529c7ea125d298e8883f5e9305f4b44f915cb2bd05af51373fd9b4af511039fa2d96f83414aaaf261bda2e97b170fb5cce2a53e675c154c0d9681596934777e2275b381ce2e40582afe67650b13e72287ff2270abcf73bb028932836fbdecfecee0a3b894473c1bbeb6b4913a536ce4f9b13f1efff71ea313c8661dd9a4ce");
			var right = SrpInteger.FromHex("71946f9bbb2aeadec111841a81abc300ecaa01bd8069d5cc91005e9fe4aad6e04d513e96d99de2569bc5e50eeeca709b50a8a987f4264edb6896fb537d0a716132ddc938fb0f836480e06ed0fcd6e9759f40462f9cf57f4564186a2c1778f1543efa270bda5e933421cbe88a4a52222190f471e9bd15f652b653b7071aec59a2705081ffe72651d08f822c9ed6d76e48b63ab15d0208573a7eef027");
			var xor = SrpInteger.FromHex("32510bfbacfbb9befd54415da243e1695ecabd58c519cd4bd90f1fa6ea5ba3624730b208d83b237176b5a41e13d1a2c0080f55d6fb05e4fd9a6e8aff84a9eec74ec0e3115dd0808c011baa15b2c29edad06d6c319976fc7c7eb6a8727e79906c96397dd14594a17511e2ba018c3267935877b5c2c1750f28b2d5bf55faa6c2218c30e58f17542717ad6f8622dd0069a4886d20d3d657a80a869c8f6025399f914f23e5ccd3a999c271a50994c7db959c5c0b73334d15ba3754e9");

			var result = left ^ right;
			Assert.AreEqual(xor, result);
		}

		[TestMethod]
		public void SrpIntegerEqualityChecks()
		{
			Assert.AreEqual(SrpInteger.FromHex("0"), SrpInteger.Zero);
			Assert.IsTrue(SrpInteger.FromHex("0") == SrpInteger.Zero);

			Assert.AreNotEqual(SrpInteger.FromHex("1"), SrpInteger.Zero);
			Assert.IsTrue(SrpInteger.FromHex("1") != SrpInteger.Zero);
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
		public void SrpHashComputesValidHashesUppercase()
		{
			var parts = new[] { "D4C7F8A2B32", "C11B8FBA9581EC4BA4F1B0421", "", "5642EF7355E37C0FC0443EF7", "56EA2C6B8EEB755A1C72302", "7663CAA265EF785B8FF6A9B35227A52D86633DBDFCA43" };
			var sample = string.Concat(parts);
			var srpint = SrpInteger.FromHex(sample);

			var md5 = SrpHash<MD5>.HashFunction;
			var hashmd5 = "34ada39bbabfa6e663f1aad3d7814121";
			Assert.AreEqual(hashmd5, md5(srpint.ToHex().ToUpper()));
			Assert.AreEqual(hashmd5, md5(sample));
			Assert.AreEqual(hashmd5, md5(parts));
			Assert.AreEqual(16, SrpHash<MD5>.HashSizeBytes);

			var sha256 = SrpHash<SHA256>.HashFunction;
			var hash256 = "1767fe8c94508ad3514b8332493fab5396757fe347023fc9d1fef6d26c3a70d3";
			Assert.AreEqual(hash256, sha256(srpint.ToHex().ToUpper()));
			Assert.AreEqual(hash256, sha256(sample));
			Assert.AreEqual(hash256, sha256(parts));
			Assert.AreEqual(256 / 8, SrpHash<SHA256>.HashSizeBytes);

			var sha512 = SrpHash<SHA512>.HashFunction;
			var hash512 = "f2406fd4b33b15a6b47ff78ccac7cd80eec7944092425b640d740e7dc695fdd42f583a9b4a4b98ffa5409680181999bfe319f2a3b50ddb111e8405019a8c552a";
			Assert.AreEqual(hash512, sha512(srpint.ToHex().ToUpper()));
			Assert.AreEqual(hash512, sha512(sample));
			Assert.AreEqual(hash512, sha512(parts));
			Assert.AreEqual(512 / 8, SrpHash<SHA512>.HashSizeBytes);
		}

		[TestMethod]
		public void SrpHashComputesValidHashesLowercase()
		{
			var parts = new[] { "d4c7f8a2b32", "c11b8fba9581ec4ba4f1b0421", "", "5642ef7355e37c0fc0443ef7", "56ea2c6b8eeb755a1c72302", "7663caa265ef785b8ff6a9b35227a52d86633dbdfca43" };
			var sample = string.Concat(parts);
			var srpint = SrpInteger.FromHex(sample);

			var md5 = SrpHash<MD5>.HashFunction;
			var hashmd5 = "2726861e354282d16c466ef0cb71b0fa";
			Assert.AreEqual(hashmd5, md5(srpint));
			Assert.AreEqual(hashmd5, md5(sample));
			Assert.AreEqual(hashmd5, md5(parts));
			Assert.AreEqual(16, SrpHash<MD5>.HashSizeBytes);

			var sha256 = SrpHash<SHA256>.HashFunction;
			var hash256 = "aea6c27fa2628f183f574b2803f4f21bb8cdd9defefeebb42f100aa4d22c78f7";
			Assert.AreEqual(hash256, sha256(srpint));
			Assert.AreEqual(hash256, sha256(sample));
			Assert.AreEqual(hash256, sha256(parts));
			Assert.AreEqual(256 / 8, SrpHash<SHA256>.HashSizeBytes);

			var sha512 = SrpHash<SHA512>.HashFunction;
			var hash512 = "24cca3c827f70d6f6ccd732614d55b7e6e5d746f21c8d531e5711f0e88775ddb2c29361c2ffa6aead6a8e0cac5466e8b5baa63b9e1cfaf37de02a9dcd882074f";
			Assert.AreEqual(hash512, sha512(srpint));
			Assert.AreEqual(hash512, sha512(sample));
			Assert.AreEqual(hash512, sha512(parts));
			Assert.AreEqual(512 / 8, SrpHash<SHA512>.HashSizeBytes);
		}

		[TestMethod]
		public void SrpClientGenerateSaltReturnsRandomInteger()
		{
			var salt = SrpClient.GenerateSalt();
			Assert.IsNotNull(salt);
			Assert.AreNotEqual(string.Empty, salt);
			Assert.AreEqual(SrpParameters.Default.HashSizeBytes * 2, salt.Length);
		}

		[TestMethod]
		public void SrpClientDerivesThePrivateKeyAndVerifier()
		{
			// private key derivation is deterministic for the same s, I, p
			var salt = "34ada39bbabfa6e663f1aad3d7814121";
			var privateKey = SrpClient.DerivePrivateKey(salt, "hacker@example.com", "secret");
			Assert.AreEqual("fa1975d086e4c1723d0139df3b7345e2321e5d86e4737c0e09afd7ec2a20fb59", privateKey);

			// verifier
			var verifier = SrpClient.DeriveVerifier(privateKey);
			Assert.AreEqual("32e3839d4db6871d81eb517287e99b2333403ab9af545b1c16a2169c8ea9a9ed0be65250b2168ad63836358aaf88a50153463259a3e7b0c47175b22b8a2f14f45335cfc3313d894c85d4d8361b69d033d6f4d45c0f88ad87f179c74523ee6d9bbd2a92f1e00910a7e2c053cac9cf85ef28c36736de1338618577fd6f40f2e703bd742c34c1cf587010d5dd213d09e7be0461327f5aff133400683a151cc9d2c2724aa6beb9c9a62fdc6cfb54bc83b496d38aaa6235af6d4c76d1d61628bcb33d56cace98cc31776cbff292e05368b2929fd72b87ac480db4fd3f49ce76179ee2c789319e9ff9c1d6a0302a6b7da3e38ca2158cb02b27eced069110eb93bc1881", verifier);
		}

		[TestMethod]
		public void SrpClientGeneratesEphemeralValue()
		{
			var ephemeral = SrpClient.GenerateEphemeral();
			Assert.IsNotNull(ephemeral.Public);
			Assert.AreNotEqual(string.Empty, ephemeral.Public);

			Assert.IsNotNull(ephemeral.Secret);
			Assert.AreNotEqual(string.Empty, ephemeral.Secret);

			Assert.AreNotEqual(ephemeral.Secret, ephemeral.Public);
			Assert.IsTrue(ephemeral.Secret.Length < ephemeral.Public.Length);
		}

		[TestMethod]
		public void SrpClientDeriveSession()
		{
			var clientEphemeralSecret = SrpInteger.FromHex("27b282fc8fbf8d8a5a075ff4992406ec730bc80eea2f9b89a75bb95f1272265e");
			var serverEphemeralPublic = SrpInteger.FromHex("084153f1c6374fbf166f99b870b771fbd4ce3d3455671d5ee974eae65a06d1791b263af47c7fc2b4288267b943f8c30d3c049f0627a60badb78be3708a76b7ab0d1a64235cf00e7376001e3bddaccfc90148752062e36d70a81a56d3b4446f258beb255d17bd1b3aa05bb6012ca306ab1342dcc558c66daa19d1169b7cefb6005fcd92fbc4d593f3e4fec3e356b214c89fe26508c49b11b9efa04ecf6f05a748a50464252909eca2e04c9623d0997273b28499b1ea8c42d5a022609e2a89f6906e13dd3c9142a92575424311448fdf588524a64488fb8d2fcd1a5f2b2c059515fe0c83fd499b7b3fb2fe46f42fa7fc8d72cc0c04a5c9b22ebceddebf8fac4d8e");
			var salt = SrpInteger.FromHex("d420d13b7e510e9457fb59d03819c6475fe53f680b4abb963ef9f6d4f6ddb04e");
			var username = "bozo";
			var privateKey = SrpInteger.FromHex("f8af13ffc45b3c64a826e3a133b8a74d0484e47625c049b7f635dd233cbda124");
			var clientSessionKey = SrpInteger.FromHex("b0c6a3e44d418636c4b0a8f0ff18f1f31621a703e3fae2220897b8bbc30f6e22");
			var clientSessionProof = SrpInteger.FromHex("0ad3f708a49e44a46ca392ee4f6277d5c27dbc1147082fff8ac979ce6f7be732");

			var clientSession = SrpClient.DeriveSession(clientEphemeralSecret, serverEphemeralPublic, salt, username, privateKey);
			Assert.IsNotNull(clientSession);
			Assert.AreEqual(clientSessionKey.ToHex(), clientSession.Key);
			Assert.AreEqual(clientSessionProof.ToHex(), clientSession.Proof);
		}
	}
}
