using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Zyan.Communication.Threading;
using Zyan.Communication.Toolbox;

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
	/// Test class for the thread pool.
	///</summary>
	[TestClass]
	public class ThreadPoolTests
	{
		[TestMethod]
		public void SimpleLockThreadPoolRunDispose()
		{
			var count = 0;
			var pool = new SimpleLockThreadPool();
			var callback = new WaitCallback(obj => Interlocked.Increment(ref count));

			for (var i = 0; i < 1000; i++)
			{
				pool.QueueUserWorkItem(callback);
			}

			pool.Dispose();
			Assert.AreEqual(1000, count);
		}
	}
}
