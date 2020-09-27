using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Zyan.Communication.Delegates;

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
	using AssertFailedException = NUnit.Framework.AssertionException;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for SafeDynamicInvoke() extension method.
	///</summary>
	[TestClass]
	public class SafeDynamicInvokerTests
	{
		[TestMethod]
		public void SingleEventHandler()
		{
			var fired = false;
			EventHandler handler = (s, e) => fired = true;
			var result = handler.SafeDynamicInvoke(null, EventArgs.Empty);
			Assert.IsTrue(fired);
			Assert.IsNull(result);
		}

		[TestMethod]
		public void MultipleEventHandlers()
		{
			var fired1 = false;
			EventHandler handler1 = (s, e) => fired1 = true;

			var fired2 = false;
			EventHandler handler2 = (s, e) =>
			{
				fired2 = true;
				throw new NotImplementedException();
			};

			var fired3 = false;
			EventHandler handler3 = (s, e) => fired3 = true;

			var fired4 = false;
			EventHandler handler4 = (s, e) =>
			{
				fired4 = true;
				throw new NotImplementedException();
			};

			handler1 += handler2;
			handler1 += handler3;
			handler1 += handler4;

			var caught = default(Exception);
			try
			{
				handler1.SafeDynamicInvoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				caught = ex;
			}

			Assert.IsNotNull(caught);
			Assert.IsTrue(fired1);
			Assert.IsTrue(fired2);
			Assert.IsTrue(fired3);
			Assert.IsTrue(fired4);
		}

		[TestMethod]
		public void SingleAction()
		{
			var fired = false;
			Action handler = () => fired = true;
			var result = handler.SafeDynamicInvoke();
			Assert.IsTrue(fired);
			Assert.IsNull(result);
		}

		[TestMethod]
		public void MultipleFunctionsWithExceptions()
		{
			var fired1 = false;
			Func<int, string> handler1 = (i) =>
			{
				fired1 = true;
				return "handler1";
			};

			var fired2 = false;
			Func<int, string> handler2 = (i) =>
			{
				fired2 = true;
				throw new NotImplementedException();
			};

			var fired3 = false;
			Func<int, string> handler3 = (i) =>
			{
				fired3 = true;
				return "handler3";
			};

			var fired4 = false;
			Func<int, string> handler4 = (i) =>
			{
				fired4 = true;
				throw new NotImplementedException();
			};

			handler1 += handler2;
			handler1 += handler3;
			handler1 += handler4;

			var caught = default(Exception);
			try
			{
				handler1.SafeDynamicInvoke(123);
			}
			catch (Exception ex)
			{
				caught = ex;
			}

			Assert.IsNotNull(caught);
			Assert.IsTrue(fired1);
			Assert.IsTrue(fired2);
			Assert.IsTrue(fired3);
			Assert.IsTrue(fired4);
		}

		[TestMethod]
		public void MultipleFunctionsWithResults()
		{
			var fired1 = false;
			Func<int, string> handler1 = (i) =>
			{
				fired1 = true;
				return "handler1";
			};

			var fired2 = false;
			Func<int, string> handler2 = (i) =>
			{
				fired2 = true;
				return "handler2";
			};

			var fired3 = false;
			Func<int, string> handler3 = (i) =>
			{
				fired3 = true;
				return "handler3";
			};

			var fired4 = false;
			Func<int, string> handler4 = (i) =>
			{
				fired4 = true;
				return "handler4";
			};

			handler1 += handler2;
			handler1 += handler3;
			handler1 += handler4;

			var result = handler1.SafeDynamicInvoke(123);

			Assert.AreEqual("handler4", result);
			Assert.IsTrue(fired1);
			Assert.IsTrue(fired2);
			Assert.IsTrue(fired3);
			Assert.IsTrue(fired4);
		}

		private delegate int LongDelegate(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8);

		[TestMethod]
		public void VeryLongArgumentList()
		{
			var sum = 0;
			LongDelegate handler = (a1, a2, a3, a4, a5, a6, a7, a8, b1, b2, b3, b4, b5, b6, b7, b8) =>
			{
				return (sum = a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + b1 + b2 + b3 + b4 + b5 + b6 + b7 + b8) * 2;
			};

			var result = handler.SafeDynamicInvoke(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
			Assert.AreEqual(272, result);
			Assert.AreEqual(136, sum);
		}
	}
}
