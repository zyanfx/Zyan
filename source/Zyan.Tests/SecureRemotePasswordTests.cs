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
		public void SrpIntegerNegativeNumbers()
		{
			var srp = SrpInteger.FromHex("-f34");
			Assert.AreEqual("-f34", srp.ToHex());

			srp = 0x19 - SrpInteger.FromHex("face");
			Assert.AreEqual("-fab5", srp.ToHex());
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
			Assert.IsTrue(0 == SrpInteger.Zero);
			Assert.IsTrue(SrpInteger.Zero == 0);
			Assert.IsTrue(0L == SrpInteger.Zero);
			Assert.IsTrue(SrpInteger.Zero == 0L);

			Assert.AreNotEqual(SrpInteger.FromHex("1"), SrpInteger.Zero);
			Assert.IsTrue(SrpInteger.FromHex("1") != SrpInteger.Zero);
			Assert.IsTrue(1 != SrpInteger.Zero);
			Assert.IsTrue(SrpInteger.Zero != 1);
			Assert.IsTrue(1L != SrpInteger.Zero);
			Assert.IsTrue(SrpInteger.Zero != 1L);
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

		[TestMethod]
		public void SrpClientDeriveSessionRegressionTest()
		{
			var clientEphemeralSecret = "72dac3f6f7ade13135e234d9d3c4899453418c929af72c4171ffdc920fcf2535";
			var serverEphemeralPublic = "1139bdcab77770878d8cb72536a4368f315897e36cdcbfe603971f70be6190500b064d3202fa4a57bb8aa25fb2fba871fa66fb59183e17f8513ec2746e6193143f3c439512d243b2c0b92cbf671a2ed5712d2ef6f190840e7e1bf6b2480c837fc7f3b8f6e4b27f25b7af96a0197a21c175c0e067164151c151f7c68190fc8b7e10b45055e4bc18a4abf07e6f9a02d3be916b2783c474d7babef10867abf12370455b65749ed35dcd376addf3dad8a156a49a306b13041e3a4795654384faec21a19c40c429e5629b92e8925fb7f7a62d925cb99a15c06b41d7c50d1b7c38a05dea2ed5a14c5657de29f2864b1535f6eedd6ff6746a5b4d1521e101481a342e4f";
			var salt = "532ec0e523a7b19db660f00eb00e91f033697f0ab58a542c99be8e9a08f48d6e";
			var username = "linus@folkdatorn.se";
			var privateKey = "79c7aadce96da2387b01a48ce5b9e910eb3f9e1ac0f8574b314c3f0fe8106f08";
			var clientSessionKey = "1080138ae545b14fe946f6d6b08e9eebdd8fcd2184bc513a5c1cc4c789b038f3";
			var clientSessionProof = "c1cabf00aec302c97c233d1c50ab733b44d16482be5f6c65419081add8e576e1";

			var clientSession = SrpClient.DeriveSession(clientEphemeralSecret, serverEphemeralPublic, salt, username, privateKey);
			Assert.IsNotNull(clientSession);
			Assert.AreEqual(clientSessionKey, clientSession.Key);
			Assert.AreEqual(clientSessionProof, clientSession.Proof);
		}

		[TestMethod]
		public void SrpServerGeneratesEphemeralValue()
		{
			var verifier = "622dad56d6c282a949f9d2702941a9866b7dd277af92a6e538b2d7cca42583275a2f4b64bd61369a24b23170223faf212e2f3bdddc529204c61055687c4162aa2cd0fd41ced0186406b8a6dda4802fa941c54f5115ca69953a8e265210349a4cb89dda3febc96c86df08a87823235ff6c87a170cc1618f38ec493e758e2cac4c04db3dcdac8656458296dbcc3599fc1f66cde1e62e477dd1696c65dbeb413d8ed832adc7304e68566b46a7849126eea62c95d5561306f76fe1f8a77a3bd85db85e6b0262064d665890ff46170f96ce403a9b485abe387e91ca85e3522d6276e2fff41754d57a71dee6da62aea614725da100631efd7442cf68a294001d8134e9";
			var ephemeral = SrpServer.GenerateEphemeral(verifier);
			Assert.IsNotNull(ephemeral.Public);
			Assert.AreNotEqual(string.Empty, ephemeral.Public);

			Assert.IsNotNull(ephemeral.Secret);
			Assert.AreNotEqual(string.Empty, ephemeral.Secret);

			Assert.AreNotEqual(ephemeral.Secret, ephemeral.Public);
			Assert.IsTrue(ephemeral.Secret.Length < ephemeral.Public.Length);
		}

		[TestMethod]
		public void SrpServerDeriveSession()
		{
			var serverSecretEphemeral = "e252dc34cc300c5f330ae3c684bf1b1657f5b1ca694bbfbba14829bb16e5638c";
			var clientPublicEphemeral = "278f74f97e2dcdf886769ce31c87513a4a73548762a29b2db0188757fdccf066393ed79305d946e80b6e5d963771d62475566cb2ce0883076c8846d8f961d9396ffcba54447879772b4b8a43de258662e52407bb7f0f6397a8402173f69e4a306aed850b9df89fc78ddbc72d76aa6b0e99555e8b08a21b4d91c6cd86cee4c2117a54a0a58ae0a7f6f0c8699cf0709e9ac7ba009c2e304b3e8559d76d3b3a27c016f2647a3f4ba3f94494a4a61d799d9fda67000331976f8e1b6f5b68504cadfd9a48fa5dc73ef39b7e7ad07338a7fc7bd82777bd7ad2a7b7abcbbcbfa50e0e949b2a5726fe30361298b3981e620fb57f0c58684b5b24ad317f18b288474b10d8";
			var salt = "d420d13b7e510e9457fb59d03819c6475fe53f680b4abb963ef9f6d4f6ddb04e";
			var username = "bozo";
			var verifier = "a9f253f5da8b0ec3ea2fdf01ae497799ff2fb3b4b2c2c488b01c9beeeed543a9de3c7014d05b4014e0986dda96c9f416d90c858a7483740845f0f6cd5a6eef1b140d1b46bb37f5bcfbb28127bf84f9b7f5c0d5cc4329cb7b166ff45375becdfe941664167903fb0fc9c035ee5b3cb5411a34b91e2f9b0dcc5310bf1b6c514ac63a15eb811bb652a65f96e105079942a5c7d21724910c1c2a2615ea1ceeddcc879c05658e6efd75db15250300080680875d4e31054dc508d446db31e2683724c785e7651fdf26faea054479ce95ea2443e6464ba1f53b62e7eaa8e21075a082a7ed6d937be65e835bacaa37d45651baf202601506e6246a2a183e178acc50bbd5";
			var clientSessionProof = "b541f4b8f5b259362b2a4984900d950c1486205bba4fd7a8837a995d16af44b9";
			var serverSessionKey = "b5ef4d6a5fb1d56f4efe99212cffd858fcdca100907f61f962a751588e2cf564";
			var serverSessionProof = "3f2718e7295c6cd54e35e3d4aed541daf799d4941e7dae87d2caa817651c5774";

			var serverSession = SrpServer.DeriveSession(serverSecretEphemeral, clientPublicEphemeral, salt, username, verifier, clientSessionProof);
			Assert.IsNotNull(serverSession);
			Assert.AreEqual(serverSessionKey, serverSession.Key);
			Assert.AreEqual(serverSessionProof, serverSession.Proof);
		}

		//[TestMethod]
		//public void SrpServerDeriveSessionRegressionTest()
		//{
		//	var serverSecretEphemeral = "10586d81ccecdce05f7a6ad2ed205b7f5615f84463fdcf584bfec2f288fad5f5";
		//	var clientPublicEphemeral = "5bff094e878aa7aefb777fe78067a75d459223e58d2d41ea810017fee3e8b0fdd7085d94ce0db7935dcb81e78d14c7e8a3dcacad4c2d6aa29c23724fab4303131ef8e9f3ed13ccd2414be43e851abf6713060699d94137fda38b59e524dbc2caebc7e3cd388e14abed4e3e9e6e25744b708a4c6ee79a84009b81b1a2e69ba0c926856b0e1858597239ad230aa0b95070968833f357613d9dd69bd30a1450af284adea261eb383cf9c3ae1e992ed8382527e8d680c20b54ad46e24c55998a784fd55f4c37a64562cd8beee0f9f3ee607d7bf4199e05c37129364ab0daf9c4768070a54c5ed125184a56d659d05f8b6b66ede56da1f82f48ee3d272370edd876ff";
		//	var salt = "4ea524accbfee7a2ba67301422b7c8ba4ce205a68bb8bfc36e32fab005c9f4f4";
		//	var username = "linus@folkdatorn.se";
		//	var verifier = "2052387840d2a36b5da0a0b74d1b4c5f3216003a00977681b2bad3b4b6005fcee73fcc644106018bcd090afc50455cbde18194b1ef34be4a44418624cd6a0b974cd7a890c9115bbe0f538806c2016b4db5b9dd8bd5f7e2819720c2e4a42479a06297eee9d8acb9326b49a9a16358b7fdd75ce20e7b03993f13f17747a5ea4c02b3b116632bcd34f1da265704a43d074845373b6fc528a858abb07c4ab162a8f30847628f19bc26149d43ecf7570c10463b2a3e886665cb3af7d186a209a4b8d9b85f6ba9c23852311856011e642633fde3bfd48cf43c2f54070b3340408d4f615e536f4bf1656b794d5bee861bb28f16c55e36025ebf3421db0f51682e03e2ea";
		//	var clientSessionProof = "f33540a50cee1cc1c55b3292253ed4f8a2509dce58b6a7d0c906daf44e98a962";
		//	var serverSessionKey = "?";
		//	var serverSessionProof = "?";

		//	var serverSession = SrpServer.DeriveSession(serverSecretEphemeral, clientPublicEphemeral, salt, username, verifier, clientSessionProof);
		//	Assert.IsNotNull(serverSession);
		//	Assert.AreEqual(serverSessionKey, serverSession.Key);
		//	Assert.AreEqual(serverSessionProof, serverSession.Proof);
		//}

		[TestMethod]
		public void SrpClientVerifiesSession()
		{
			var clientEphemeralPublic = "30fca5854c2391faa219fd863487c31f2591f5ba9988ce5129319906929ff2d23bc4e24c3f36f6ed12034111881ca705b033edfb782a1714e0f4d892f17c7d8432a1089c311c3170848bba0a0f64930d3f097c670b08384f1641a73833edaf9d1493744e655043df0d68f0c18a1571cc1c07c41ad817b57c262f48dde991d413628c0f3fa1de55afcf2d87e994c7f6e25c07cf1a803d41f555158997cd8703da68a48e54598b5b4947cc661d5c0138a5ecaa55996d5d6b566578f9de3b1ca1e128ff223c290595252497835646b9f8c0e330f4d6a3e61f31ff3eb8e305f563cb112ca90942e770f94cd02396041ab4c47e0c58675ded8bb0026640f9723b4d67";
			var clientSessionKey = "0bb4c696fd6f240fa0b268f3ce267044b05d620ac5f9871d21e4f89a3b0ac841";
			var clientSessionProof = "50a240e5b5f4d0db633e147d92a32aa0c9451e5d0508bded623b40200d237eef";
			var serverSessionProof = "a06d7fe3d45542f993c39b145ea3a0e3f5d6943373af35af355bb82692d692e8";
			var clientSession = new SrpSession
			{
				Key = clientSessionKey,
				Proof = clientSessionProof,
			};

			SrpClient.VerifySession(clientEphemeralPublic, clientSession, serverSessionProof);
		}

		[TestMethod]
		public void SrpShouldAuthenticateAUser()
		{
			// https://github.com/LinusU/secure-remote-password/blob/master/test.js
			var username = "linus@folkdatorn.se";
			var password = "$uper$ecure";

			// sign up
			var salt = SrpClient.GenerateSalt();
			var privateKey = SrpClient.DerivePrivateKey(salt, username, password);
			var verifier = SrpClient.DeriveVerifier(privateKey);

			// authenticate
			var clientEphemeral = SrpClient.GenerateEphemeral();
			var serverEphemeral = SrpServer.GenerateEphemeral(verifier);
			var clientSession = SrpClient.DeriveSession(clientEphemeral.Secret, serverEphemeral.Public, salt, username, privateKey);
			var serverSession = SrpServer.DeriveSession(serverEphemeral.Secret, clientEphemeral.Public, salt, username, verifier, clientSession.Proof);
			SrpClient.VerifySession(clientEphemeral.Public, clientSession, serverSession.Proof);

			// make sure both the client and the server have the same session key
			Assert.AreEqual(clientSession.Key, serverSession.Key);
		}
	}
}
