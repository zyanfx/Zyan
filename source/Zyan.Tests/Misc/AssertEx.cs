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
			Assert.IsInstanceOfType(typeof(T), obj);
#else
			Assert.IsInstanceOfType(obj, typeof(T));
#endif
		}

		public static void IsNotInstanceOf<T>(object obj)
		{
#if NUNIT
			Assert.IsNotInstanceOfType(typeof(T), obj);
#else
			Assert.IsNotInstanceOfType(obj, typeof(T));
#endif
		}

		public static void Throws<T>(Action action) where T : Exception
		{
			try
			{
				action();
			}
			catch (T)
			{
				// everything is fine!
				return;
			}
			catch (Exception ex)
			{
				Assert.Fail($"Expected to caught an exception of type {typeof(T)}, but got {ex.GetType()} istead.");
			}

			Assert.Fail($"Expected to caught an exception of type {typeof(T)}, but no exception was thrown.");
		}
	}
}
