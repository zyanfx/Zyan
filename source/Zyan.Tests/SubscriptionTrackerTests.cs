using System;
using System.Collections.Generic;
using System.Linq;
using Zyan.Communication.Delegates;

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
	/// Test class for the subscription tracker.
	///</summary>
	[TestClass]
	public class SubscriptionTrackerTests
	{
		[TestMethod]
		public void EmptySubscriptionTracker()
		{
			// sha256 hash of a zero-length buffer
			Assert.AreEqual("0:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", new SubscriptionTracker().Checksum);
		}

		[TestMethod]
		public void SubscriptionTrackerCanAddAndRemoveGuids()
		{
			// add one
			var subscriptionTracker = new SubscriptionTracker();
			var guids = new List<Guid>();
			guids.Add(new Guid("d970d9f3-b15b-4c88-8bdc-34fb3f9d119c"));
			Assert.AreEqual("1:e2a5278b5a85bfc1ec258003b697183d8a3330eb750ec3439cbcbfe19e285b87", subscriptionTracker.Add(guids));
			Assert.AreEqual(1, subscriptionTracker.Count);

			// add many
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
			Assert.AreEqual("11:5540cde62cf9871c1b8f69bfd7f2b0047c8ee6f0a613f4e10739ab059adbd618", subscriptionTracker.Add(moreGuids));
			Assert.AreEqual(11, subscriptionTracker.Count);

			// add a few guids already included in the set
			var duplicates = @"
				d4b506e5-c70f-47a7-902b-764d2c8ac64a
				3ccc7db1-bc5e-476d-b348-752e3695a317
				51dd1835-824f-49c7-be96-45f8cb768aa8
				a3c9a21f-e6b4-43e6-a651-ec6c9afde1a0
				38edea28-9bca-419a-a85b-8393f468f354
				5f21501d-4f10-4258-b7f5-a1979a124b7b
				e45bbd78-93dd-42bc-bda9-137f204fbba4"
				.Split('\n')
				.Select(s => s.Trim())
				.Where(s => !string.IsNullOrEmpty(s))
				.Select(s => new Guid(s));

			// checksum should not be changed
			Assert.AreEqual("11:5540cde62cf9871c1b8f69bfd7f2b0047c8ee6f0a613f4e10739ab059adbd618", subscriptionTracker.Add(duplicates));
			Assert.AreEqual(11, subscriptionTracker.Count);

			// remove a few guids
			Assert.AreEqual("4:854614224fd9c031cc2315959677c3796154f385afdba799a6088ab759bb83a8", subscriptionTracker.Remove(duplicates));
			Assert.AreEqual(4, subscriptionTracker.Count);

			// remove again already removed guid, checksum should not be changed
			Assert.AreEqual("4:854614224fd9c031cc2315959677c3796154f385afdba799a6088ab759bb83a8", subscriptionTracker.Remove(new[]
			{
				new Guid("a3c9a21f-e6b4-43e6-a651-ec6c9afde1a0")
			}));

			// remove again the same guids, checksum should not be changed
			Assert.AreEqual("4:854614224fd9c031cc2315959677c3796154f385afdba799a6088ab759bb83a8", subscriptionTracker.Remove(duplicates));
			Assert.AreEqual(4, subscriptionTracker.Count);

			// remove all except one
			Assert.AreEqual("1:e2a5278b5a85bfc1ec258003b697183d8a3330eb750ec3439cbcbfe19e285b87", subscriptionTracker.Remove(moreGuids));
			Assert.AreEqual(1, subscriptionTracker.Count);

			// remove the first one
			Assert.AreEqual("0:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", subscriptionTracker.Remove(new[]
			{
				new Guid("d970d9f3-b15b-4c88-8bdc-34fb3f9d119c")
			}));

			// nothing left
			Assert.AreEqual(0, subscriptionTracker.Count);
		}

		[TestMethod]
		public void SubscriptionTrackerCanReset()
		{
			// add one
			var subscriptionTracker = new SubscriptionTracker();
			var guids = new List<Guid>();
			guids.Add(new Guid("d970d9f3-b15b-4c88-8bdc-34fb3f9d119c"));
			Assert.AreEqual("1:e2a5278b5a85bfc1ec258003b697183d8a3330eb750ec3439cbcbfe19e285b87", subscriptionTracker.Add(guids));
			Assert.AreEqual(1, subscriptionTracker.Count);

			// clear
			Assert.AreEqual("0:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", subscriptionTracker.Reset());
			Assert.AreEqual(0, subscriptionTracker.Count);
		}
	}
}
