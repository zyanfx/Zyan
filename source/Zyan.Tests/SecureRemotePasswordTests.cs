using System;
using System.Linq;
using System.Security.Cryptography;
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
	/// Test class for SRP-6a protocol implementation.
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
		public void SrpIntegerNormalizedLength()
		{
			var hex = SrpInteger.FromHex(@"
				7E273DE8 696FFC4F 4E337D05 B4B375BE B0DDE156 9E8FA00A 9886D812
				9BADA1F1 822223CA 1A605B53 0E379BA4 729FDC59 F105B478 7E5186F5
				C671085A 1447B52A 48CF1970 B4FB6F84 00BBF4CE BFBB1681 52E08AB5
				EA53D15C 1AFF87B2 B9DA6E04 E058AD51 CC72BFC9 033B564E 26480D78
				E955A5E2 9E7AB245 DB2BE315 E2099AFB").ToHex();

			Assert.IsTrue(hex.StartsWith("7e27") && hex.EndsWith("9afb"));
			Assert.AreEqual(256, hex.Length);
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
		public void SrpIntegetModPowCompatibleWithJsbn()
		{
			// jsbn results:
			//  5 ^ 3 % 1000 =  <SRPInteger 7d>  = 125
			// -5 ^ 3 % 1000 =  <SRPInteger f83> = 0x1000-0x7d
			// 5 ^ 33 % 1000 =  <SRPInteger 2bd> = 701
			//-5 ^ 33 % 1000 =  <SRPInteger d43> = 0x1000-0x2bd,
			// 5 ^ 90 % 1000 =  <SRPInteger dc1>
			//-5 ^ 90 % 1000 =  <SRPInteger dc1>

			var p5 = SrpInteger.FromHex("5");
			var n5 = SrpInteger.FromHex("-5");
			var x3 = SrpInteger.FromHex("3");
			var x33 = SrpInteger.FromHex("33");
			var x90 = SrpInteger.FromHex("90");
			var m = SrpInteger.FromHex("1000");

			var result = p5.ModPow(x3, m);
			Assert.AreEqual("007d", result.ToHex());

			result = p5.ModPow(x33, m);
			Assert.AreEqual("02bd", result.ToHex());

			result = p5.ModPow(x90, m);
			Assert.AreEqual("0dc1", result.ToHex());

			result = n5.ModPow(x3, m);
			Assert.AreEqual("0f83", result.ToHex());

			result = n5.ModPow(x33, m);
			Assert.AreEqual("0d43", result.ToHex());

			result = n5.ModPow(x90, m);
			Assert.AreEqual("0dc1", result.ToHex());
		}

		[TestMethod]
		public void SrpIntegerModPowRegressionTest()
		{
			var p = new SrpParameters();
			var g = p.G;
			var N = p.N;

			var a = SrpInteger.FromHex("64e1124e73967bb4806cf5e3f151c574d0012147255e10fca02e9b4bafc8f4ba");
			var A = g.ModPow(a, N);

			Assert.AreEqual("07be00c7e6aa8198eddc42cc2f251901f3bc05795fefd5f40f90f0a6bfe66743954ef18ece62d229095a704197be18c0d1ca3a280381c8a53b42173df36867c29c564e8c974cf4ff4718547d27bd9c08eb9a909fb984e8e23a109eaf4f57a337c9cbe1609e35b9fddbc9f847825b1c37167cb3f10b3b284a7370323818571e6369e91b4ac6f6eedcdbc1c7d8d57b2020d43be7fec3df14a120c76d27ebabc8d93cdc555362a4c7c08a1052e67647e9f3f879846389672e7a5d6e1ff93940d4196bef451e8d6a3b410a5062ac29cee3783e9a5aeac9724ad1375a2189c3b5a8dbf671dfad990132d2e5b73eb5a2e3d2034b6b908210f5fe61272b2cf4d1e3a4aa", A.ToHex());
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

			si = new SrpInteger("B0", 10);
			arr = new byte[] { 0, 0, 0, 0, 0xb0 };
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

			var md5 = new SrpHash<MD5>().HashFunction;
			var hashmd5 = SrpInteger.FromHex("34ada39bbabfa6e663f1aad3d7814121");
			Assert.AreEqual(hashmd5, md5(srpint.ToHex().ToUpper()));
			Assert.AreEqual(hashmd5, md5(sample));
			Assert.AreEqual(hashmd5, md5(parts));
			Assert.AreEqual(16, new SrpHash<MD5>().HashSizeBytes);

			var sha256 = new SrpHash<SHA256>().HashFunction;
			var hash256 = SrpInteger.FromHex("1767fe8c94508ad3514b8332493fab5396757fe347023fc9d1fef6d26c3a70d3");
			Assert.AreEqual(hash256, sha256(srpint.ToHex().ToUpper()));
			Assert.AreEqual(hash256, sha256(sample));
			Assert.AreEqual(hash256, sha256(parts));
			Assert.AreEqual(256 / 8, new SrpHash<SHA256>().HashSizeBytes);

			var sha512 = new SrpHash<SHA512>().HashFunction;
			var hash512 = SrpInteger.FromHex("f2406fd4b33b15a6b47ff78ccac7cd80eec7944092425b640d740e7dc695fdd42f583a9b4a4b98ffa5409680181999bfe319f2a3b50ddb111e8405019a8c552a");
			Assert.AreEqual(hash512, sha512(srpint.ToHex().ToUpper()));
			Assert.AreEqual(hash512, sha512(sample));
			Assert.AreEqual(hash512, sha512(parts));
			Assert.AreEqual(512 / 8, new SrpHash<SHA512>().HashSizeBytes);
		}

		[TestMethod]
		public void SrpHashComputesValidSrpIntegerHashes()
		{
			var parts = new[] { "Hello", " ", "world!" };
			var sample = string.Concat(parts);
			var srpint = SrpInteger.FromHex("48 65 6C 6C 6F 20 77 6F 72 6c 64 21");

			var md5 = new SrpHash<MD5>().HashFunction;
			var hashmd5 = SrpInteger.FromHex("86FB269D190D2C85F6E0468CECA42A20");
			Assert.AreEqual(hashmd5, md5(srpint));
			Assert.AreEqual(hashmd5, md5(sample));
			Assert.AreEqual(hashmd5, md5(parts));
			Assert.AreEqual(16, new SrpHash<MD5>().HashSizeBytes);

			var sha256 = new SrpHash<SHA256>().HashFunction;
			var hash256 = SrpInteger.FromHex("C0535E4BE2B79FFD93291305436BF889314E4A3FAEC05ECFFCBB7DF31AD9E51A");
			Assert.AreEqual(hash256, sha256(srpint));
			Assert.AreEqual(hash256, sha256(sample));
			Assert.AreEqual(hash256, sha256(parts));
			Assert.AreEqual(256 / 8, new SrpHash<SHA256>().HashSizeBytes);

			var sha512 = new SrpHash<SHA512>().HashFunction;
			var hash512 = SrpInteger.FromHex("F6CDE2A0F819314CDDE55FC227D8D7DAE3D28CC556222A0A8AD66D91CCAD4AAD6094F517A2182360C9AACF6A3DC323162CB6FD8CDFFEDB0FE038F55E85FFB5B6");
			Assert.AreEqual(hash512, sha512(srpint));
			Assert.AreEqual(hash512, sha512(sample));
			Assert.AreEqual(hash512, sha512(parts));
			Assert.AreEqual(512 / 8, new SrpHash<SHA512>().HashSizeBytes);
		}

		[TestMethod]
		public void SrpClientGenerateSaltReturnsRandomInteger()
		{
			var salt = new SrpClient().GenerateSalt();
			Assert.IsNotNull(salt);
			Assert.AreNotEqual(string.Empty, salt);
			Assert.AreEqual(new SrpParameters().HashSizeBytes * 2, salt.Length);
		}

		[TestMethod]
		public void SrpClientDerivesThePrivateKeyAndVerifier()
		{
			// validate intermediate steps
			var userName = "hacker@example.com";
			var password = "secret";
			var H = new SrpHash<SHA256>().HashFunction;
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
			var privateKey = new SrpClient().DerivePrivateKey(salt, userName, password);
			Assert.AreEqual("e2db59181003e48e326292b3b307a1173a5f1fd12c6ffde55f7289503065fd6c", privateKey);

			// verifier
			var verifier = new SrpClient().DeriveVerifier(privateKey);
			Assert.AreEqual("622dad56d6c282a949f9d2702941a9866b7dd277af92a6e538b2d7cca42583275a2f4b64bd61369a24b23170223faf212e2f3bdddc529204c61055687c4162aa2cd0fd41ced0186406b8a6dda4802fa941c54f5115ca69953a8e265210349a4cb89dda3febc96c86df08a87823235ff6c87a170cc1618f38ec493e758e2cac4c04db3dcdac8656458296dbcc3599fc1f66cde1e62e477dd1696c65dbeb413d8ed832adc7304e68566b46a7849126eea62c95d5561306f76fe1f8a77a3bd85db85e6b0262064d665890ff46170f96ce403a9b485abe387e91ca85e3522d6276e2fff41754d57a71dee6da62aea614725da100631efd7442cf68a294001d8134e9", verifier);
		}

		[TestMethod]
		public void SrpClientGeneratesEphemeralValue()
		{
			var ephemeral = new SrpClient().GenerateEphemeral();
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
			var clientEphemeralSecret = "27b282fc8fbf8d8a5a075ff4992406ec730bc80eea2f9b89a75bb95f1272265e";
			var serverEphemeralPublic = "084153f1c6374fbf166f99b870b771fbd4ce3d3455671d5ee974eae65a06d1791b263af47c7fc2b4288267b943f8c30d3c049f0627a60badb78be3708a76b7ab0d1a64235cf00e7376001e3bddaccfc90148752062e36d70a81a56d3b4446f258beb255d17bd1b3aa05bb6012ca306ab1342dcc558c66daa19d1169b7cefb6005fcd92fbc4d593f3e4fec3e356b214c89fe26508c49b11b9efa04ecf6f05a748a50464252909eca2e04c9623d0997273b28499b1ea8c42d5a022609e2a89f6906e13dd3c9142a92575424311448fdf588524a64488fb8d2fcd1a5f2b2c059515fe0c83fd499b7b3fb2fe46f42fa7fc8d72cc0c04a5c9b22ebceddebf8fac4d8e";
			var salt = "d420d13b7e510e9457fb59d03819c6475fe53f680b4abb963ef9f6d4f6ddb04e";
			var username = "bozo";
			var privateKey = "f8af13ffc45b3c64a826e3a133b8a74d0484e47625c049b7f635dd233cbda124";
			var clientSessionKey = "52121d4c5d029b91bd856fe373bdf7cd81c7c48727eb8d765959518b9eda20a7";
			var clientSessionProof = "96340088aec5717eb66b88e3a47c70865756970f48876ab4c8ca6ea359a70e2d";

			var clientSession = new SrpClient().DeriveSession(clientEphemeralSecret, serverEphemeralPublic, salt, username, privateKey);
			Assert.IsNotNull(clientSession);
			Assert.AreEqual(clientSessionKey, clientSession.Key);
			Assert.AreEqual(clientSessionProof, clientSession.Proof);
		}

		[TestMethod]
		public void SrpClientDeriveSessionRegressionTest()
		{
			var clientEphemeralSecret = "72dac3f6f7ade13135e234d9d3c4899453418c929af72c4171ffdc920fcf2535";
			var serverEphemeralPublic = "1139bdcab77770878d8cb72536a4368f315897e36cdcbfe603971f70be6190500b064d3202fa4a57bb8aa25fb2fba871fa66fb59183e17f8513ec2746e6193143f3c439512d243b2c0b92cbf671a2ed5712d2ef6f190840e7e1bf6b2480c837fc7f3b8f6e4b27f25b7af96a0197a21c175c0e067164151c151f7c68190fc8b7e10b45055e4bc18a4abf07e6f9a02d3be916b2783c474d7babef10867abf12370455b65749ed35dcd376addf3dad8a156a49a306b13041e3a4795654384faec21a19c40c429e5629b92e8925fb7f7a62d925cb99a15c06b41d7c50d1b7c38a05dea2ed5a14c5657de29f2864b1535f6eedd6ff6746a5b4d1521e101481a342e4f";
			var salt = "532ec0e523a7b19db660f00eb00e91f033697f0ab58a542c99be8e9a08f48d6e";
			var username = "linus@folkdatorn.se";
			var privateKey = "79c7aadce96da2387b01a48ce5b9e910eb3f9e1ac0f8574b314c3f0fe8106f08";
			var clientSessionKey = "39be93f466aeea2de0a498600c546969eaeebbf015690bd6cefe624ddaf5c383";
			var clientSessionProof = "2410ed11831f58d7522f088f089e3d68fa2eaf4f0510913764f50f0e31e8c471";

			var clientSession = new SrpClient().DeriveSession(clientEphemeralSecret, serverEphemeralPublic, salt, username, privateKey);
			Assert.IsNotNull(clientSession);
			Assert.AreEqual(clientSessionKey, clientSession.Key);
			Assert.AreEqual(clientSessionProof, clientSession.Proof);
		}

		[TestMethod]
		public void SrpServerGeneratesEphemeralValue()
		{
			var verifier = "622dad56d6c282a949f9d2702941a9866b7dd277af92a6e538b2d7cca42583275a2f4b64bd61369a24b23170223faf212e2f3bdddc529204c61055687c4162aa2cd0fd41ced0186406b8a6dda4802fa941c54f5115ca69953a8e265210349a4cb89dda3febc96c86df08a87823235ff6c87a170cc1618f38ec493e758e2cac4c04db3dcdac8656458296dbcc3599fc1f66cde1e62e477dd1696c65dbeb413d8ed832adc7304e68566b46a7849126eea62c95d5561306f76fe1f8a77a3bd85db85e6b0262064d665890ff46170f96ce403a9b485abe387e91ca85e3522d6276e2fff41754d57a71dee6da62aea614725da100631efd7442cf68a294001d8134e9";
			var ephemeral = new SrpServer().GenerateEphemeral(verifier);
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
			var clientSessionProof = "63f0ae40f93cce889c08dc143e2535d8b0797920cdd29484e77aec010827692a";
			var serverSessionKey = "7de5394ade704c03b2ac22011b6b66fba7280dc7ce8a9c07d28af762bc5f07cc";
			var serverSessionProof = "75b9ed3883ecc9bc01b6eeebd953b94179ed0e8816810f7bcc140786929289b0";

			var serverSession = new SrpServer().DeriveSession(serverSecretEphemeral, clientPublicEphemeral, salt, username, verifier, clientSessionProof);
			Assert.IsNotNull(serverSession);
			Assert.AreEqual(serverSessionKey, serverSession.Key);
			Assert.AreEqual(serverSessionProof, serverSession.Proof);
		}

		[TestMethod]
		public void SrpServerDeriveSessionRegressionTest1()
		{
			var serverSecretEphemeral = "10586d81ccecdce05f7a6ad2ed205b7f5615f84463fdcf584bfec2f288fad5f5";
			var clientPublicEphemeral = "5bff094e878aa7aefb777fe78067a75d459223e58d2d41ea810017fee3e8b0fdd7085d94ce0db7935dcb81e78d14c7e8a3dcacad4c2d6aa29c23724fab4303131ef8e9f3ed13ccd2414be43e851abf6713060699d94137fda38b59e524dbc2caebc7e3cd388e14abed4e3e9e6e25744b708a4c6ee79a84009b81b1a2e69ba0c926856b0e1858597239ad230aa0b95070968833f357613d9dd69bd30a1450af284adea261eb383cf9c3ae1e992ed8382527e8d680c20b54ad46e24c55998a784fd55f4c37a64562cd8beee0f9f3ee607d7bf4199e05c37129364ab0daf9c4768070a54c5ed125184a56d659d05f8b6b66ede56da1f82f48ee3d272370edd876ff";
			var salt = "4ea524accbfee7a2ba67301422b7c8ba4ce205a68bb8bfc36e32fab005c9f4f4";
			var username = "linus@folkdatorn.se";
			var verifier = "2052387840d2a36b5da0a0b74d1b4c5f3216003a00977681b2bad3b4b6005fcee73fcc644106018bcd090afc50455cbde18194b1ef34be4a44418624cd6a0b974cd7a890c9115bbe0f538806c2016b4db5b9dd8bd5f7e2819720c2e4a42479a06297eee9d8acb9326b49a9a16358b7fdd75ce20e7b03993f13f17747a5ea4c02b3b116632bcd34f1da265704a43d074845373b6fc528a858abb07c4ab162a8f30847628f19bc26149d43ecf7570c10463b2a3e886665cb3af7d186a209a4b8d9b85f6ba9c23852311856011e642633fde3bfd48cf43c2f54070b3340408d4f615e536f4bf1656b794d5bee861bb28f16c55e36025ebf3421db0f51682e03e2ea";
			var clientSessionProof = "6842a3726f5b3452983f5eb20cbf244d67a8269d558cb4d11dab6cfbe9908097";
			var serverSessionKey = "389c0b233952136feaeb68816b6a759d31deb80e8a86696969acf939df9f0688";
			var serverSessionProof = "2420ad80c3eec1d6568fb9112198b20d4b576f4457a3cb1a10df85ecf670c466";

			var serverSession = new SrpServer().DeriveSession(serverSecretEphemeral, clientPublicEphemeral, salt, username, verifier, clientSessionProof);
			Assert.IsNotNull(serverSession);
			Assert.AreEqual(serverSessionKey, serverSession.Key);
			Assert.AreEqual(serverSessionProof, serverSession.Proof);
		}

		[TestMethod]
		public void SrpServerDeriveSessionRegressionTest2()
		{
			// regression test:
			var parameters = new SrpParameters();
			var clientEphemeral = new SrpEphemeral
			{
				Secret = "64e1124e73967bb4806cf5e3f151c574d0012147255e10fca02e9b4bafc8f4ba",
				Public = "07be00c7e6aa8198eddc42cc2f251901f3bc05795fefd5f40f90f0a6bfe66743954ef18ece62d229095a704197be18c0d1ca3a280381c8a53b42173df36867c29c564e8c974cf4ff4718547d27bd9c08eb9a909fb984e8e23a109eaf4f57a337c9cbe1609e35b9fddbc9f847825b1c37167cb3f10b3b284a7370323818571e6369e91b4ac6f6eedcdbc1c7d8d57b2020d43be7fec3df14a120c76d27ebabc8d93cdc555362a4c7c08a1052e67647e9f3f879846389672e7a5d6e1ff93940d4196bef451e8d6a3b410a5062ac29cee3783e9a5aeac9724ad1375a2189c3b5a8dbf671dfad990132d2e5b73eb5a2e3d2034b6b908210f5fe61272b2cf4d1e3a4aa",
			};

			var serverEphemeral = new SrpEphemeral
			{
				Secret = "54f5f01dc134a3decef47e5e74feb20ce60716965c1908aa422ec701e5c2ce23",
				Public = "47b1e293dff41447e74d33b6a13cfd3dc77e17580a6d724c633d106827dcba9578d222ea6931dfb37ba282998df04dae849eafc57e4bdbf8478f0fd312b4393af8d6512f6013ab4199b831673ce99f14240ef3202803bb4ced05cb046c42a108b2342fdd3e30f8ba7b8f154243b6873a30c467d368888a5a95ed7abaad10ba0bd093717c1479e46e8e15b20809bc7e2f3bc316d09c0b6a3289852ac4d441be50d3ce1ec76ded2f44c643e8fbfa762a62f3311e3425c7f6730d7b35f9037dc07d6165968ece3b4885b5d5cb264a50595cf989622b2fe156a0d98101e5f14f808a3595da761885188f50230fcddc4dd34ec38de5f64a44fdcd1f535f5f83f900d7",
			};

			var salt = "31c3af4879262b1ee85295480b14800672cbb59870e7ae1980a07ee56eaa25fc";
			var username = "hello";
			var privateKey = "d3f37035827919d8803d246d0a81dcf0118e84f85e45c4c06f2c362262422118";
			var verifier = "1786105be4cde9793d4896047cd178260ded3a0623491d18b0e942469107012f0a8d67d40c41d5b4863233ee5cd6b765bf3bffd56d0b429445be5af163303d42ae5ced9ff29e3cd275eeba482d3dad3bac3d6f2cf2113c6be5c50dfd2e3a2a9a1bbf2d829d4a5538c36e94197dfce12e990d030a124ee77ebb843c416701d85f0e00f1001a93051aef27d6e7c7120d00f08c52e4b1ea99b050c6d4080d59c0080af439f9291d07e384f13d121c1374d71f0d168e6fbfab9408974bf652844c7ac07b77b5dbc3cb53cb89de9d7fdcaf33f21e1e73c16bdc487732b2773aa34da0777b1d057a8aa3fc3a0679661956fa2ee01f69bcc1535d381feaaa973e7d802c";
			var clientSessionProof = "ddc8c78aafe9c471086b3d20a4e4eb2401de2fcaa48081fea5357114dc508a23";
			var serverSessionKey = "bd0079ddefc205d65da9241ba416c44a131440c723e20de6e3bdb5bd662c9de0";
			var serverSessionProof = "01a62474121b11347f84d422088b469b949d9a376f89b87b8080f17931846ef5";

			var clientSession = new SrpClient(parameters).DeriveSession(clientEphemeral.Secret, serverEphemeral.Public, salt, username, privateKey);
			Assert.IsNotNull(clientSession);
			Assert.AreEqual(serverSessionKey, clientSession.Key);
			Assert.AreEqual(clientSessionProof, clientSession.Proof);

			var serverSession = new SrpServer(parameters).DeriveSession(serverEphemeral.Secret, clientEphemeral.Public, salt, username, verifier, clientSessionProof);
			Assert.IsNotNull(serverSession);
			Assert.AreEqual(serverSessionKey, serverSession.Key);
			Assert.AreEqual(serverSessionProof, serverSession.Proof);
		}

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

			new SrpClient().VerifySession(clientEphemeralPublic, clientSession, serverSessionProof);
		}

		[TestMethod]
		public void SrpTestVectorsFromRfc5054()
		{
			// https://www.ietf.org/rfc/rfc5054.txt
			var N = SrpInteger.FromHex(@"EEAF0AB9 ADB38DD6 9C33F80A FA8FC5E8 60726187 75FF3C0B 9EA2314C
				9C256576 D674DF74 96EA81D3 383B4813 D692C6E0 E0D5D8E2 50B98BE4
				8E495C1D 6089DAD1 5DC7D7B4 6154D6B6 CE8EF4AD 69B15D49 82559B29
				7BCF1885 C529F566 660E57EC 68EDBC3C 05726CC0 2FD4CBF4 976EAA9A
				FD5138FE 8376435B 9FC61D2F C0EB06E3");
			var g = SrpInteger.FromHex("02");
			var p = SrpParameters.Create<SHA1>(N, g);
			var H = p.H;
			var k = p.K;
			var kx = SrpInteger.FromHex(@"7556AA04 5AEF2CDD 07ABAF0F 665C3E81 8913186F");
			Assert.AreEqual(kx, k);

			// prepare known parameters
			var I = "alice";
			var P = "password123";
			var s = SrpInteger.FromHex(@"BEB25379 D1A8581E B5A72767 3A2441EE").ToHex();
			var srp = new SrpClient(p);

			// validate the private key
			var x = SrpInteger.FromHex(srp.DerivePrivateKey(s, I, P));
			var xx = SrpInteger.FromHex(@"94B7555A ABE9127C C58CCF49 93DB6CF8 4D16C124");
			Assert.AreEqual(xx, x);

			// validate the verifier
			var v = SrpInteger.FromHex(srp.DeriveVerifier(x));
			var vx = SrpInteger.FromHex(@"
				7E273DE8 696FFC4F 4E337D05 B4B375BE B0DDE156 9E8FA00A 9886D812
				9BADA1F1 822223CA 1A605B53 0E379BA4 729FDC59 F105B478 7E5186F5
				C671085A 1447B52A 48CF1970 B4FB6F84 00BBF4CE BFBB1681 52E08AB5
				EA53D15C 1AFF87B2 B9DA6E04 E058AD51 CC72BFC9 033B564E 26480D78
				E955A5E2 9E7AB245 DB2BE315 E2099AFB");
			Assert.AreEqual(vx, v);

			// client ephemeral
			var a = SrpInteger.FromHex("60975527 035CF2AD 1989806F 0407210B C81EDC04 E2762A56 AFD529DD DA2D4393");
			var A = g.ModPow(a, N);
			var Ax = SrpInteger.FromHex(@"
				61D5E490 F6F1B795 47B0704C 436F523D D0E560F0 C64115BB 72557EC4
				4352E890 3211C046 92272D8B 2D1A5358 A2CF1B6E 0BFCF99F 921530EC
				8E393561 79EAE45E 42BA92AE ACED8251 71E1E8B9 AF6D9C03 E1327F44
				BE087EF0 6530E69F 66615261 EEF54073 CA11CF58 58F0EDFD FE15EFEA
				B349EF5D 76988A36 72FAC47B 0769447B");
			Assert.AreEqual(Ax, A);
			var clientEphemeral = new SrpEphemeral { Public = A, Secret = a };

			// server ephemeral
			var b = SrpInteger.FromHex("E487CB59 D31AC550 471E81F0 0F6928E0 1DDA08E9 74A004F4 9E61F5D1 05284D20");
			var B = (k * SrpInteger.FromHex(v) + g.ModPow(b, N)) % N;
			var Bx = SrpInteger.FromHex(@"
				BD0C6151 2C692C0C B6D041FA 01BB152D 4916A1E7 7AF46AE1 05393011
				BAF38964 DC46A067 0DD125B9 5A981652 236F99D9 B681CBF8 7837EC99
				6C6DA044 53728610 D0C6DDB5 8B318885 D7D82C7F 8DEB75CE 7BD4FBAA
				37089E6F 9C6059F3 88838E7A 00030B33 1EB76840 910440B1 B27AAEAE
				EB4012B7 D7665238 A8E3FB00 4B117B58");
			Assert.AreEqual(Bx, B);
			var serverEphemeral = new SrpEphemeral { Public = B, Secret = b };

			// u
			var u = H(A, B);
			var ux = SrpInteger.FromHex("CE38B959 3487DA98 554ED47D 70A7AE5F 462EF019");
			Assert.AreEqual(ux, u);

			// premaster secret — client version
			var S = (B - (k * (g.ModPow(x, N)))).ModPow(a + (u * x), N);
			var Sx = SrpInteger.FromHex(@"
				B0DC82BA BCF30674 AE450C02 87745E79 90A3381F 63B387AA F271A10D
				233861E3 59B48220 F7C4693C 9AE12B0A 6F67809F 0876E2D0 13800D6C
				41BB59B6 D5979B5C 00A172B4 A2A5903A 0BDCAF8A 709585EB 2AFAFA8F
				3499B200 210DCC1F 10EB3394 3CD67FC8 8A2F39A4 BE5BEC4E C0A3212D
				C346D7E4 74B29EDE 8A469FFE CA686E5A");
			Assert.AreEqual(Sx, S);

			// premaster secret — server version
			S = (A * v.ModPow(u, N)).ModPow(b, N);
			Assert.AreEqual(Sx, S);

			// client session
			var session = srp.DeriveSession(a, B, s, I, x);
			Assert.AreEqual("017eefa1cefc5c2e626e21598987f31e0f1b11bb", session.Key);
			Assert.AreEqual("3f3bc67169ea71302599cf1b0f5d408b7b65d347", session.Proof);

			// server session
			var srvsess = new SrpServer(p).DeriveSession(b, A, s, I, v, session.Proof);
			Assert.AreEqual("017eefa1cefc5c2e626e21598987f31e0f1b11bb", srvsess.Key);
			Assert.AreEqual("9cab3c575a11de37d3ac1421a9f009236a48eb55", srvsess.Proof);

			// verify server session
			srp.VerifySession(A, session, srvsess.Proof);
		}

		[TestMethod]
		public void SrpShouldAuthenticateAUser()
		{
			// default parameters, taken from https://github.com/LinusU/secure-remote-password/blob/master/test.js
			SrpAuthentication("linus@folkdatorn.se", "$uper$ecure");

			// sha512, 512-bit prime number
			var parameters = SrpParameters.Create<SHA512>("D4C7F8A2B32C11B8FBA9581EC4BA4F1B04215642EF7355E37C0FC0443EF756EA2C6B8EEB755A1C723027663CAA265EF785B8FF6A9B35227A52D86633DBDFCA43", "03");
			SrpAuthentication("yallie@yandex.ru", "h4ck3r$", parameters);

			// md5, 1024-bit prime number from wikipedia (generated using "openssl dhparam -text 1024")
			parameters = SrpParameters.Create<SHA384>("00c037c37588b4329887e61c2da3324b1ba4b81a63f9748fed2d8a410c2fc21b1232f0d3bfa024276cfd88448197aae486a63bfca7b8bf7754dfb327c7201f6fd17fd7fd74158bd31ce772c9f5f8ab584548a99a759b5a2c0532162b7b6218e8f142bce2c30d7784689a483e095e701618437913a8c39c3dd0d4ca3c500b885fe3", "07");
			SrpAuthentication("bozo", "h4ck3r", parameters);

			// sha1 hash function, default N and g values for all standard groups
			SrpAuthenticationUsingStandardParameters<SHA1>("hello", "world");
		}

		// [Test, Explicit]
		public void SrpStressTest()
		{
			// 100 iterations take ~10 minutes on my machine
			for (var i = 0; i < 100; i++)
			{
				SrpShouldAuthenticateAUser();
			}
		}

		// [Test, Explicit]
		public void SrpUsingStandardParameters()
		{
			// takes ~42 seconds on my machine
			SrpAuthenticationUsingStandardParameters<SHA1>("user", "password");
			SrpAuthenticationUsingStandardParameters<SHA256>("LongUser", "stronger-password");
			SrpAuthenticationUsingStandardParameters<SHA384>("root", "$hacker$");
			SrpAuthenticationUsingStandardParameters<SHA512>("Administrator", "123456");
			SrpAuthenticationUsingStandardParameters<MD5>("not-safe", "dont-use");
		}

		private void SrpAuthenticationUsingStandardParameters<T>(string username, string password) where T : HashAlgorithm
		{
			// test all standard groups using the same hashing algorithm
			SrpAuthentication(username, password, SrpParameters.Create1024<T>());
			SrpAuthentication(username, password, SrpParameters.Create1536<T>());
			SrpAuthentication(username, password, SrpParameters.Create2048<T>());
			SrpAuthentication(username, password, SrpParameters.Create3072<T>());
			SrpAuthentication(username, password, SrpParameters.Create4096<T>());
			SrpAuthentication(username, password, SrpParameters.Create6144<T>());
			SrpAuthentication(username, password, SrpParameters.Create8192<T>());
		}

		private void SrpAuthentication(string username, string password, SrpParameters parameters = null)
		{
			// use default parameters if not specified: sha256, 2048-bit prime number
			var client = new SrpClient(parameters);
			var server = new SrpServer(parameters);

			// sign up
			var salt = client.GenerateSalt();
			var privateKey = client.DerivePrivateKey(salt, username, password);
			var verifier = client.DeriveVerifier(privateKey);

			// authenticate
			var clientEphemeral = client.GenerateEphemeral();
			var serverEphemeral = server.GenerateEphemeral(verifier);
			var clientSession = client.DeriveSession(clientEphemeral.Secret, serverEphemeral.Public, salt, username, privateKey);

			try
			{
				var serverSession = server.DeriveSession(serverEphemeral.Secret, clientEphemeral.Public, salt, username, verifier, clientSession.Proof);
				client.VerifySession(clientEphemeral.Public, clientSession, serverSession.Proof);

				// make sure both the client and the server have the same session key
				Assert.AreEqual(clientSession.Key, serverSession.Key);
			}
			catch
			{
				// generate the regression test code
				Console.WriteLine("// regression test:");
				Console.WriteLine($"var parameters = {parameters?.ToString() ?? "new SrpParameters()"};");
				Console.WriteLine($"var serverEphemeral = new SrpEphemeral");
				Console.WriteLine($"{{");
				Console.WriteLine($"	Secret = \"{serverEphemeral.Secret}\",");
				Console.WriteLine($"	Public = \"{serverEphemeral.Public}\",");
				Console.WriteLine($"}};");
				Console.WriteLine();
				Console.WriteLine($"var clientEphemeral = new SrpEphemeral");
				Console.WriteLine($"{{");
				Console.WriteLine($"	Secret = \"{clientEphemeral.Secret}\",");
				Console.WriteLine($"	Public = \"{clientEphemeral.Public}\",");
				Console.WriteLine($"}};");
				Console.WriteLine();
				Console.WriteLine($"var salt = \"{salt}\";");
				Console.WriteLine($"var username = \"{username}\";");
				Console.WriteLine($"var privateKey = \"{privateKey}\";");
				Console.WriteLine($"var verifier = \"{verifier}\";");
				Console.WriteLine($"var clientSessionProof = \"{clientSession.Proof}\";");
				Console.WriteLine($"var serverSessionKey = \"{clientSession.Key}\";");
				Console.WriteLine($"var serverSessionProof = \"????\";");
				Console.WriteLine();
				Console.WriteLine($"var clientSession = new SrpClient(parameters).DeriveSession(clientEphemeral.Secret, serverEphemeral.Public, salt, username, privateKey);");
				Console.WriteLine($"Assert.IsNotNull(clientSession);");
				Console.WriteLine($"Assert.AreEqual(serverSessionKey, clientSession.Key);");
				Console.WriteLine($"Assert.AreEqual(clientSessionProof, clientSession.Proof);");
				Console.WriteLine();
				Console.WriteLine($"var serverSession = new SrpServer(parameters).DeriveSession(serverEphemeral.Secret, clientEphemeral.Public, salt, username, verifier, clientSessionProof);");
				Console.WriteLine($"Assert.IsNotNull(serverSession);");
				Console.WriteLine($"Assert.AreEqual(serverSessionKey, serverSession.Key);");
				Console.WriteLine($"Assert.AreEqual(serverSessionProof, serverSession.Proof);");
				throw;
			}
		}
	}
}
