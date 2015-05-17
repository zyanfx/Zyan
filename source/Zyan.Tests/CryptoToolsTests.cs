using System;
using System.Security.Cryptography;
using Zyan.Communication.ChannelSinks.Encryption;

namespace Zyan.Tests
{
	#region Unit testing platform abstraction layer
#if NUNIT
	using NUnit.Framework;
	using TestClass = NUnit.Framework.TestFixtureAttribute;
	using TestMethod = NUnit.Framework.TestAttribute;

#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassCleanupNonStatic = DummyAttribute;
	using ClassInitializeNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for CryptoTools.
	///</summary>
	[TestClass]
	public class CryptoToolsTests
	{
		[TestMethod]
		public void CryptoTools_CreateDes()
		{
			var prov = CryptoTools.CreateSymmetricCryptoProvider("DES");
			Assert.IsNotNull(prov);
			Assert.AreEqual(typeof(DESCryptoServiceProvider), prov.GetType());

			prov = CryptoTools.CreateSymmetricCryptoProvider("des");
			Assert.IsNotNull(prov);
			Assert.AreEqual(typeof(DESCryptoServiceProvider), prov.GetType());
		}

		[TestMethod]
		public void CryptoTools_Create3Des()
		{
			var prov = CryptoTools.CreateSymmetricCryptoProvider("3DES");
			Assert.IsNotNull(prov);
			Assert.AreEqual(typeof(TripleDESCryptoServiceProvider), prov.GetType());

			prov = CryptoTools.CreateSymmetricCryptoProvider("3des");
			Assert.IsNotNull(prov);
			Assert.AreEqual(typeof(TripleDESCryptoServiceProvider), prov.GetType());
		}

		[TestMethod]
		public void CryptoTools_CreateRijndael()
		{
			var prov = CryptoTools.CreateSymmetricCryptoProvider("RIJNDAEL");
			Assert.IsNotNull(prov);
			Assert.AreEqual(typeof(RijndaelManaged), prov.GetType());

			prov = CryptoTools.CreateSymmetricCryptoProvider("rijndael");
			Assert.IsNotNull(prov);
			Assert.AreEqual(typeof(RijndaelManaged), prov.GetType());
		}

		[TestMethod]
		public void CryptoTools_CreateRc2()
		{
			var prov = CryptoTools.CreateSymmetricCryptoProvider("RC2");
			Assert.IsNotNull(prov);
			Assert.AreEqual(typeof(RC2CryptoServiceProvider), prov.GetType());

			prov = CryptoTools.CreateSymmetricCryptoProvider("rc2");
			Assert.IsNotNull(prov);
			Assert.AreEqual(typeof(RC2CryptoServiceProvider), prov.GetType());
		}

		[TestMethod, ExpectedException(typeof(ArgumentException))]
		public void CryptoTools_UnknownAlgorithm()
		{
			CryptoTools.CreateSymmetricCryptoProvider("nicetry");
		}
	}
}
