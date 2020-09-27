using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;

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
	/// Test class for invocation details.
	///</summary>
	[TestClass]
	public class InvocationDetailsTests
	{
		interface IService
		{
			void Foo();

			void Bar();
		}

		class Service : IService
		{
			public void Foo() { }

			void IService.Bar() { }
		}

		[TestMethod]
		public void InvocationDetailsObtainPublicMethodInfo()
		{
			var foo = new InvocationDetails
			{
				Type = typeof(Service),
				InterfaceType = typeof(IService),
				MethodName = "Foo",
				ParamTypes = new Type[0],
				GenericArguments = new Type[0]
			};

			Assert.IsTrue(foo.FindMethodInfo());
			Assert.IsNotNull(foo.MethodInfo);
			Assert.AreEqual(foo.MethodName, foo.MethodInfo.Name);
		}

		[TestMethod]
		public void InvocationDetailsObtainPrivateMethodInfo()
		{
			var bar = new InvocationDetails
			{
				Type = typeof(Service),
				InterfaceType = typeof(IService),
				MethodName = "Bar",
				ParamTypes = new Type[0],
				GenericArguments = new Type[0]
			};

			Assert.IsTrue(bar.FindMethodInfo());
			Assert.IsNotNull(bar.MethodInfo);
			Assert.AreNotEqual(bar.MethodName, bar.MethodInfo.Name);
		}

		[TestMethod]
		public void InvocationDetailsUnknownMethod()
		{
			var baz = new InvocationDetails
			{
				Type = typeof(Service),
				InterfaceType = typeof(IService),
				MethodName = "Baz",
				ParamTypes = new Type[0],
				GenericArguments = new Type[0]
			};

			Assert.IsFalse(baz.FindMethodInfo());
			Assert.IsNull(baz.MethodInfo);
		}
	}
}
