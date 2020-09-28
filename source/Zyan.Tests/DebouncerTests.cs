using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
	/// Test class for the debouncer.
	///</summary>
	[TestClass]
	public class DebouncerTests
	{
		[TestMethod]
		public void NullActionIsNotAllowedForSetTimeout()
		{
			Assert.Throws<ArgumentNullException>(() => Debouncer.SetTimeout(null, 10));
		}

		[TestMethod]
		public void SetTimeoutExecutesTheGivenActionAfterAnInterval()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			Debouncer.SetTimeout(inc, 10);
			Assert.AreEqual(0, counter);

			Thread.Sleep(100);
			Assert.AreEqual(1, counter);
		}

		[TestMethod]
		public void SetTimeoutCanBeCancelled()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			var timer = Debouncer.SetTimeout(inc, 10);
			Assert.AreEqual(0, counter);
			timer.Dispose();

			Thread.Sleep(100);
			Assert.AreEqual(0, counter);
		}

		[TestMethod]
		public void NullActionIsNotAllowedForSetInterval()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				Debouncer.SetInterval(null, 10);
			});
		}

		[TestMethod]
		public void SetIntervalExecutesTheGivenActionAtAGivenInterval()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			Debouncer.SetInterval(inc, 10);
			Assert.AreEqual(0, counter);

			Thread.Sleep(100);
			Assert.IsTrue(counter > 1);
		}

		[TestMethod]
		public void SetIntervalCanBeCancelled()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			var timer = Debouncer.SetInterval(inc, 10);
			Assert.AreEqual(0, counter);

			Thread.Sleep(100);
			Assert.IsTrue(counter > 1);
			timer.Dispose();

			var lastCounter = counter;
			Thread.Sleep(100);
			Assert.AreEqual(lastCounter, counter);
		}

		[TestMethod]
		public void NullActionIsNotAllowedForDebounce()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				Debouncer.Debounce(null);
			});
		}

		[TestMethod]
		public void DebouncedActionIsCalledOnce()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			// debounce the given function
			var debounced = inc.Debounce(10);
			Assert.IsNotNull(debounced);

			// try to call the debounced version and make sure the target is not yet called
			debounced();
			debounced();
			debounced();
			debounced();
			Assert.AreEqual(0, counter);

			Thread.Sleep(100);
			Assert.AreEqual(1, counter);
		}

		[TestMethod]
		public void DebounceWithZeroIntervalMeansImmediateExecution()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			// debounce the given function
			var debounced = inc.Debounce(0).Debounce(-100);
			Assert.AreSame(debounced, inc);

			// try to call the debounced version and make sure the target is immediately called
			debounced();
			debounced();
			debounced();
			debounced();
			Assert.AreEqual(4, counter);
		}

		[TestMethod]
		public void NullActionIsNotAllowedForCancellableDebounce()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				Debouncer.CancellableDebounce(null);
			});
		}

		[TestMethod]
		public void CancellableDebouncedActionIsCalledOnce()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			// debounce the given function
			var debounced = inc.CancellableDebounce(10);
			Assert.IsNotNull(debounced);

			// try to call the debounced version and make sure the target is not yet called
			debounced();
			debounced();
			debounced();
			debounced();
			Assert.AreEqual(0, counter);

			Thread.Sleep(100);
			Assert.AreEqual(1, counter);
		}

		[TestMethod]
		public void CancellableDebounceWithZeroIntervalMeansImmediateExecution()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			// debounce the given function
			var debounced = inc.CancellableDebounce(0);
			Assert.AreNotSame(debounced, inc);

			// try to call the debounced version and make sure the target is immediately called
			debounced();
			debounced();
			debounced();
			debounced();
			Assert.AreEqual(4, counter);
		}

		[TestMethod]
		public void PendingCancellableDebouncedActionExecutionCanBeCanceled()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			// debounce the given function
			var debounced = inc.CancellableDebounce(10);
			Assert.IsNotNull(debounced);

			// try to call the debounced version and make sure the target is not yet called
			debounced();
			debounced();
			debounced();
			debounced();
			Assert.AreEqual(0, counter);

			// now cancel pending execution
			debounced(false);
			Thread.Sleep(100);

			// the code wasn't executed
			Assert.AreEqual(0, counter);
		}

		[TestMethod]
		public void PendingCancellableDebounceWithZeroIntervalWithCancelArgumentDoesntExecuteTheAction()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			// debounce the given function
			var debounced = inc.CancellableDebounce(0);
			Assert.AreNotSame(debounced, inc);

			// try to call the debounced version and make sure the target is not called
			debounced(false);
			debounced(false);
			debounced(false);
			debounced(false);
			Assert.AreEqual(0, counter);

			debounced(true);
			debounced(true);
			Assert.AreEqual(2, counter);
		}

		[TestMethod]
		public void ActionCanBeExecutedByOneThreadAtMost()
		{
			var counter = 0;
			var action = new Action(() =>
			{
				Thread.Sleep(50);
				Interlocked.Increment(ref counter); 
			});

			// the original action is executed by all worker threads
			for (var i = 0; i < 5; i++)
			{
				ThreadPool.QueueUserWorkItem(x => action());
			}

			Thread.Sleep(300);
			Assert.AreEqual(5, counter);

			// convert it into an exclusive action
			counter = 0;
			action = action.ExecuteByOneThreadAtMost();

			// the modified action is executed by only one thread
			for (var i = 0; i < 4; i++)
			{
				// assume that all worker threads are started within
				// the 50ms time span while the first action is still running
				ThreadPool.QueueUserWorkItem(x => action());
			}

			// assume that all user work items are processed now
			Thread.Sleep(300);
			Assert.AreEqual(1, counter);

			// the modified action is still executed by only one thread
			for (var i = 0; i < 4; i++)
			{
				ThreadPool.QueueUserWorkItem(x => action());
			}

			Thread.Sleep(300);
			Assert.AreEqual(2, counter);
		}
	}
}
