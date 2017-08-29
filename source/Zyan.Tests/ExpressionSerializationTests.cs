using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zyan.InterLinq;
using Zyan.InterLinq.Expressions;

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
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for ExpressionSerializationHandler.
	/// </summary>
	[TestClass]
	public class ExpressionSerializationTests
	{
		/// <summary>
		/// Gets or sets the test context which provides
		/// information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext { get; set; }

		private ExpressionSerializationHandler Handler { get; } = new ExpressionSerializationHandler();

		/// <summary>
		/// Simple expression
		/// </summary>
		[TestMethod]
		public void TestSimpleSerializationUsingHandler()
		{
			// http://interlinq.codeplex.com/discussions/60896
			Expression<Func<Guid, bool>> expression = (guid) => guid != Guid.Empty;

			var sx = Handler.Serialize(expression);
			Assert.IsTrue(sx.Length <= 4954); // original size: 5585
			var dx = Handler.Deserialize(expression.GetType(), sx);

			Assert.AreEqual(expression.ToString(), dx.ToString());
		}

		/// <summary>
		/// A bit more complex expression
		/// </summary>
		[TestMethod]
		public void TestComplexSerializationUsingHandler()
		{
			Expression<Func<Expression, bool>> expression = t => t.Type.FullName.ToLower().EndsWith("e");
			var sx = Handler.Serialize(expression);
			Assert.IsTrue(sx.Length <= 6681); // original size: 7342
			var dx = Handler.Deserialize(expression.GetType(), sx);

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
		public void TestMemberExpressionSerializationUsingHandler()
		{
			Expression<Func<Sample, Delegate>> expression = new Sample().GetExpression();
			var sx = Handler.Serialize(expression);
			Assert.IsTrue(sx.Length <= 3841); // original size: 4256
			var dx = Handler.Deserialize(expression.GetType(), sx);

			Assert.AreEqual(expression.ToString(), dx.ToString());
		}

		class Example
		{
			public int ID { get; set; }
		}

		[TestMethod]
		public void TestExpressionSerializationWithTwoNestedClosuresUsingHandler()
		{
			var managerList = new[] { 1, 2, 3 };
			var filter = GetFilter<Example>(r => r.ID < 0);

			// С# 6.0 produces nested closures here:
			{
				var locationId = 123;
				filter = GetFilter<Example>(r => managerList.Contains(r.ID) && r.ID == locationId);
			}

			var sx = Handler.Serialize(filter);
			Assert.IsTrue(sx.Length <= 7520); // original size: 8339
			var dx = Handler.Deserialize(filter.GetType(), sx);

			Assert.AreEqual("r => (value(System.Int32[]).Contains(r.ID) AndAlso (r.ID == 123))", dx.ToString());
		}

		private Expression<Func<T, bool>> GetFilter<T>(Expression<Func<T, bool>> filter)
		{
			return filter;
		}

		[TestMethod]
		public void TestExpressionSerializationWithManyNestedClosuresUsingHandler()
		{
			var managerList = new[] { 1, 2, 3 };
			var filter = GetFilter<Example>(r => r.ID < 0);
			{
				var locationId = 123;
				filter = GetFilter<Example>(r => managerList.Contains(r.ID) && r.ID == locationId);
				{
					var more = 0;
					filter = GetFilter<Example>(r => managerList.Contains(r.ID) && r.ID == locationId && r.ID > more);
					{
						var last = 321;
						filter = GetFilter<Example>(r => managerList.Contains(r.ID) && r.ID == locationId && r.ID > more && r.ID < last);
					}
				}
			}

			var sx = Handler.Serialize(filter);
			Assert.IsTrue(sx.Length <= 7848); // original size: 8667
			var dx = Handler.Deserialize(filter.GetType(), sx);

			Assert.AreEqual("r => (((value(System.Int32[]).Contains(r.ID) AndAlso (r.ID == 123)) AndAlso (r.ID > 0)) AndAlso (r.ID < 321))", dx.ToString());
		}
	}
}
