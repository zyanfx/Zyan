using System;
using System.Collections.Generic;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Protocols.Ipc;
using Zyan.Communication.SessionMgmt;

namespace Zyan.Tests
{
	#region Unit testing platform abstraction layer
#if NUNIT
	using NUnit.Framework;
	using TestClass = NUnit.Framework.TestFixtureAttribute;
	using TestMethod = NUnit.Framework.TestAttribute;
	using ClassInitializeParameterless = NUnit.Framework.TestFixtureSetUpAttribute;
	using ClassInitialize = DummyAttribute;
	using ClassCleanup = NUnit.Framework.TestFixtureTearDownAttribute;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeParameterless = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for deterministic cleanup
	/// </summary>
	[TestClass]
	public class CleanupTests
	{
		#region Sample component classes and interfaces

		public interface ISampleComponent
		{
		}

		public class DisposableComponent : ISampleComponent, IDisposable
		{
			static int instanceCount = 0;

			public DisposableComponent()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				Interlocked.Decrement(ref instanceCount);
			}

			public static int InstanceCount { get { return instanceCount; } }
		}

		public class ReleasableComponent : ISampleComponent
		{
			static int instanceCount = 0;

			public ReleasableComponent()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Release()
			{
				Interlocked.Decrement(ref instanceCount);
			}

			public static int InstanceCount { get { return instanceCount; } }
		}

		public class NonDisposableComponent : ISampleComponent
		{
			static List<NonDisposableComponent> instanceList = new List<NonDisposableComponent>();

			public NonDisposableComponent()
			{
				// prevent GC from collecting this instance
				instanceList.Add(this);
			}

			public static int InstanceCount { get { return instanceList.Count; } }
		}

		#endregion

		public TestContext TestContext { get; set; }

		//=============================================
		// IDisposable support
		//=============================================

