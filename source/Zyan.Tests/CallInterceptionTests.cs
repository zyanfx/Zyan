using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

			void EmitGenericHandlerEvent();

			event EventHandler<EventArgs> GenericHandlerEvent;
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

			public IEnumerable<string> EnumerateProcedure()
			{
				return Enumerable.Repeat("test", 2);
			}

			private void OnProcedureCalled()
			{
				ProcedureCalled?.Invoke(null, EventArgs.Empty);
			}

			public event EventHandler<EventArgs> GenericHandlerEvent;

			public void EmitGenericHandlerEvent()
			{
				RaiseGenericHandlerEvent();
			}

			private void RaiseGenericHandlerEvent()
			{
				var h = GenericHandlerEvent;
				h?.Invoke(null, new EventArgs());
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
			proxy.ProcedureCalled += (sender, args) =>
			{
				procedureCalled = true;
			};
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
		public void GenericEventHandlerInterceptionTest()
		{
			var intercepted = false;
			var procedureCalled = false;

			var interceptor = new CallInterceptor(typeof(IInterceptableComponent),
				MemberTypes.Method, "add_GenericHandlerEvent", new[] { typeof(EventHandler<EventArgs>) }, data =>
				{
					intercepted = true;
				});

			ZyanConnection.CallInterceptors.Add(interceptor);

			var proxy = ZyanConnection.CreateProxy<IInterceptableComponent>();
			proxy.GenericHandlerEvent += (sender, args) =>
			{
				procedureCalled = true;
			};

			Assert.IsFalse(procedureCalled);
			Assert.IsTrue(intercepted);

			proxy.EmitGenericHandlerEvent();
			Assert.IsTrue(procedureCalled);
		}

		[TestMethod]
		public void CallInterceptionBug()
		{
			const string instanceName = "FirstTestServer";
			const int port = 18888;

			var namedService1 = nameof(NamedService1);
			var namedService2 = nameof(NamedService2);

			using (var host = new ZyanComponentHost(instanceName, port))
			{
				// register 3 service instances by the same interface: 2 with unique name, one is unnamed
				// named service1 has reference to service2 as child
				host.RegisterComponent<ITestService, NamedService1>(namedService1, ActivationType.Singleton);
				host.RegisterComponent<ITestService, NamedService2>(namedService2, ActivationType.Singleton);
				host.RegisterComponent<ITestService, UnnamedService>(ActivationType.Singleton);

				using (var connection = new ZyanConnection($"tcp://127.0.0.1:{port}/{instanceName}")
				{
					CallInterceptionEnabled = true
				})
				{
					// add a call interceptor for the TestService.TestMethod
					connection.CallInterceptors.Add(CallInterceptor.For<ITestService>()
						.WithUniqueNameFilter(@"NamedService\d+")
						.Action(service => service.TestMethod(), data =>
						{
							data.ReturnValue = $"intercepted_{data.MakeRemoteCall()}";
							data.Intercepted = true;
						}));

					// for unnamed service
					connection.CallInterceptors
						.For<ITestService>()
						.Add(c => c.TestMethod(), data =>
						{
							data.ReturnValue = $"intercepted_unnamed_{data.MakeRemoteCall()}";
							data.Intercepted = true;
						});

					connection.CallInterceptors
						.For<ITestService>()
						.WithUniqueNameFilter(".*") // for all services does not matter named or not
						.Add(c => c.EnumerateProcedure(), action =>
						{
							var result = (IEnumerable<string>) action.MakeRemoteCall();
							var intercepted = result.Select(r => $"intercepted_{r}");
							action.ReturnValue = intercepted.ToList();
							action.Intercepted = true;
						});

					// intercept and return children like a collection of proxies on client side
					// suppress remote call
					connection.CallInterceptors.For<ITestService>()
						.WithUniqueNameFilter(".*")
						.Add(c => c.GetChildren(), action =>
						{
							action.Intercepted = true;
							var childNames = connection.CreateProxy<ITestService>(action.InvokerUniqueName)?
								.GetChildrenName()
								.ToList();

							var children = new List<ITestService>();
							foreach (var childName in childNames)
							{
								children.Add(connection.CreateProxy<ITestService>(childName));
							}
							// prevent remote call
							//action.MakeRemoteCall();
							action.ReturnValue = children;
						});

					var namedClient1 = connection.CreateProxy<ITestService>(namedService1);
					var namedClient2 = connection.CreateProxy<ITestService>(namedService2);
					var unnamedClient = connection.CreateProxy<ITestService>();

					// assert names
					Assert.AreEqual(namedClient1.Name, namedService1);
					Assert.AreEqual(namedClient2.Name, namedService2);
					Assert.AreEqual(unnamedClient.Name, nameof(UnnamedService));

					// assert method class interception result
					var named1_TestMethod_Result = namedClient1.TestMethod();
					var named2_TestMethod_Result = namedClient2.TestMethod();
					var unnamed_TestMethod_Result = unnamedClient.TestMethod();

					Assert.AreEqual($"intercepted_{namedService1}", named1_TestMethod_Result);
					Assert.AreEqual($"intercepted_{namedService2}", named2_TestMethod_Result);
					Assert.AreEqual($"intercepted_unnamed_{nameof(UnnamedService)}", unnamed_TestMethod_Result);

					// enumerate procedure: all class are handled by single interceptor
					var named1_enumerate_result = namedClient1.EnumerateProcedure();
					var named2_enumerate_result = namedClient2.EnumerateProcedure();
					var unnnamed_enumerate_result = unnamedClient.EnumerateProcedure();

					Assert.AreEqual(1, named1_enumerate_result.Count());
					Assert.IsTrue(named1_enumerate_result.All(r => string.Equals(r, $"intercepted_{namedService1}")));

					Assert.AreEqual(2, named2_enumerate_result.Count());
					Assert.IsTrue(named2_enumerate_result.All(r => string.Equals(r, $"intercepted_{namedService2}")));

					Assert.IsFalse(unnnamed_enumerate_result.Any());
				}
			}
		}

		public interface ITestService
		{
			string Name { get; }

			string TestMethod();

			IEnumerable<ITestService> GetChildren();

			IEnumerable<string> GetChildrenName();

			IEnumerable<string> EnumerateProcedure();
		}

		public class NamedService1 : ITestService
		{
			[NonSerialized]
			private NamedService2 child = new NamedService2();

			public string Name => nameof(NamedService1);

			public string TestMethod()
			{
				return nameof(NamedService1);
			}

			public IEnumerable<string> GetChildrenName()
			{
				return GetChildren().Select(c => c.Name).ToList();
			}

			public IEnumerable<string> EnumerateProcedure()
			{
				return Enumerable.Repeat(nameof(NamedService1), 1).ToArray();
			}

			public IEnumerable<ITestService> GetChildren()
			{
				return Enumerable.Repeat(child, 1).ToArray();
			}
		}

		public class NamedService2 : ITestService
		{
			public string Name => nameof(NamedService2);

			public string TestMethod()
			{
				return nameof(NamedService2);
			}

			public IEnumerable<ITestService> GetChildren()
			{
				return Enumerable.Empty<ITestService>().ToArray();
			}

			public IEnumerable<string> GetChildrenName()
			{
				return Enumerable.Empty<string>().ToList();
			}

			public IEnumerable<string> EnumerateProcedure()
			{
				return Enumerable.Repeat(nameof(NamedService2), 2).ToArray();
			}
		}

		public class UnnamedService : ITestService
		{
			public string Name => nameof(UnnamedService);

			public string TestMethod()
			{
				return nameof(UnnamedService);
			}

			public IEnumerable<ITestService> GetChildren()
			{
				return Enumerable.Empty<ITestService>().ToList();
			}

			public IEnumerable<string> GetChildrenName()
			{
				return Enumerable.Empty<string>().ToArray();
			}

			public IEnumerable<string> EnumerateProcedure()
			{
				return Enumerable.Empty<string>().ToList();
			}
		}
	}
}
