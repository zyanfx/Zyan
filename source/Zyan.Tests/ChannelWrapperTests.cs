using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Zyan.Communication.Toolbox.Compression;
using Zyan.Communication.ChannelSinks.Compression;
using Zyan.Communication.Protocols.Wrapper;

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
	/// Test class for channel wrapper.
	///</summary>
	[TestClass]
	public class ChannelWrapperTests
	{
		[TestMethod]
		public void ChannelWrapper_RandomizesGivenUrl()
		{
			var originalUrl = "tcpex://localhost:12356/CoolService";
			var randomized1 = ChannelWrapper.RandomizeUrl(originalUrl);
			var randomized2 = ChannelWrapper.RandomizeUrl(originalUrl);

			Assert.IsFalse(string.IsNullOrEmpty(randomized1));
			Assert.IsFalse(string.IsNullOrEmpty(randomized2));

			Assert.AreNotEqual(originalUrl, randomized1);
			Assert.AreNotEqual(originalUrl, randomized2);
			Assert.AreNotEqual(randomized1, randomized2);

			Assert.IsTrue(randomized1.EndsWith(originalUrl));
			Assert.IsTrue(randomized2.EndsWith(originalUrl));
		}

		[TestMethod]
		public void ChannelWrapper_NormalizesRandomizedUrl()
		{
			var randomizedUrl = "wrap://2354gfds45#tcpex://localhost:12356/CoolService";
			var originalUrl = ChannelWrapper.NormalizeUrl(randomizedUrl);

			Assert.IsFalse(string.IsNullOrEmpty(originalUrl));
			Assert.AreEqual("tcpex://localhost:12356/CoolService", originalUrl);
		}
	}
}
