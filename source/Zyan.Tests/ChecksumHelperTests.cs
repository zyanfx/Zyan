using System;
using System.Collections.Generic;
using System.Linq;
using Zyan.Communication.Toolbox;

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
	/// Test class for the cheksum helper.
	///</summary>
	[TestClass]
	public class ChecksumHelperTests
	{
		[TestMethod]
		public void ChecksumHelperComputesChecksumOfAnEmptyList()
		{
			// sha256 hash of a zero-length buffer
			Assert.AreEqual("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", ChecksumHelper.ComputeHash(null));
			Assert.AreEqual("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", ChecksumHelper.ComputeHash(new Guid[0]));
		}

		[TestMethod]
		public void ChecksumHelperComputesKnownChecksums()
		{
			var guids = new List<Guid>();
			guids.Add(new Guid("d970d9f3-b15b-4c88-8bdc-34fb3f9d119c"));
			Assert.AreEqual("e2a5278b5a85bfc1ec258003b697183d8a3330eb750ec3439cbcbfe19e285b87", ChecksumHelper.ComputeHash(guids));

			var moreGuids = @"
				87c0b82e-00bb-4e7e-b1d4-ae53b4bd6feb
				d4b506e5-c70f-47a7-902b-764d2c8ac64a
				3ccc7db1-bc5e-476d-b348-752e3695a317
				ae6ba09b-6f2a-4ae9-85bd-d770cbfdabc0
				81c897ed-ec29-468c-894a-6397911373aa
				51dd1835-824f-49c7-be96-45f8cb768aa8
				a3c9a21f-e6b4-43e6-a651-ec6c9afde1a0
				38edea28-9bca-419a-a85b-8393f468f354
				5f21501d-4f10-4258-b7f5-a1979a124b7b
				e45bbd78-93dd-42bc-bda9-137f204fbba4"
				.Split('\n')
				.Select(s => s.Trim())
				.Where(s => !string.IsNullOrEmpty(s))
				.Select(s => new Guid(s));
			Assert.AreEqual("5540cde62cf9871c1b8f69bfd7f2b0047c8ee6f0a613f4e10739ab059adbd618", ChecksumHelper.ComputeHash(guids.Concat(moreGuids)));
		}
	}
}
