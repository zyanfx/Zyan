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

			Thread.Sleep(TimeSpan.FromSeconds(1));

			pool.Dispose();
			Assert.AreEqual(1000, count);
		}

		[TestMethod]
		public void SimpleLockThreadPoolQueueFromMultipleThreads()
		{
			var count = 0;
			var queueCount = 0;
			var pool = new SimpleLockThreadPool();
			var queuePool = new SimpleLockThreadPool();
			var callback = new WaitCallback(obj => Interlocked.Increment(ref count));
			var queueCallback = new WaitCallback(obj =>
			{
				Interlocked.Increment(ref queueCount);
				pool.QueueUserWorkItem(callback);
			});

			for (var i = 0; i < 1000; i++)
			{
				queuePool.QueueUserWorkItem(queueCallback);
			}

			Thread.Sleep(TimeSpan.FromSeconds(2));

			queuePool.Dispose();
			Assert.AreEqual(1000, queueCount);

			Thread.Sleep(TimeSpan.FromSeconds(1));

			pool.Dispose();
			Assert.AreEqual(1000, count);
		}

		[TestMethod]
		public void SimpleLockThreadPoolWorkerThreadsAreNamed()
		{
			var pool = new SimpleLockThreadPool()
			{
				WorkerThreadName = "I'm not anonymous",
			};

			var count = 0;
			var callback = new WaitCallback(obj =>
			{
				Assert.AreEqual(pool.WorkerThreadName, Thread.CurrentThread.Name);
				Interlocked.Increment(ref count);
			});

			for (var i = 0; i < 10; i++)
			{
				pool.QueueUserWorkItem(callback);
			}

			Thread.Sleep(TimeSpan.FromSeconds(0.1));
			pool.Dispose();
			Assert.AreEqual(10, count);
		}
	}
}
