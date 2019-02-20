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
	/// Test class for the debouncer.
	///</summary>
	[TestClass]
	public class DebouncerTests
	{
		[TestMethod, ExpectedException(typeof(ArgumentNullException))]
		public void NullActionIsNotAllowedForSetTimeout()
		{
			Debouncer.SetTimeout(null, 10);
		}

		[TestMethod]
		public void SetTimeoutExecutesTheGivenActionAfterAnInterval()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			Debouncer.SetTimeout(inc, 10);
			Assert.AreEqual(0, counter);

			Thread.Sleep(50);
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

			Thread.Sleep(50);
			Assert.AreEqual(0, counter);
		}

		[TestMethod, ExpectedException(typeof(ArgumentNullException))]
		public void NullActionIsNotAllowedForSetInterval()
		{
			Debouncer.SetInterval(null, 10);
		}

		[TestMethod]
		public void SetIntervalExecutesTheGivenActionAtAGivenInterval()
		{
			// target function
			var counter = 0;
			Action inc = () => counter++;

			Debouncer.SetInterval(inc, 10);
			Assert.AreEqual(0, counter);

			Thread.Sleep(50);
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

			Thread.Sleep(50);
			Assert.IsTrue(counter > 1);
			timer.Dispose();

			var lastCounter = counter;
			Thread.Sleep(50);
			Assert.AreEqual(lastCounter, counter);
		}

		[TestMethod, ExpectedException(typeof(ArgumentNullException))]
		public void NullActionIsNotAllowedForDebounce()
		{
			Debouncer.Debounce(null);
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

			Thread.Sleep(50);
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

			// try to call the debounced version and make sure the target is not yet called
			debounced();
			debounced();
			debounced();
			debounced();
			Assert.AreEqual(4, counter);
		}
	}
}
