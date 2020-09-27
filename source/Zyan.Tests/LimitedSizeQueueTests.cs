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
	/// Test class for the extension methods.
	///</summary>
	[TestClass]
	public class LimitedSizeQueueTests
	{
		[TestMethod]
		public void LimitedSizeQueueLimitsTheSizeOfTheInternalQueue()
		{
			var q = new LimitedSizeQueue<int>(3);
			Assert.AreEqual(0, q.Count);
			Assert.AreEqual(3, q.Limit);
			Assert.IsFalse(q.TryPeek(out _));
			Assert.IsFalse(q.TryDequeue(out _));

			Assert.IsTrue(q.TryEnqueue(1));
			Assert.IsTrue(q.TryEnqueue(2));
			Assert.IsTrue(q.TryEnqueue(3));
			Assert.IsFalse(q.TryEnqueue(4));
			Assert.IsFalse(q.TryEnqueue(5));

			var items = string.Join(", ", q.Select(i => i.ToString()).ToArray());
			Assert.AreEqual("1, 2, 3", items);

			Assert.IsTrue(q.TryDequeue(out var item));
			Assert.AreEqual(1, item);

			Assert.IsTrue(q.TryEnqueue(6));
			Assert.IsFalse(q.TryEnqueue(7));

			Assert.IsTrue(q.TryDequeue(out item));
			Assert.AreEqual(2, item);

			Assert.IsTrue(q.TryDequeue(out item));
			Assert.AreEqual(3, item);

			Assert.IsTrue(q.TryPeek(out item));
			Assert.AreEqual(6, item);

			Assert.IsTrue(q.TryDequeue(out item));
			Assert.AreEqual(6, item);

			Assert.IsFalse(q.TryPeek(out _));
			Assert.IsFalse(q.TryDequeue(out _));
		}

		[TestMethod]
		public void LimitedSizeQueueCanClearItems()
		{
			var q = new LimitedSizeQueue<int>(3);
			Assert.IsTrue(q.TryEnqueue(1));
			Assert.IsTrue(q.TryEnqueue(2));
			Assert.IsTrue(q.TryEnqueue(3));
			Assert.IsFalse(q.TryEnqueue(4));

			Assert.AreEqual(3, q.Count);
			Assert.AreEqual(3, q.Limit);

			q.Clear();
			Assert.AreEqual(0, q.Count);
			Assert.AreEqual(3, q.Limit);

			var items = string.Join(", ", q.Select(i => i.ToString()).ToArray());
			Assert.AreEqual(string.Empty, items);
		}

		[TestMethod]
		public void ZeroLimitMeansUnlimitedSize()
		{
			var q = new LimitedSizeQueue<int>(0);
			foreach (var item in Enumerable.Range(1, 1000))
			{
				Assert.IsTrue(q.TryEnqueue(item));
			}

			q.Clear();
			Assert.AreEqual(0, q.Count);
			Assert.AreEqual(0, q.Limit);
		}
	}
}
