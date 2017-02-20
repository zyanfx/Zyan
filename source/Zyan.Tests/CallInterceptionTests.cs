using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Zyan.Communication;
using Zyan.Communication.Protocols.Null;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;

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
	using TestInitialize = NUnit.Framework.SetUpAttribute;
	using TestCleanup = NUnit.Framework.TearDownAttribute;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for strong-typed call interceptor builder.
	/// </summary>
	[TestClass]
	public class CallInterceptionTests
	{
		#region Interfaces and components

		public interface IInterceptableComponent
		{
			void Procedure();

			void Procedure(long arg1, TimeSpan arg2, short arg3, IInterceptableComponent arg4);

			string Function(int a, DateTime b, TimeSpan c);

			bool GenericFunction<T>(int a, T b);

			event EventHandler ProcedureCalled;
		}

		public class InterceptableComponent : IInterceptableComponent
		{
			public void Procedure()
			{
				OnProcedureCalled();
			}

			public void Procedure(long arg1, TimeSpan arg2, short arg3, IInterceptableComponent arg4)
			{
				OnProcedureCalled();
			}

			public string Function(int a, DateTime b, TimeSpan c)
			{
				return string.Format("Called with arguments: {0}, {1}, {2}", a, b, c);
			}

			public bool GenericFunction<T>(int a, T b)
			{
				return false;
			}

			public event EventHandler ProcedureCalled;

			private void OnProcedureCalled()
			{
				if (ProcedureCalled != null)
					ProcedureCalled(null, EventArgs.Empty);
			}
		}

		#endregion

		#region Initialization and cleanup

		public TestContext TestContext { get; set; }

		private static ZyanComponentHost ZyanHost { get; set; }

		private static ZyanConnection ZyanConnection { get; set; }

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			StartServer(null);
		}

		[ClassCleanupNonStatic]
		public void Cleanup()
		{
		}

		[ClassInitialize]
		public static void StartServer(TestContext ctx)
		{
			ZyanSettings.LegacyBlockingEvents = true;
			ZyanSettings.LegacyBlockingSubscriptions = true;

			var serverSetup = new NullServerProtocolSetup(3456);
			ZyanHost = new ZyanComponentHost("CallInterceptorServer", serverSetup);
			ZyanHost.RegisterComponent<IInterceptableComponent, InterceptableComponent>(ActivationType.Singleton);

			ZyanConnection = new ZyanConnection("null://NullChannel:3456/CallInterceptorServer");
			ZyanConnection.CallInterceptionEnabled = true;
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		[TestCleanup]
		public void ClearCallInterceptors()
		{
			ZyanConnection.CallInterceptors.Clear();
		}

		#endregion

		[TestMethod]
		public void ParameterlessVoidMethod_IsIntercepted()
		{
			var flag = false;

			ZyanConnection.CallInterceptors.For<IInterceptableComponent>().Add(
				component => component.Procedure(),
				data =>
				{
					flag = true;
					data.Intercepted = true;
				});

			var proxy = ZyanConnection.CreateProxy<IInterceptableComponent>();
			proxy.Procedure();
			Assert.IsTrue(flag);
		}

		[TestMethod]
		public void VoidMethodWithParameters_IsIntercepted()
		{
			var flag = false;

			var interceptor = CallInterceptor.For<IInterceptableComponent>().Action<long, TimeSpan, short, IInterceptableComponent>(
				(component, arg1, arg2, arg3, arg4) => component.Procedure(arg1, arg2, arg3, arg4),
				(data, arg1, arg2, arg3, arg4) =>
				{
					if (arg1 == 123)
					{
						flag = true;
						data.Intercepted = true;
					}
				});

			ZyanConnection.CallInterceptors.Add(interceptor);
			var proxy = ZyanConnection.CreateProxy<IInterceptableComponent>();

			// not intercepted
			proxy.Procedure(321, TimeSpan.FromSeconds(1), 0, null);
			Assert.IsFalse(flag);

			// intercepted
			proxy.Procedure(123, TimeSpan.FromSeconds(1), 0, null);
			Assert.IsTrue(flag);
		}

		[TestMethod]
		public void FunctionWithParameters_IsIntercepted()
		{
			const string interceptedResult = "Intercepted!";

			var interceptor = CallInterceptor.For<IInterceptableComponent>().Func<int, DateTime, TimeSpan, string>(
				(component, arg1, arg2, arg3) => component.Function(arg1, arg2, arg3),
				(data, arg1, arg2, arg3) =>
				{
					if (arg1 == 123)
					{
						data.Intercepted = true;
						return interceptedResult;
					}

					// not intercepted
					return null;
				});

			ZyanConnection.CallInterceptors.Add(interceptor);
			var proxy = ZyanConnection.CreateProxy<IInterceptableComponent>();

			// not intercepted
			var result = proxy.Function(321, DateTime.Now, TimeSpan.FromSeconds(1));
			Assert.AreNotEqual(interceptedResult, result);

			// intercepted
			result = proxy.Function(123, DateTime.Today, TimeSpan.FromSeconds(1));
			Assert.AreEqual(interceptedResult, result);
		}

		[TestMethod]
		public void FunctionWithParameters_IsInterceptedAndRemoteMethodIsInvoked()
		{
			string interceptPrefix = "Intercepted:";

			var interceptor = CallInterceptor.For<IInterceptableComponent>().Func<int, DateTime, TimeSpan, string>(
				(component, arg1, arg2, arg3) => component.Function(arg1, arg2, arg3),
				(data, arg1, arg2, arg3) =>
				{
					if (arg1 == 123)
					{
						data.Intercepted = true;
						var realResult = data.MakeRemoteCall() ?? string.Empty;
						return interceptPrefix + realResult.ToString();
					}

					// not intercepted
					return null;
				});

			ZyanConnection.CallInterceptors.Add(interceptor);
			var proxy = ZyanConnection.CreateProxy<IInterceptableComponent>();

			// not intercepted
			var result = proxy.Function(321, DateTime.Now, TimeSpan.FromSeconds(1));
			Assert.IsFalse(string.IsNullOrWhiteSpace(result));
			Assert.IsFalse(result.StartsWith(interceptPrefix));

			// intercepted
			result = proxy.Function(123, DateTime.Today, TimeSpan.FromSeconds(1));
			Assert.AreNotEqual(interceptPrefix, result);
			Assert.IsTrue(result.StartsWith(interceptPrefix));
		}

		[TestMethod]
		public void GenericFunctionWithParameters_IsIntercepted()
		{
			var interceptor = CallInterceptor.For<IInterceptableComponent>().Func<int, Guid, bool>(
				(component, arg1, arg2) => component.GenericFunction(arg1, arg2),
				(data, arg1, arg2) =>
				{
					if (arg1 == 123)
					{
						data.Intercepted = true;
						return true;
					}

					// not intercepted
					return false;
				});

			ZyanConnection.CallInterceptors.Add(interceptor);
			var proxy = ZyanConnection.CreateProxy<IInterceptableComponent>();

			// not intercepted
			var result = proxy.GenericFunction(321, Guid.Empty);
			Assert.IsFalse(result);

			// intercepted
			result = proxy.GenericFunction(123, Guid.Empty);
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void EventHandlerSubscriptionIsIntercepted()
		{
			var intercepted = false;
			var procedureCalled = false;

			var interceptor = new CallInterceptor(typeof(IInterceptableComponent),
				MemberTypes.Method, "add_ProcedureCalled", new[] { typeof(EventHandler) }, data =>
				{
					intercepted = true;
				});

			ZyanConnection.CallInterceptors.Add(interceptor);

			var proxy = ZyanConnection.CreateProxy<IInterceptableComponent>();
			proxy.ProcedureCalled += (sender, args) => procedureCalled = true;
			Assert.IsFalse(procedureCalled);
			Assert.IsTrue(intercepted);

			proxy.Procedure();
			Assert.IsTrue(procedureCalled);
		}

		[TestMethod]
		public void EventHandlerUnsubscriptionIsIntercepted()
		{
			var intercepted = false;
			var procedureCalled = false;

			var interceptor = new CallInterceptor(typeof(IInterceptableComponent),
				MemberTypes.Method, "remove_ProcedureCalled", new[] { typeof(EventHandler) }, data =>
				{
					intercepted = true;
				});

			ZyanConnection.CallInterceptors.Add(interceptor);

			var proxy = ZyanConnection.CreateProxy<IInterceptableComponent>();
			var handler = new EventHandler((sender, args) => procedureCalled = true);
			proxy.ProcedureCalled += handler;
			Assert.IsFalse(procedureCalled);
			Assert.IsFalse(intercepted);

			proxy.Procedure();
			Assert.IsTrue(procedureCalled);
			Assert.IsFalse(intercepted);

			proxy.ProcedureCalled -= handler;
			Assert.IsTrue(procedureCalled);
			Assert.IsTrue(intercepted);
		}

		[TestMethod]
		public void CallInterceptorRegistrationThreadingTest()
		{
			var interceptor = CallInterceptor.For<IInterceptableComponent>().Action(c => c.Procedure(), c =>
			{
				c.Intercepted = true;
			});

			ZyanConnection.CallInterceptors.Clear();
			var proxy = ZyanConnection.CreateProxy<IInterceptableComponent>();
			var count = 100000;

			var action1 = new Action(async () =>
			{
				for (var i = 0; i < count; i++)
				{
					ZyanConnection.CallInterceptors.Add(interceptor);
					await Task.Yield();
				}
			});

			var action2 = new Action(async () =>
			{
				for (var i = 0; i < count; i++)
				{
					proxy.Procedure();
					await Task.Delay(0);
				}
			});

			Task.WaitAll(Task.Run(action1), Task.Run(action2));
		}

		[TestMethod]
		public void CallInterceptionBug()
		{
			using (var host = new ZyanComponentHost("FirstTestServer", 18888))
			{
				host.RegisterComponent<ITestService, TestService>(ActivationType.Singleton);

				using (var connection = new ZyanConnection("tcp://127.0.0.1:18888/FirstTestServer") { CallInterceptionEnabled = true })
				{
					// add a call interceptor for the TestService.TestMethod
					connection.CallInterceptors.Add(CallInterceptor.For<ITestService>().Action(service => service.TestMethod(), data =>
					{
						data.Intercepted = true;
						data.MakeRemoteCall();
						data.ReturnValue = nameof(TestService);
					}));

					var testService = connection.CreateProxy<ITestService>(null);
					var result = testService.TestMethod();
					Assert.AreEqual(nameof(TestService), result);
				}
			}
		}

		public interface ITestService
		{
			string TestMethod();
		}

		public class TestService : ITestService
		{
			public string TestMethod()
			{
				return string.Empty;
			}
		}
	}
}
