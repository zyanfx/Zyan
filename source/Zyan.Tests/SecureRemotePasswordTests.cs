using System.Linq;
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
		public void SrpIntegerToByteArrayConversion()
		{
			var si = SrpInteger.FromHex("02");
			var arr = new byte[] { 0x02 };
			Assert.IsTrue(Enumerable.SequenceEqual(arr, si.ToByteArray()));

			si = SrpInteger.FromHex("01F2C3A4B506");
			arr = new byte[] { 0x01, 0xF2, 0xC3, 0xA4, 0xB5, 0x06 };
			Assert.IsTrue(Enumerable.SequenceEqual(arr, si.ToByteArray()));

			si = SrpInteger.FromHex("ed3250071433e544b62b5dd0341564825a697357b5379f07aabca795a4e0a109");
			arr = new byte[] { 0xed, 0x32, 0x50, 0x07, 0x14, 0x33, 0xe5, 0x44, 0xb6, 0x2b, 0x5d, 0xd0, 0x34, 0x15, 0x64, 0x82, 0x5a, 0x69, 0x73, 0x57, 0xb5, 0x37, 0x9f, 0x07, 0xaa, 0xbc, 0xa7, 0x95, 0xa4, 0xe0, 0xa1, 0x09 };
			Assert.IsTrue(Enumerable.SequenceEqual(arr, si.ToByteArray()));
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
		public void SrpHashComputesValidStringHashes()
		{
			var parts = new[] { "D4C7F8A2B32", "C11B8FBA9581EC4BA4F1B0421", "", "5642EF7355E37C0FC0443EF7", "56EA2C6B8EEB755A1C72302", "7663CAA265EF785B8FF6A9B35227A52D86633DBDFCA43" };
			var sample = string.Concat(parts);
			var srpint = SrpInteger.FromHex(sample);

			var md5 = SrpHash<MD5>.HashFunction;
			var hashmd5 = SrpInteger.FromHex("34ada39bbabfa6e663f1aad3d7814121");
			Assert.AreEqual(hashmd5, md5(srpint.ToHex().ToUpper()));
			Assert.AreEqual(hashmd5, md5(sample));
			Assert.AreEqual(hashmd5, md5(parts));
			Assert.AreEqual(16, SrpHash<MD5>.HashSizeBytes);

			var sha256 = SrpHash<SHA256>.HashFunction;
			var hash256 = SrpInteger.FromHex("1767fe8c94508ad3514b8332493fab5396757fe347023fc9d1fef6d26c3a70d3");
			Assert.AreEqual(hash256, sha256(srpint.ToHex().ToUpper()));
			Assert.AreEqual(hash256, sha256(sample));
			Assert.AreEqual(hash256, sha256(parts));
			Assert.AreEqual(256 / 8, SrpHash<SHA256>.HashSizeBytes);

			var sha512 = SrpHash<SHA512>.HashFunction;
			var hash512 = SrpInteger.FromHex("f2406fd4b33b15a6b47ff78ccac7cd80eec7944092425b640d740e7dc695fdd42f583a9b4a4b98ffa5409680181999bfe319f2a3b50ddb111e8405019a8c552a");
			Assert.AreEqual(hash512, sha512(srpint.ToHex().ToUpper()));
			Assert.AreEqual(hash512, sha512(sample));
			Assert.AreEqual(hash512, sha512(parts));
			Assert.AreEqual(512 / 8, SrpHash<SHA512>.HashSizeBytes);
		}

		[TestMethod]
		public void SrpHashComputesValidSrpIntegerHashes()
		{
			var parts = new[] { "Hello", " ", "world!" };
			var sample = string.Concat(parts);
			var srpint = SrpInteger.FromHex("48 65 6C 6C 6F 20 77 6F 72 6c 64 21");

			var md5 = SrpHash<MD5>.HashFunction;
			var hashmd5 = SrpInteger.FromHex("86FB269D190D2C85F6E0468CECA42A20");
			Assert.AreEqual(hashmd5, md5(srpint));
			Assert.AreEqual(hashmd5, md5(sample));
			Assert.AreEqual(hashmd5, md5(parts));
			Assert.AreEqual(16, SrpHash<MD5>.HashSizeBytes);

			var sha256 = SrpHash<SHA256>.HashFunction;
			var hash256 = SrpInteger.FromHex("C0535E4BE2B79FFD93291305436BF889314E4A3FAEC05ECFFCBB7DF31AD9E51A");
			Assert.AreEqual(hash256, sha256(srpint));
			Assert.AreEqual(hash256, sha256(sample));
			Assert.AreEqual(hash256, sha256(parts));
			Assert.AreEqual(256 / 8, SrpHash<SHA256>.HashSizeBytes);

			var sha512 = SrpHash<SHA512>.HashFunction;
			var hash512 = SrpInteger.FromHex("F6CDE2A0F819314CDDE55FC227D8D7DAE3D28CC556222A0A8AD66D91CCAD4AAD6094F517A2182360C9AACF6A3DC323162CB6FD8CDFFEDB0FE038F55E85FFB5B6");
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
			// validate intermediate steps
			var userName = "hacker@example.com";
			var password = "secret";
			var H = SrpHash<SHA256>.HashFunction;
			var step1 = H($"{userName}:{password}");
			Assert.AreEqual(SrpInteger.FromHex("ed3250071433e544b62b5dd0341564825a697357b5379f07aabca795a4e0a109"), step1);

			// step1.1
			var salt = "34ada39bbabfa6e663f1aad3d7814121";
			var s = SrpInteger.FromHex(salt);
			var step11 = H(s);
			Assert.AreEqual(SrpInteger.FromHex("a5acfc1292e1b8e171b7c9a0f7b5bcd9bbcd4a3485c18d9d4fcf4480e8573442"), step11);

			// step1.2
			var step12 = H(step1);
			Assert.AreEqual(SrpInteger.FromHex("446d597ddf0e65ca0395926665e70f10a2b0f8194f633243e71359028895be6f"), step12);

			// step2
			var step2 = H(s, step1);
			Assert.AreEqual(SrpInteger.FromHex("e2db59181003e48e326292b3b307a1173a5f1fd12c6ffde55f7289503065fd6c"), step2);

			// private key derivation is deterministic for the same s, I, p
			var privateKey = SrpClient.DerivePrivateKey(salt, userName, password);
			Assert.AreEqual("e2db59181003e48e326292b3b307a1173a5f1fd12c6ffde55f7289503065fd6c", privateKey);

			// verifier
			var verifier = SrpClient.DeriveVerifier(privateKey);
			Assert.AreEqual("622dad56d6c282a949f9d2702941a9866b7dd277af92a6e538b2d7cca42583275a2f4b64bd61369a24b23170223faf212e2f3bdddc529204c61055687c4162aa2cd0fd41ced0186406b8a6dda4802fa941c54f5115ca69953a8e265210349a4cb89dda3febc96c86df08a87823235ff6c87a170cc1618f38ec493e758e2cac4c04db3dcdac8656458296dbcc3599fc1f66cde1e62e477dd1696c65dbeb413d8ed832adc7304e68566b46a7849126eea62c95d5561306f76fe1f8a77a3bd85db85e6b0262064d665890ff46170f96ce403a9b485abe387e91ca85e3522d6276e2fff41754d57a71dee6da62aea614725da100631efd7442cf68a294001d8134e9", verifier);
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
			var clientEphemeralSecret = SrpInteger.FromHex("27b282fc8fbf8d8a5a075ff4992406ec730bc80eea2f9b89a75bb95f1272265e").ToHex();
			var serverEphemeralPublic = SrpInteger.FromHex("084153f1c6374fbf166f99b870b771fbd4ce3d3455671d5ee974eae65a06d1791b263af47c7fc2b4288267b943f8c30d3c049f0627a60badb78be3708a76b7ab0d1a64235cf00e7376001e3bddaccfc90148752062e36d70a81a56d3b4446f258beb255d17bd1b3aa05bb6012ca306ab1342dcc558c66daa19d1169b7cefb6005fcd92fbc4d593f3e4fec3e356b214c89fe26508c49b11b9efa04ecf6f05a748a50464252909eca2e04c9623d0997273b28499b1ea8c42d5a022609e2a89f6906e13dd3c9142a92575424311448fdf588524a64488fb8d2fcd1a5f2b2c059515fe0c83fd499b7b3fb2fe46f42fa7fc8d72cc0c04a5c9b22ebceddebf8fac4d8e").ToHex();
			var salt = SrpInteger.FromHex("d420d13b7e510e9457fb59d03819c6475fe53f680b4abb963ef9f6d4f6ddb04e").ToHex();
			var username = "bozo";
			var privateKey = SrpInteger.FromHex("f8af13ffc45b3c64a826e3a133b8a74d0484e47625c049b7f635dd233cbda124").ToHex();
			var clientSessionKey = SrpInteger.FromHex("b0c6a3e44d418636c4b0a8f0ff18f1f31621a703e3fae2220897b8bbc30f6e22");
			var clientSessionProof = SrpInteger.FromHex("0ad3f708a49e44a46ca392ee4f6277d5c27dbc1147082fff8ac979ce6f7be732");

			var clientSession = SrpClient.DeriveSession(clientEphemeralSecret, serverEphemeralPublic, salt, username, privateKey);
			Assert.IsNotNull(clientSession);
			Assert.AreEqual(clientSessionKey.ToHex(), clientSession.Key);
			Assert.AreEqual(clientSessionProof.ToHex(), clientSession.Proof);
		}
	}
}
