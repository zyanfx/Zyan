using System;
using System.Linq;
using Zyan.Communication;
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
#endif
	#endregion

	/// <summary>
	/// Test class for dynamic wires.
	///</summary>
	[TestClass]
	public class DynamicWireTests
	{
		public TestContext TestContext { get; set; }

		/// <summary>
		/// Mock DynamicWire class.
		/// </summary>
		class MockWire<T> : DynamicWire<T>
		{
			public MockWire(Func<object[], object> handler)
			{
				Handler = handler;
			}

			Func<object[], object> Handler { get; set; }

			protected override object InvokeClientDelegate(params object[] args)
			{
				return Handler(args);
			}
		}

		/// <summary>
		/// Sample void delegate.
		/// </summary>
		public delegate void VoidMethod(int intValue, DateTime dateValue);

		/// <summary>
		/// Sample non-void delegate.
		/// </summary>
		public delegate string NonVoidMethod(char charValue);

		[TestMethod]
		public void TestDynamicWireForAction()
		{
			var executed = false;
			var wire = new MockWire<Action>((args) =>
			{
				Assert.IsNotNull(args);
				Assert.AreEqual(0, args.Length);

				executed = true;
				return null;
			});

			wire.In();
			Assert.IsTrue(executed);
		}

		[TestMethod]
		public void TestDynamicWireForActionWithParameters()
		{
			var arg0 = int.MaxValue;
			var arg1 = DateTime.Now;
			var arg2 = String.Empty;

			var executed = false;
			var wire = new MockWire<Action<int, DateTime, string>>((args) =>
			{
				Assert.IsNotNull(args);
				Assert.AreEqual(3, args.Length);
				Assert.AreEqual(arg0, args[0]);
				Assert.AreEqual(arg1, args[1]);
				Assert.AreEqual(arg2, args[2]);

				executed = true;
				return null;
			});

			wire.In(arg0, arg1, arg2);
			Assert.IsTrue(executed);
		}

		[TestMethod]
		public void TestDynamicWireForFunc()
		{
			var executed = false;
			var wire = new MockWire<Func<int>>((args) =>
			{
				Assert.IsNotNull(args);
				Assert.AreEqual(0, args.Length);

				executed = true;
				return int.MaxValue;
			});

			var result = wire.In();
			Assert.IsTrue(executed);
			Assert.AreEqual(int.MaxValue, result);
		}

		[TestMethod]
		public void TestDynamicWireForFuncWithParameters()
		{
			var arg0 = int.MaxValue;
			var arg1 = DateTime.Now;
			var arg2 = String.Empty;
			var res = GetType().FullName;

			var executed = false;
			var wire = new MockWire<Func<int, DateTime, string, string>>((args) =>
			{
				Assert.IsNotNull(args);
				Assert.AreEqual(3, args.Length);
				Assert.AreEqual(arg0, args[0]);
				Assert.AreEqual(arg1, args[1]);
				Assert.AreEqual(arg2, args[2]);

				executed = true;
				return res;
			});

			var result = wire.In(arg0, arg1, arg2);
			Assert.IsTrue(executed);
			Assert.AreEqual(res, result);
		}

		[TestMethod]
		public void TestDynamicWireForVoidMethod()
		{
			var arg0 = int.MaxValue;
			var arg1 = DateTime.Now;

			var executed = false;
			var wire = new MockWire<VoidMethod>((args) =>
			{
				Assert.IsNotNull(args);
				Assert.AreEqual(2, args.Length);
				Assert.AreEqual(arg0, args[0]);
				Assert.AreEqual(arg1, args[1]);

				executed = true;
				return null;
			});

			wire.In(arg0, arg1);
			Assert.IsTrue(executed);
		}

		[TestMethod]
		public void TestDynamicWireForNonVoidMethod()
		{
			var arg0 = '1';
			var res = "TestDynamicWireForNonVoidMethod";

			var executed = false;
			var wire = new MockWire<NonVoidMethod>((args) =>
			{
				Assert.IsNotNull(args);
				Assert.AreEqual(1, args.Length);
				Assert.AreEqual(arg0, args[0]);

				executed = true;
				return res;
			});

			var result = wire.In(arg0);
			Assert.IsTrue(executed);
			Assert.AreEqual(res, result);
		}

		[TestMethod]
		public void GetDynamicWireReturnsDynamicWire()
		{
			var executed = false;
			var wire = new MockWire<VoidMethod>(args =>
			{
				executed = true;
				Assert.IsNotNull(args);
				Assert.AreEqual(2, args.Length);
				Assert.AreEqual(123, args[0]);
				return null;
			});

			Assert.IsFalse(executed);
			wire.In(123, DateTime.MinValue);
			Assert.IsTrue(executed);

			Assert.AreSame(wire, DynamicWireFactory.GetDynamicWire(wire.In));
			Assert.AreSame(wire, DynamicWireFactory.GetDynamicWire(wire.InDelegate));
		}
	}
}
