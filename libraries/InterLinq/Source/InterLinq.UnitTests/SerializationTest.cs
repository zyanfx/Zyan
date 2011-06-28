using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using InterLinq.Expressions;

namespace InterLinq.UnitTests
{
	/// <summary>
	/// Expression serialization tests
	/// </summary>
	[TestClass]
	public class SerializationTest
	{
		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext { get; set; }

		/// <summary>
		/// Simple expression
		/// </summary>
		[TestMethod]
		public void TestSimpleSerialization()
		{
			// http://interlinq.codeplex.com/discussions/60896
			Expression<Func<Guid, bool>> expression = (guid) => guid != Guid.Empty;

			var sx = expression.MakeSerializable();
			var dx = sx.Deserialize();

			Assert.AreEqual(expression.ToString(), dx.ToString());
		}

		/// <summary>
		/// A bit more complex expression
		/// </summary>
		[TestMethod]
		public void TestComplexSerialization()
		{
			Expression<Func<Expression, bool>> expression = t => t.Type.FullName.ToLower().EndsWith("e");
			var sx = expression.MakeSerializable();
			var dx = sx.Deserialize();

			Assert.AreEqual(expression.ToString(), dx.ToString());
		}
	}
}