		[TestMethod]
		public void SingleCallComponentRegisteredWithFactoryMethod_IsDisposed()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent>(() => new DisposableComponent(), ActivationType.SingleCall);
			Assert.AreEqual(0, DisposableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, DisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(0, DisposableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingleCallComponentRegisteredWithComponentType_IsDisposed()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, DisposableComponent>(ActivationType.SingleCall);
			Assert.AreEqual(0, DisposableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, DisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(0, DisposableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithFactoryMethod_IsDisposed()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent>(() => new DisposableComponent(), ActivationType.Singleton);
			Assert.AreEqual(0, DisposableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, DisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(0, DisposableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithComponentType_IsDisposed()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, DisposableComponent>(ActivationType.Singleton);
			Assert.AreEqual(0, DisposableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, DisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(0, DisposableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithComponentInstance_IsNotDisposed()
		{
			// this component instance is externally-owned
			var immortalServer = new DisposableComponent();

			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, DisposableComponent>(immortalServer);
			Assert.AreEqual(1, DisposableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, DisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(1, DisposableComponent.InstanceCount);

			immortalServer.Dispose();
			Assert.AreEqual(0, DisposableComponent.InstanceCount);
		}

		//=============================================
		// Cleanup delegate support
		//=============================================

		[TestMethod]
		public void SingleCallComponentRegisteredWithFactoryMethod_IsCleanedUp()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent>(() => new ReleasableComponent(), ActivationType.SingleCall, v => ((ReleasableComponent)v).Release());
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, ReleasableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingleCallComponentRegisteredWithComponentType_IsCleanedUp()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, ReleasableComponent>(ActivationType.SingleCall, v => ((ReleasableComponent)v).Release());
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, ReleasableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithFactoryMethod_IsCleanedUp()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent>(() => new ReleasableComponent(), ActivationType.Singleton, v => ((ReleasableComponent)v).Release());
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, ReleasableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithComponentType_IsCleanedUp()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, ReleasableComponent>(ActivationType.Singleton, v => ((ReleasableComponent)v).Release());
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, ReleasableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithComponentInstance_IsCleanedUp()
		{
			// this component instance is created outside, but the ownership
			// is transferred to the ComponentCatalog via cleanup delegate
			var mortalServer = new ReleasableComponent();

			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, ReleasableComponent>(mortalServer, v => ((ReleasableComponent)v).Release());
			Assert.AreEqual(1, ReleasableComponent.InstanceCount);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(1, ReleasableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);
		}

		//=============================================
		// Non-disposable components are not handled
		//=============================================

		[TestMethod]
		public void SingleCallNonDisposableComponentRegisteredWithFactoryMethod_IsNotDisposed()
		{
			var cat = new ComponentCatalog();
			var count = NonDisposableComponent.InstanceCount;
			cat.RegisterComponent<ISampleComponent>(() => new NonDisposableComponent(), ActivationType.SingleCall);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(count + 1, NonDisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(count + 1, NonDisposableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingleCallNonDisposableComponentRegisteredWithComponentType_IsNotDisposed()
		{
			var cat = new ComponentCatalog();
			var count = NonDisposableComponent.InstanceCount;
			cat.RegisterComponent<ISampleComponent, NonDisposableComponent>(ActivationType.SingleCall);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(count + 1, NonDisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(count + 1, NonDisposableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingletonNonDisposableComponentRegisteredWithFactoryMethod_IsNotDisposed()
		{
			var cat = new ComponentCatalog();
			var count = NonDisposableComponent.InstanceCount;
			cat.RegisterComponent<ISampleComponent>(() => new NonDisposableComponent(), ActivationType.Singleton);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(count + 1, NonDisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(count + 1, NonDisposableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingletonNonDisposableComponentRegisteredWithComponentType_IsNotDisposed()
		{
			var cat = new ComponentCatalog();
			var count = NonDisposableComponent.InstanceCount;
			cat.RegisterComponent<ISampleComponent, NonDisposableComponent>(ActivationType.Singleton);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(count + 1, NonDisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(count + 1, NonDisposableComponent.InstanceCount);
		}

		[TestMethod]
		public void SingletonNonDisposableComponentRegisteredWithComponentInstance_IsNotDisposed()
		{
			// this component instance is externally-owned
			var immortalServer = new NonDisposableComponent();

			var cat = new ComponentCatalog();
			var count = NonDisposableComponent.InstanceCount;
			cat.RegisterComponent<ISampleComponent, NonDisposableComponent>(immortalServer);

			var instance = cat.GetComponent<ISampleComponent>();
			Assert.AreEqual(count, NonDisposableComponent.InstanceCount);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.AreEqual(count, NonDisposableComponent.InstanceCount);
		}

		//=============================================
		// ComponentCatalog ownership
		//=============================================

		[TestMethod]
		public void OwnedComponentCatalog_IsDisposed()
		{
			var mortalServer = new ReleasableComponent();
			Assert.AreEqual(1, ReleasableComponent.InstanceCount);

			var serverSetup = new IpcBinaryServerProtocolSetup("CleanupTest1");
			using (var host = new ZyanComponentHost("SampleServer1", serverSetup))
			{
				host.RegisterComponent<ISampleComponent, ReleasableComponent>(
					mortalServer, s => ((ReleasableComponent)s).Release());
			}

			Assert.AreEqual(0, ReleasableComponent.InstanceCount);
		}

		[TestMethod]
		public void ExternalComponentCatalog_IsNotDisposed()
		{
			var mortalServer = new ReleasableComponent();
			Assert.AreEqual(1, ReleasableComponent.InstanceCount);

			var catalog = new ComponentCatalog();
			var serverSetup = new IpcBinaryServerProtocolSetup("CleanupTest2");
			using (var host = new ZyanComponentHost("SampleServer2", serverSetup, new InProcSessionManager(), catalog))
			{
				host.RegisterComponent<ISampleComponent, ReleasableComponent>(
					mortalServer, s => ((ReleasableComponent)s).Release());
			}

			Assert.AreEqual(1, ReleasableComponent.InstanceCount);
			mortalServer.Release();
			Assert.AreEqual(0, ReleasableComponent.InstanceCount);
		}
	}
}
