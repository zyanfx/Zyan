using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Zyan.InterLinq;
using Zyan.InterLinq.Expressions;

namespace InterLinq.UnitTests
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
	using Owner = DummyAttribute;
	using TestContext = System.Object;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

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

		class Sample
		{
			public event EventHandler TestEvent;

			public Expression<Func<Sample, Delegate>> GetExpression()
			{
				return x => x.TestEvent;
			}
		}

		/// <summary>
		/// Expression with member access.
		/// </summary>
		[TestMethod]
		public void TestMemberExpressionSerialization()
		{
			Expression<Func<Sample, Delegate>> expression = new Sample().GetExpression();
			var sx = expression.MakeSerializable();
			var dx = sx.Deserialize();

			Assert.AreEqual(expression.ToString(), dx.ToString());
		}
	}
}
