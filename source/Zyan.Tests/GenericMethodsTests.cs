using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Zyan.Communication;
using Zyan.Communication.Protocols.Ipc;
using System.Diagnostics;

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
	/// Tests for generic method invocation.
	/// </summary>
	[TestClass]
	public class GenericMethodsTest
	{
		#region Interfaces and components

		/// <summary>
		/// Sample server interface
		/// </summary>
		public interface ISampleServer
		{
			string ProcessData<T>(T data);

			int OverloadedMethod(string a, int b, DateTime c);

			T Duplicate<T>(T data);

			T GetValue<T>(string name);

			T OverloadedMethod<T>(string a, T b, DateTime c);
		}

		/// <summary>
		/// Sample server implementation
		/// </summary>
		public class SampleServer : ISampleServer
		{
			public string ProcessData<T>(T data)
			{
				return data.ToString().ToLower();
			}

			public T Duplicate<T>(T data)
			{
				var result = default(object);

				if (typeof(int).IsAssignableFrom(typeof(T)))
				{
					result = (int)(object)data * 2;
				}

				if (typeof(string).IsAssignableFrom(typeof(T)))
				{
					result = data.ToString() + data.ToString();
				}

				return (T)result;
			}

			public int OverloadedMethod(string a, int b, DateTime c)
			{
				return String.Format(a, b, c).Length;
			}

			public T OverloadedMethod<T>(string a, T b, DateTime c)
			{
				var result = String.Format(a, b, c);

				if (typeof(string).IsAssignableFrom(typeof(T)))
				{
					return (T)(object)result;
				}

				if (typeof(int).IsAssignableFrom(typeof(T)))
				{
					return (T)(object)result.Length;
				}

				return default(T);
			}

			public T GetValue<T>(string propertyName)
			{
				var proc = Process.GetCurrentProcess();
				var prop = proc.GetType().GetProperty(propertyName);
				return (T)prop.GetValue(proc, null);
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
			var serverSetup = new IpcBinaryServerProtocolSetup("GenericMethodTest");
			ZyanHost = new ZyanComponentHost("GenericServer", serverSetup);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer>();

			var clientSetup = new IpcBinaryClientProtocolSetup();
			ZyanConnection = new ZyanConnection("ipc://GenericMethodTest/GenericServer", clientSetup);
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		[TestMethod]
		public void TestGenericArguments()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var result = proxy.ProcessData(123);
			Assert.AreEqual("123", result);

			result = proxy.ProcessData("Hello World!");
			Assert.AreEqual("hello world!", result);
		}

		[TestMethod]
		public void TestGenericReturnValue1()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var proc = Process.GetCurrentProcess();

			var id1 = proc.Id;
			var id2 = proxy.GetValue<int>("Id");
			Assert.AreEqual(id1, id2);

			var mn1 = proc.MachineName;
			var mn2 = proxy.GetValue<string>("MachineName");
			Assert.AreEqual(mn1, mn2);

			var st1 = proc.StartTime;
			var st2 = proxy.GetValue<DateTime>("StartTime");
			Assert.AreEqual(st1, st2);
		}
		
		[TestMethod]
		public void TestGenericReturnValue2()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var intResult = proxy.Duplicate(123);
			Assert.AreEqual(246, intResult);

			var strResult = proxy.Duplicate("He");
			Assert.AreEqual("HeHe", strResult);

			var result = proxy.Duplicate(new object());
			Assert.IsNull(result);
		}

		[TestMethod]
		public void TestOverloadedMethods()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var format = "Result: {0}, {1:dd MM yy}";

			// non-generic call
			var intResult = proxy.OverloadedMethod(format, 123, DateTime.MinValue);
			Assert.AreEqual(21, intResult);

			// generic call #1
			var strResult = proxy.OverloadedMethod(format, "123", DateTime.MinValue);
			Assert.AreEqual("Result: 123, 01 01 01", strResult);

			// generic call #2
			var result = proxy.OverloadedMethod(format, new object(), DateTime.MinValue);
			Assert.IsNull(result);
		}
	}
}
