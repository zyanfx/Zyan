using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using GenericWrappers;

namespace UnitTests
{
	[TestFixture]
	public class GenericParameterListTests
	{
		[Test]
		public void EqualityTest()
		{
			var pl1 = new GenericParameterList(typeof(int), typeof(string));
			var pl2 = new GenericParameterList(typeof(int), typeof(string));
			Assert.AreEqual(pl1, pl2);
		}

		[Test]
		public void NonEqualityTest()
		{
			var pl1 = new GenericParameterList(typeof(int), typeof(string));
			var pl2 = new GenericParameterList(typeof(Dictionary<,>));
			Assert.AreNotEqual(pl1, pl2);
		}

		[Test]
		public void CoverageRelatedTest()
		{
			var pl1 = new GenericParameterList(typeof(int), typeof(string));
			var pl2 = new object();
			Assert.AreNotEqual(pl1, pl2);
		}

		[Test]
		public void HashCodeTest()
		{
			var pl1 = new GenericParameterList(typeof(bool));
			var pl2 = new GenericParameterList(typeof(bool));
			Assert.AreEqual(pl1.GetHashCode(), pl2.GetHashCode());
		}
	}
}
