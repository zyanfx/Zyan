using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Zyan.Communication;
using Zyan.Communication.Protocols.Ipc;
using Zyan.InterLinq;

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
	/// Test class for Linq stuff
	/// </summary>
	[TestClass]
	public class LinqTests
	{
		#region Interfaces and components

		interface ISampleService
		{
			string CompileAndExecuteExpression(Expression<Func<string, string>> ex, string data);

			Expression ProcessExpression(Expression<Action<string>> ex, string data);
		}

		class SampleService : ISampleService
		{
			public string CompileAndExecuteExpression(Expression<Func<string, string>> ex, string data)
			{
				var func = ex.Compile();
				return func(data);
			}

			public Expression ProcessExpression(Expression<Action<string>> ex, string data)
			{
				return Expression.Invoke(ex, Expression.Constant(data));
			}
		}

		interface IMixedInterface
		{
			void OverloadedMethod();

			IEnumerable<T> OverloadedMethod<T>() where T : class, new();

			IQueryable<T> Query<T>() where T : class, new();
		}

		class MixedComponent : IMixedInterface
		{
			public void OverloadedMethod()
			{
			}

			public IEnumerable<T> OverloadedMethod<T>() where T : class, new()
			{
				foreach (var i in Enumerable.Range(1, 10))
					yield return new T();

				yield break;
			}

			public IQueryable<T> Query<T>() where T : class, new()
			{
				return Enumerable.Range(1, 5).Select(i => new T()).ToArray().AsQueryable();
			}
		}

		class Sample
		{
			static Random random = new Random();

			public int Value { get; private set; }

			public Sample()
			{
				Value = random.Next(500, 1000);
			}
		}

		#endregion

		public TestContext TestContext { get; set; }

		static ZyanComponentHost ZyanHost { get; set; }

		static ZyanConnection ZyanConnection { get; set; }

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			StartServer(null);
		}

		[ClassCleanupNonStatic]
		public void Cleanup()
		{
			StopServer();
		}

		[ClassInitialize]
		public static void StartServer(TestContext ctx)
		{
			var serverSetup = new IpcBinaryServerProtocolSetup("LinqTest");
			ZyanHost = new ZyanComponentHost("SampleQueryableServer", serverSetup);

			ZyanHost.RegisterComponent<ISampleService, SampleService>();
			ZyanHost.RegisterQueryableComponent(new SampleObjectSource(new[] { "Hello", "World!" }));
			ZyanHost.RegisterQueryableComponent("Sample1", new SampleObjectSource(new[] { "this", "is", "an", "example" }));
			ZyanHost.RegisterQueryableComponent("Sample2", () => new SampleObjectSource(new[] { "lorem", "ipsum", "dolor", "sit", "amet" }));
			ZyanHost.RegisterQueryableComponent("Sample3", () => new SampleObjectSource(new[] { "consectetur", "adipisicing", "elit" }), ActivationType.SingleCall);
			ZyanHost.RegisterQueryableComponent("Sample4", (Type t) => new object[] { "quietly", "turning", "the", "backdoor", "key" });
			ZyanHost.RegisterQueryableComponent("Sample5", (Type t) => (new object[] { "stepping", "outside", "she", "is", "free" }).AsQueryable());
			ZyanHost.RegisterQueryableComponent<SampleObjectSource>("Sample6");
			ZyanHost.RegisterQueryableComponent<SampleObjectSource>("Sample7", ActivationType.SingleCall);
			ZyanHost.RegisterQueryableComponent("DbSample", new DataWrapper());
			ZyanHost.RegisterComponent<IMixedInterface, MixedComponent>();

			var clientSetup = new IpcBinaryClientProtocolSetup();
			ZyanConnection = new ZyanConnection("ipc://LinqTest/SampleQueryableServer", clientSetup);
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		[TestMethod]
		public void TestUntitledComponent()
		{
			var proxy = ZyanConnection.CreateQueryableProxy();
			var query =
				from s in proxy.Get<string>()
				where s.EndsWith("!")
				select s;

			var result = query.FirstOrDefault();
			Assert.IsNotNull(result);
			Assert.AreEqual("World!", result);
		}

		[TestMethod]
		public void TestSample1Component()
		{
			var proxy = ZyanConnection.CreateQueryableProxy("Sample1");
			var query =
				from s in proxy.Get<string>()
				where s.Length > 2
				orderby s
				select s + s.ToUpper();

			var result = String.Concat(query);
			Assert.AreEqual("exampleEXAMPLEthisTHIS", result);
		}

		[TestMethod]
		public void TestSample2Component()
		{
			var proxy = ZyanConnection.CreateQueryableProxy("Sample2");
			var query =
				from s in proxy.Get<string>()
				orderby s
				select s.ToUpper();

			var result = String.Join(" ", query);
			Assert.AreEqual("AMET DOLOR IPSUM LOREM SIT", result);
		}

		[TestMethod]
		public void TestSample3Component()
		{
			var proxy = ZyanConnection.CreateQueryableProxy("Sample3");
			var query =
				from s in proxy.Get<string>()
				orderby s.Length
				select s.Substring(0, 3);

			var result = String.Concat(query);
			Assert.AreEqual("eliconadi", result);
		}

		[TestMethod]
		public void TestSample4Component()
		{
			var proxy = ZyanConnection.CreateQueryableProxy("Sample4");
			var query =
				from s in proxy.Get<string>()
				orderby s
				select s.Substring(0, 1);

			var result = String.Concat(query);
			Assert.AreEqual("bkqtt", result);
		}

		[TestMethod]
		public void TestSample5Component()
		{
			var proxy = ZyanConnection.CreateQueryableProxy("Sample5");
			var query =
				from s in proxy.Get<string>()
				orderby s descending
				select s.Substring(0, 1);

			var result = String.Concat(query);
			Assert.AreEqual("ssoif", result);
		}

		[TestMethod]
		public void TestSample6Component()
		{
			var proxy = ZyanConnection.CreateQueryableProxy("Sample6");
			var query =
				from s in proxy.Get<string>()
				where s == "fox" || s == "dog" || s == "frog" || s == "mouse"
				select s.Replace('o', 'i');

			var result = String.Join(" & ", query);
			Assert.AreEqual("fix & dig", result);
		}

		[TestMethod]
		public void TestSample7Component()
		{
			var proxy = ZyanConnection.CreateQueryableProxy("Sample7");
			var query =
				from s in proxy.Get<string>()
				where Regex.IsMatch(s, "[nyg]$")
				select s;

			var result = String.Join(" ", query);
			Assert.AreEqual("brown lazy dog", result);
		}

		[TestMethod]
		public void TestDbSampleComponent1()
		{
			var proxy = ZyanConnection.CreateQueryableProxy("DbSample");
			var query =
				from s in proxy.Get<SampleEntity>()
				where Regex.IsMatch(s.FirstName, "[rt]$")
				select s.LastName;

			var result = String.Join(" ", query);
			Assert.AreEqual("Einstein Friedmann Kapitsa Oppenheimer Compton Lawrence Wilson Kurchatov", result);
		}

		[TestMethod]
		public void TestDbSampleComponent2()
		{
			var proxy = ZyanConnection.CreateQueryableProxy("DbSample");
			var query =
				from s in proxy.Get<SampleEntity>()
				orderby s.FirstName.Length, s.FirstName
				select s.FirstName;

			var result = String.Join(", ", query);
			Assert.AreEqual(
				"Leó, Lev, Hans, Igor, Glenn, James, Klaus, Leona, Niels, Pyotr, Ralph, Albert, Arthur, Edward, Emilio, " +
				"Enrico, Ernest, George, Harold, Robert, Robert, Richard, William, Alexander, Stanislaw, Chien-Shiung", result);
		}

		[TestMethod]
		public void TestExpressionParameter()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleService>();
			Expression<Func<string, string>> ex =
				s => s.ToLower() + "-" + s.ToUpper();

			var result = proxy.CompileAndExecuteExpression(ex, "Ru");
			Assert.AreEqual("ru-RU", result);
		}

		[TestMethod]
		public void TestExpressionReturnValue()
		{
			const string message = "Hello, World!";

			var proxy = ZyanConnection.CreateProxy<ISampleService>();
			Expression<Action<string>> ex = s => Console.WriteLine(s);
			Expression expression = Expression.Invoke(ex, Expression.Constant(message));

			var result = proxy.ProcessExpression(ex, message);
			Assert.AreEqual(expression.ToString(), result.ToString());
		}

		[TestMethod]
		public void TestMixedQueryable()
		{
			var mc = ZyanConnection.CreateProxy<IMixedInterface>();
			var count = mc.Query<StringBuilder>().Count();
			Assert.AreEqual(count, 5);
		}

		[TestMethod]
		public void TestMixedEnumerable()
		{
			var mc = ZyanConnection.CreateProxy<IMixedInterface>();

			var count = 0;
			foreach (var value in mc.OverloadedMethod<StringBuilder>())
				count += value.Length + 1;

			Assert.AreEqual(count, 10);
		}
	}
}
