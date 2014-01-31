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
	/// Test class for deterministic cleanup.
	/// </summary>
	[TestClass]
	public class CleanupTests
	{
		#region Sample component classes and interfaces

		public interface ISampleComponent
		{
			Action Handler { get; set; }
		}

		public class DisposableComponent : ISampleComponent, IDisposable
		{
			public Action Handler { get; set; }

			public DisposableComponent()
			{
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Assert.IsNotNull(Handler);
				Handler();
			}
		}

		public class ReleasableComponent : ISampleComponent
		{
			public Action Handler { get; set; }

			public ReleasableComponent()
			{
			}

			public void Release()
			{
				Assert.IsNotNull(Handler);
				Handler();
			}
		}

		#endregion

		public TestContext TestContext { get; set; }

		//=============================================
		// IDisposable support
		//=============================================

		[TestMethod]
		public void SingleCallComponentRegisteredWithFactoryMethod_IsDisposed()
		{
			var disposed = false;
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent>(() => new DisposableComponent { Handler = () => disposed = true }, ActivationType.SingleCall);
			Assert.IsFalse(disposed);

			var instance = cat.GetComponent<ISampleComponent>();
			AssertEx.IsInstanceOf<DisposableComponent>(instance);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.IsTrue(disposed);
		}

		[TestMethod]
		public void SingleCallComponentRegisteredWithComponentType_IsDisposed()
		{
			var disposed = false;
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, DisposableComponent>(ActivationType.SingleCall);
			Assert.IsFalse(disposed);

			var instance = cat.GetComponent<ISampleComponent>();
			AssertEx.IsInstanceOf<DisposableComponent>(instance);
			instance.Handler = () => disposed = true;

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.IsTrue(disposed);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithFactoryMethod_IsDisposed()
		{
			var disposed = false;
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent>(() => new DisposableComponent { Handler = () => disposed = true }, ActivationType.Singleton);
			Assert.IsFalse(disposed);

			var instance = cat.GetComponent<ISampleComponent>();
			AssertEx.IsInstanceOf<DisposableComponent>(instance);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.IsTrue(disposed);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithComponentType_IsDisposed()
		{
			var disposed = false;
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, DisposableComponent>(ActivationType.Singleton);
			Assert.IsFalse(disposed);

			var instance = cat.GetComponent<ISampleComponent>();
			AssertEx.IsInstanceOf<DisposableComponent>(instance);
			instance.Handler = () => disposed = true;

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.IsTrue(disposed);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithComponentInstance_IsNotDisposed()
		{
			// this component instance is externally-owned
			var disposed = false;
			using (var immortalServer = new DisposableComponent { Handler = () => disposed = true })
			using (var cat = new ComponentCatalog())
			{
				cat.RegisterComponent<ISampleComponent, DisposableComponent>(immortalServer);
				Assert.IsFalse(disposed);

				var instance = cat.GetComponent<ISampleComponent>();
				AssertEx.IsInstanceOf<DisposableComponent>(instance);

				var reg = cat.GetRegistration(typeof(ISampleComponent));
				cat.CleanUpComponentInstance(reg, instance);
				Assert.IsFalse(disposed);

				immortalServer.Dispose();
				Assert.IsTrue(disposed);
			}
		}

		//=============================================
		// Cleanup delegate support
		//=============================================

		[TestMethod]
		public void SingleCallComponentRegisteredWithFactoryMethod_IsCleanedUp()
		{
			var disposed = false;
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent>(
				() => new ReleasableComponent { Handler = () => disposed = true }, 
				ActivationType.SingleCall, v => ((ReleasableComponent)v).Release());
			Assert.IsFalse(disposed);

			var instance = cat.GetComponent<ISampleComponent>();
			AssertEx.IsInstanceOf<ReleasableComponent>(instance);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.IsTrue(disposed);
		}

		[TestMethod]
		public void SingleCallComponentRegisteredWithComponentType_IsCleanedUp()
		{
			var disposed = false;
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, ReleasableComponent>(
				ActivationType.SingleCall, v => ((ReleasableComponent)v).Release());
			Assert.IsFalse(disposed);

			var instance = cat.GetComponent<ISampleComponent>();
			AssertEx.IsInstanceOf<ReleasableComponent>(instance);
			instance.Handler = () => disposed = true;

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.IsTrue(disposed);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithFactoryMethod_IsCleanedUp()
		{
			var disposed = false;
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent>(
				() => new ReleasableComponent { Handler = () => disposed = true },
				ActivationType.Singleton, v => ((ReleasableComponent)v).Release());
			Assert.IsFalse(disposed);

			var instance = cat.GetComponent<ISampleComponent>();
			AssertEx.IsInstanceOf<ReleasableComponent>(instance);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.IsTrue(disposed);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithComponentType_IsCleanedUp()
		{
			var disposed = false;
			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, ReleasableComponent>(
				ActivationType.Singleton, v => ((ReleasableComponent)v).Release());
			Assert.IsFalse(disposed);

			var instance = cat.GetComponent<ISampleComponent>();
			AssertEx.IsInstanceOf<ReleasableComponent>(instance);
			instance.Handler = () => disposed = true;

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.IsTrue(disposed);
		}

		[TestMethod]
		public void SingletonComponentRegisteredWithComponentInstance_IsCleanedUp()
		{
			// this component instance is created outside, but the ownership
			// is transferred to the ComponentCatalog via cleanup delegate
			var disposed = false;
			var mortalServer = new ReleasableComponent { Handler = () => disposed = true };

			var cat = new ComponentCatalog();
			cat.RegisterComponent<ISampleComponent, ReleasableComponent>(mortalServer, v => ((ReleasableComponent)v).Release());
			Assert.IsFalse(disposed);

			var instance = cat.GetComponent<ISampleComponent>();
			AssertEx.IsInstanceOf<ReleasableComponent>(instance);

			var reg = cat.GetRegistration(typeof(ISampleComponent));
			cat.CleanUpComponentInstance(reg, instance);
			Assert.IsTrue(disposed);
		}

		//=============================================
		// ComponentCatalog ownership
		//=============================================

		[TestMethod]
		public void OwnedComponentCatalog_IsDisposed()
		{
			var disposed = false;
			var server = new ReleasableComponent { Handler = () => disposed = true };
			Assert.IsFalse(disposed);

			var serverSetup = new IpcBinaryServerProtocolSetup("CleanupTest1");
			using (var host = new ZyanComponentHost("SampleServer1", serverSetup))
			{
				host.RegisterComponent<ISampleComponent, ReleasableComponent>(
					server, s => ((ReleasableComponent)s).Release());
			}

			Assert.IsTrue(disposed);
		}

		[TestMethod]
		public void ExternalComponentCatalog_IsNotDisposed()
		{
			var disposed = false;
			var server = new ReleasableComponent { Handler = () => disposed = true };
			Assert.IsFalse(disposed);

			var serverSetup = new IpcBinaryServerProtocolSetup("CleanupTest2");
			using (var catalog = new ComponentCatalog())
			using (var host = new ZyanComponentHost("SampleServer2", serverSetup, new InProcSessionManager(), catalog))
			{
				host.RegisterComponent<ISampleComponent, ReleasableComponent>(
					server, s => ((ReleasableComponent)s).Release());

				Assert.IsFalse(disposed);
				server.Release();
				Assert.IsTrue(disposed);
			}
		}
	}
}
