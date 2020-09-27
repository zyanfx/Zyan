using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
	/// Test class for event stub.
	///</summary>
	[TestClass]
	public class EmptyDelegateFactoryTests
	{
		[TestMethod]
		public void EventHandler()
		{
			var handler = EmptyDelegateFactory.CreateEmptyDelegate<EventHandler>();
			Assert.IsNotNull(handler);

			var fired = false;
			EventHandler myHandler = (sender, e) => fired = true;

			handler(null, EventArgs.Empty);
			Assert.IsFalse(fired);

			handler += myHandler;
			handler(null, EventArgs.Empty);
			Assert.IsTrue(fired);

			fired = false;
			handler -= myHandler;
			handler(null, EventArgs.Empty);
			Assert.IsFalse(fired);
		}

		[TestMethod]
		public void EventHandler2()
		{
			var handler1 = EmptyDelegateFactory.CreateEmptyDelegate<EventHandler>();
			var fired1 = false;
			handler1 += (sender, e) => fired1 = true;

			var handler2 = EmptyDelegateFactory.CreateEmptyDelegate<EventHandler>();
			var fired2 = false;
			handler2 += (sender, e) => fired2 = true;

			handler1(this, EventArgs.Empty);
			Assert.IsTrue(fired1);
			Assert.IsFalse(fired2);

			fired1 = false;
			handler2(null, EventArgs.Empty);
			Assert.IsFalse(fired1);
			Assert.IsTrue(fired2);
		}

		[TestMethod]
		public void EventHandlerCancelEventArgs()
		{
			var handler = EmptyDelegateFactory.CreateEmptyDelegate<EventHandler<CancelEventArgs>>();
			Assert.IsNotNull(handler);

			var fired = false;
			EventHandler<CancelEventArgs> myHandler = (sender, e) => fired = true;

			handler(null, new CancelEventArgs());
			Assert.IsFalse(fired);

			handler += myHandler;
			handler(null, new CancelEventArgs());
			Assert.IsTrue(fired);

			fired = false;
			handler -= myHandler;
			handler(null, new CancelEventArgs());
			Assert.IsFalse(fired);
		}

		[TestMethod]
		public void EventHandlerCancelEventArgs2()
		{
			var handler1 = EmptyDelegateFactory.CreateEmptyDelegate<EventHandler<CancelEventArgs>>();
			var fired1 = false;
			handler1 += (sender, e) => fired1 = true;

			var handler2 = EmptyDelegateFactory.CreateEmptyDelegate<EventHandler<CancelEventArgs>>();
			var fired2 = false;
			handler2 += (sender, e) => fired2 = true;

			handler1(this, new CancelEventArgs());
			Assert.IsTrue(fired1);
			Assert.IsFalse(fired2);

			fired1 = false;
			handler2(null, new CancelEventArgs());
			Assert.IsFalse(fired1);
			Assert.IsTrue(fired2);
		}

		[TestMethod]
		public void Action()
		{
			var handler = EmptyDelegateFactory.CreateEmptyDelegate<Action>();
			Assert.IsNotNull(handler);

			var fired = false;
			Action myHandler = () => fired = true;

			handler();
			Assert.IsFalse(fired);

			handler += myHandler;
			handler();
			Assert.IsTrue(fired);

			fired = false;
			handler -= myHandler;
			handler();
			Assert.IsFalse(fired);
		}

		[TestMethod]
		public void Action2()
		{
			var handler1 = EmptyDelegateFactory.CreateEmptyDelegate<Action>();
			var fired1 = false;
			handler1 += () => fired1 = true;

			var handler2 = EmptyDelegateFactory.CreateEmptyDelegate<Action>();
			var fired2 = false;
			handler2 += () => fired2 = true;

			handler1();
			Assert.IsTrue(fired1);
			Assert.IsFalse(fired2);

			fired1 = false;
			handler2();
			Assert.IsFalse(fired1);
			Assert.IsTrue(fired2);
		}

		[TestMethod]
		public void FuncWithArguments()
		{
			var handler = EmptyDelegateFactory.CreateEmptyDelegate<Func<string, int>>();
			Assert.IsNotNull(handler);

			var fired = false;
			Func<string, int> myHandler = s =>
			{
				fired = true;
				Assert.AreEqual("Goodbye", s);
				return 1;
			};

			Assert.AreEqual(default(int), handler("Hello"));
			Assert.IsFalse(fired);

			handler += myHandler;
			Assert.AreEqual(1, handler("Goodbye"));
			Assert.IsTrue(fired);

			fired = false;
			handler -= myHandler;
			Assert.AreEqual(default(int), handler(string.Empty));
			Assert.IsFalse(fired);
		}

		[TestMethod]
		public void FuncWithArguments2()
		{
			var handler1 = EmptyDelegateFactory.CreateEmptyDelegate<Func<int, string>>();
			var fired1 = false;
			handler1 += i =>
			{
				fired1 = true;
				Assert.AreEqual(123, i);
				return "123";
			};

			var handler2 = EmptyDelegateFactory.CreateEmptyDelegate<Func<int, string>>();
			var fired2 = false;
			handler2 += i =>
			{
				fired2 = true;
				Assert.AreEqual(321, i);
				return "321";
			};

			Assert.AreEqual("123", handler1(123));
			Assert.IsTrue(fired1);
			Assert.IsFalse(fired2);

			fired1 = false;
			Assert.AreEqual("321", handler2(321));
			Assert.IsFalse(fired1);
			Assert.IsTrue(fired2);
		}
	}
}
