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
	public class ExtensionsTests
	{
		[TestMethod]
		public void IsNullOrEmptyWorksAsExpected()
		{
			var array = new byte[0];
			Assert.IsTrue(array.IsNullOrEmpty());

			array = null;
			Assert.IsTrue(array.IsNullOrEmpty());

			array = new byte[] { 1, 2, 3 };
			Assert.IsFalse(array.IsNullOrEmpty());
		}

		[TestMethod]
		public void EmptyIfNullWorksAsExpected()
		{
			var array = "Hello".ToCharArray();
			Assert.AreSame(array, array.EmptyIfNull());

			array = null;
			Assert.IsNotNull(array.EmptyIfNull());
			Assert.IsFalse(array.EmptyIfNull().Any());
		}
	}
}
