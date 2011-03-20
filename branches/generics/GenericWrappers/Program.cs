using System;
using System.Diagnostics;

namespace GenericWrappers
{
	[CoverageExclude]
	class GenericTest
	{
		public T GetDefault<T>()
		{
			return default(T);
		}

		public bool Equals<T>(T a, T b)
		{
			return a.Equals(b);
		}
	}

	[CoverageExclude]
	class NonGenericWrapper
	{
		GenericTest instance;
		GenericMethodHolder getDefaultHolder;
		GenericMethodHolder equalsHolder;

		public NonGenericWrapper(GenericTest gt)
		{
			instance = gt;
			getDefaultHolder = new GenericMethodHolder(typeof(GenericTest), "GetDefault", 1, new Type[0]);
			equalsHolder = new GenericMethodHolder(typeof(GenericTest), "Equals", 1, new Type[] { null, null });
		}

		public object GetDefault_T(Type t1)
		{
			return getDefaultHolder.Invoke(instance, new[] { t1 }, new object[0]);
		}

		public bool Equals_T(Type t1, object a, object b)
		{
			return (bool)equalsHolder.Invoke(instance, new[] { t1 }, new[] { a, b });
		}
	}

	[CoverageExclude]
	class Program
	{
		const int Iterations = 100000;

		static void Main(string[] args)
		{
			var test = new GenericTest();
			Console.WriteLine("Default(int) = {0}", test.GetDefault<int>());
			Console.WriteLine("Default(string) = {0}", test.GetDefault<string>());
			Console.WriteLine("Default(Guid) = {0}", test.GetDefault<Guid>());
			Console.WriteLine("Equals(123, 123) = {0}", test.Equals(123, 123));
			Console.WriteLine("Equals(\"Some\", null) = {0}", test.Equals("Some", null));
			Console.WriteLine("--------------");

			var testWrapper = new NonGenericWrapper(test);
			Console.WriteLine("Default(int) = {0}", testWrapper.GetDefault_T(typeof(int)));
			Console.WriteLine("Default(string) = {0}", testWrapper.GetDefault_T(typeof(string)));
			Console.WriteLine("Default(Guid) = {0}", testWrapper.GetDefault_T(typeof(Guid)));
			Console.WriteLine("Equals(123, 123) = {0}", testWrapper.Equals_T(typeof(int), 123, 123));
			Console.WriteLine("Equals(\"Some\", null) = {0}", testWrapper.Equals_T(typeof(string), "Some", null));
			Console.WriteLine("--------------");

			var sw = new Stopwatch();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
			{
				var a = testWrapper.GetDefault_T(typeof(int));
				var b = testWrapper.GetDefault_T(typeof(string));
				var c = testWrapper.GetDefault_T(typeof(Guid));
				var d = testWrapper.Equals_T(typeof(int), 123, 123);
				var e = testWrapper.Equals_T(typeof(string), "Some", null);
			}
			sw.Stop();
			Console.WriteLine("Elapsed: {0}", sw.Elapsed);
			Console.ReadLine();
		}
	}
}
