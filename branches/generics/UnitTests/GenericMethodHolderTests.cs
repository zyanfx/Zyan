using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using GenericWrappers;

namespace UnitTests
{
	[TestFixture]
	public class GenericMethodHolderTests
	{
		[Test]
		public void MethodLookupTest1()
		{ 
			var holder = new GenericMethodHolder(typeof(GenericMethodHolderTests), "MyTest", 2, null, null, typeof(int), typeof(bool));
			var result = holder.Invoke(this, new[] { typeof(object), typeof(object) }, null, null, 1, true);
			Assert.AreEqual("MyTest<T, X>(T t, X x, int a, bool b)", result);
			Assert.AreEqual("System.String MyTest[T,X](T, X, Int32, Boolean)", holder.ToString());
		}

		[Test]
		public void MethodLookupTest2()
		{
			var holder = new GenericMethodHolder(typeof(GenericMethodHolderTests), "MyTest", 2, typeof(int), typeof(string));
			var result1 = holder.Invoke(this, new[] { typeof(object), typeof(object) }, 1, "123");
			var result2 = holder.Invoke(this, new[] { typeof(object), typeof(object) }, 1, "123"); // test cache
			Assert.AreEqual("MyTest<T, X>(int a, string b)", result1);
			Assert.AreEqual(result1, result2);
		}

		[Test]
		public void MethodLookupTest3()
		{
			var holder = new GenericMethodHolder(typeof(GenericMethodHolderTests), "MyTest", 2);
			var result = holder.Invoke(this, new[] { GetType(), GetType() });
			Assert.AreEqual("MyTest<T, X>()", result);
		}

		[Test, ExpectedException(typeof(ApplicationException)), CoverageExclude]
		public void MethodLookupFailureTest()
		{
			var holder = new GenericMethodHolder(typeof(GenericMethodHolderTests), "MyTest", 0, typeof(int), typeof(bool));
		}

		#region Methods to lookup

		public string MyTest<T, X>(T t, X x, int a, bool b)
		{
			return "MyTest<T, X>(T t, X x, int a, bool b)";
		}

		[CoverageExclude]
		public string MyTest<T, X>(int a, bool b)
		{
			throw new AssertionException("This method should not be called");
		}

		public string MyTest<T, X>(int a, string b)
		{
			return "MyTest<T, X>(int a, string b)";
		}

		public string MyTest<T, X>()
		{
			return "MyTest<T, X>()";
		}

		[CoverageExclude]
		public string MyTest(int a, bool b)
		{
			throw new AssertionException("This method should never be picked up as it's not generic");
		}

		#endregion
	}
}
