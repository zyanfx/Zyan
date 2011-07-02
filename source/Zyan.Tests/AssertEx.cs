using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Tests
{
	#region Unit testing platform abstraction layer
#if NUNIT
	using NUnit.Framework;
	using TestClass = NUnit.Framework.TestFixtureAttribute;
	using TestMethod = NUnit.Framework.TestAttribute;
	using ClassInitializeParameterless = NUnit.Framework.TestFixtureSetUpAttribute;
	using ClassInitialize = DummyAttribute;
	using ClassCleanup = NUnit.Framework.TestFixtureTearDownAttribute;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeParameterless = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Extended version of Assert class.
	/// </summary>
	internal static class AssertEx
	{
		public static void IsInstanceOf<T>(object obj)
		{
#if NUNIT
			Assert.IsInstanceOf<T>(obj);
#else
			Assert.IsInstanceOfType(obj, typeof(T));
#endif
		}

		public static void IsNotInstanceOf<T>(object obj)
		{
#if NUNIT
			Assert.IsInstanceOf<T>(obj);
#else
			Assert.IsNotInstanceOfType(obj, typeof(T));
#endif
		}
	}
}
