using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Composition;
using System.Collections.Generic;
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
	/// Test class for server-side MEF integration.
	///</summary>
	[TestClass]
	public class MefServerTests
	{
		#region Interfaces and components

		/// <summary>
		/// Sample interface
		/// </summary>
		public interface IMefSample
		{
		}

		/// <summary>
		/// Recommended component registration: ZyanComponent attribute, creation policy not specified (Zyan implies non-shared creation policy).
		/// </summary>
		[ZyanComponent(typeof(IMefSample))]
		public class MefSample1 : IMefSample, IDisposable
		{
			static int instanceCount = 0;

			public static int InstanceCount { get { return instanceCount; } }

			public MefSample1()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Interlocked.Decrement(ref instanceCount);
			}
		}

		/// <summary>
		/// Recommended component registration: named ZyanComponent attribute, creation policy not specified.
		/// </summary>
		[ZyanComponent("UniqueName_MefSample2", typeof(IMefSample))]
		public class MefSample2 : IMefSample, IDisposable
		{
			static int instanceCount = 0;

			public static int InstanceCount { get { return instanceCount; } }

			public MefSample2()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Interlocked.Decrement(ref instanceCount);
			}
		}

		/// <summary>
		/// Zyan-agnostic component registration: standard Export and ExportMetadata attributes.
		/// </summary>
		[Export("UniqueName_MefSample3", typeof(IMefSample))]
		[ExportMetadata("ComponentInterface", typeof(IMefSample))]
		public class MefSample3 : IMefSample, IDisposable
		{
			static int instanceCount = 0;

			public static int InstanceCount { get { return instanceCount; } }

			public MefSample3()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Interlocked.Decrement(ref instanceCount);
			}
		}

		/// <summary>
		/// This component is not published by Zyan, but is still registered in MEF catalog.
		/// It may be considered as internal service which is only available on the server-side.
		/// </summary>
		[Export("UniqueName_MefSample4", typeof(IMefSample))]
		[ExportMetadata("ComponentInterface", typeof(IMefSample))]
		[ExportMetadata("IsPublished", false)]
		public class MefSample4 : IMefSample
		{
		}

		/// <summary>
		/// Sample InheritedExport interface.
		/// </summary>
		[InheritedExport("UniqueName_MefSample5")]
		[ExportMetadata("ComponentInterface", typeof(IMefSample5))]
		public interface IMefSample5
		{
		}

		/// <summary>
		/// Zyan-agnostic component registration: standard InheritedExport and ExportMetadata attributes.
		/// This component is published even though no attributes are attached to it.
		/// </summary>
		public class MefSample5 : IMefSample5, IDisposable
		{
			static int instanceCount = 0;

			public static int InstanceCount { get { return instanceCount; } }

			public MefSample5()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Interlocked.Decrement(ref instanceCount);
			}
		}

		/// <summary>
		/// Private InheritedExport interface.
		/// Classes implementing this interface are exported by MEF, but ignored by Zyan.
		/// </summary>
		[InheritedExport("UniqueName_MefSample6")]
		[ExportMetadata("ComponentInterface", typeof(IMefSample6))]
		[ExportMetadata("IsPublished", false)]
		public interface IMefSample6
		{
		}

		/// <summary>
		/// This class is not published by Zyan.
		/// </summary>
		public class MefSample6 : IMefSample6
		{
		}

		/// <summary>
		/// Sample ZyanInterface.
		/// Component implementing this interface is published automatically.
		/// </summary>
		[ZyanInterface(typeof(IMefSample7))]
		public interface IMefSample7
		{
		}

		/// <summary>
		/// Alternative registration: class implements interface decorated with ZyanInterface attribute.
		/// This component is published even though no attributes are attached to it.
		/// </summary>
		public class MefSample7 : IMefSample7, IDisposable
		{
			static int instanceCount = 0;

			public static int InstanceCount { get { return instanceCount; } }

			public MefSample7()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Interlocked.Decrement(ref instanceCount);
			}
		}

		/// <summary>
		/// Named ZyanInterface.
		/// Component implementing this interface is published automatically.
		/// </summary>
		[ZyanInterface("UniqueName_MefSample8", typeof(IMefSample8))]
		public interface IMefSample8
		{
		}

		/// <summary>
		/// Alternative registration: class implements interface decorated with named ZyanInterface attribute.
		/// This component is published even though no attributes are attached to it.
		/// </summary>
		public class MefSample8 : IMefSample8, IDisposable
		{
			static int instanceCount = 0;

			public static int InstanceCount { get { return instanceCount; } }

			public MefSample8()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Interlocked.Decrement(ref instanceCount);
			}
		}

		/// <summary>
		/// Component uses Shared creation policy and acts as a singleton.
		/// It is disposed together with root CompositionContainer.
		/// </summary>
		[ZyanComponent("UniqueName_MefSample9", typeof(IMefSample))]
		[PartCreationPolicy(CreationPolicy.Shared)]
		public class MefSample9 : IMefSample, IDisposable
		{
			static int instanceCount = 0;

			public static int InstanceCount { get { return instanceCount; } }

			public MefSample9()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Interlocked.Decrement(ref instanceCount);
			}
		}

		/// <summary>
		/// Queryable component is not registered as IQueryRemoteHandler anymore. It's plain IEntitySource.
		/// </summary>
		[ZyanComponent("UniqueName_MefSample10", typeof(IEntitySource))]
		public class MefSample10 : IEntitySource, IDisposable
		{
			public IQueryable<T> Get<T>() where T : class
			{
				throw new NotImplementedException();
			}

			static int instanceCount = 0;

			public static int InstanceCount { get { return instanceCount; } }

			public MefSample10()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Interlocked.Decrement(ref instanceCount);
			}
		}

		/// <summary>
		/// Queryable component is not registered as IQueryRemoteHandler anymore. It's plain IObjectSource.
		/// </summary>
		[ZyanComponent("UniqueName_MefSample11", typeof(IObjectSource))]
		public class MefSample11 : IObjectSource, IDisposable
		{
			public IEnumerable<T> Get<T>() where T : class
			{
				throw new NotImplementedException();
			}

			static int instanceCount = 0;

			public static int InstanceCount { get { return instanceCount; } }

			public MefSample11()
			{
				Interlocked.Increment(ref instanceCount);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Interlocked.Decrement(ref instanceCount);
			}
		}

		/// <summary>
		/// This interface is mixed. It contains simple methods as well as IQueryable/IEnumerable methods.
		/// </summary>
		public interface IMefSample12
		{
			void DoSomething();

			IQueryable<T> GetTable<T>() where T : class, new();

			IEnumerable<T> GetList<T>() where T : class, new();
		}

		/// <summary>
		/// This component is mixed. It contains simple methods as well as IQueryable/IEnumerable methods.
		/// </summary>
		[ZyanComponent("UniqueName_MefSample12", typeof(IMefSample12))]
		public class MefSample12 : IMefSample12
		{
			public void DoSomething()
			{
			}

			public IQueryable<T> GetTable<T>() where T : class, new()
			{
				return new T[10].AsQueryable();
			}

			public IEnumerable<T> GetList<T>() where T : class, new()
			{
				return new T[5];
			}
		}

		/// <summary>
		/// Shared component should be registered as singleton.
		/// </summary>
		[ZyanComponent("UniqueName_MefSample13", typeof(IMefSample)), PartCreationPolicy(CreationPolicy.Shared)]
		public class MefSample13 : IMefSample, IDisposable
		{
			public Guid Guid = Guid.NewGuid();

			public void Dispose()
			{
				Guid = Guid.Empty;
			}
		}

		#endregion

		#region Initialization and cleanup

		public TestContext TestContext { get; set; }

		static ComposablePartCatalog MefCatalog { get; set; }

		static CompositionContainer MefContainer { get; set; }

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			DiscoverComposableParts(null);
		}

		[ClassInitialize]
		public static void DiscoverComposableParts(TestContext ctx)
		{
			MefCatalog = new AssemblyCatalog(typeof(MefServerTests).Assembly);
			MefContainer = new CompositionContainer(MefCatalog);
		}

		#endregion

		[TestMethod]
		public void ZyanComponentFromMefCatalog_IsRegisteredAsSingleCall()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration(typeof(IMefSample));
			Assert.IsNotNull(reg);
			Assert.AreEqual(ActivationType.SingleCall, reg.ActivationType);

			// get component instance
			var obj = cat.GetComponent<IMefSample>();
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample1>(obj);
			Assert.AreEqual(1, MefSample1.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample1.InstanceCount);
		}

		[TestMethod]
		public void ZyanComponentFromMefContainer_IsRegisteredAsSingleCall()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration(typeof(IMefSample));
			Assert.IsNotNull(reg);
			Assert.AreEqual(ActivationType.SingleCall, reg.ActivationType);

			// get component instance
			var obj = cat.GetComponent<IMefSample>();
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample1>(obj);
			Assert.AreEqual(1, MefSample1.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample1.InstanceCount);
		}

		[TestMethod]
		public void NamedZyanComponentFromMefCatalog_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample2");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample2") as IMefSample;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample2>(obj);
			Assert.AreEqual(1, MefSample2.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample2.InstanceCount);
		}

		[TestMethod]
		public void NamedZyanComponentFromMefContainer_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample2");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample2") as IMefSample;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample2>(obj);
			Assert.AreEqual(1, MefSample2.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample2.InstanceCount);
		}

		[TestMethod]
		public void ExportedPartFromMefCatalog_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample3");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample3") as IMefSample;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample3>(obj);
			Assert.AreEqual(1, MefSample3.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample3.InstanceCount);
		}

		[TestMethod]
		public void ExportedPartFromFromMefContainer_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample3");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample3") as IMefSample;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample3>(obj);
			Assert.AreEqual(1, MefSample3.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample3.InstanceCount);
		}

		[TestMethod, ExpectedException(typeof(KeyNotFoundException))]
		public void PrivateExportedPartFromMefCatalog_IsNotRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// component is available in MefCatalog
			var id = new ImportDefinition(def => def.Metadata.ContainsKey("ComponentInterface"), "UniqueName_MefSample4", ImportCardinality.ExactlyOne, false, false);
			var exports = MefCatalog.GetExports(id);
			Assert.IsNotNull(exports);
			Assert.AreEqual(1, exports.Count());

			// component is not registered in Zyan ComponentCatalog
			cat.GetRegistration("UniqueName_MefSample4");
		}

		[TestMethod, ExpectedException(typeof(KeyNotFoundException))]
		public void PrivateExportedPartFromFromMefContainer_IsNotRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// component is available in MefContainer
			var obj = MefContainer.GetExport<IMefSample>("UniqueName_MefSample4").Value;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample4>(obj);

			// component is not registered in Zyan ComponentCatalog
			cat.GetRegistration("UniqueName_MefSample4");
		}

		[TestMethod]
		public void InheritedExportFromMefCatalog_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample5");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample5") as IMefSample5;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample5>(obj);
			Assert.AreEqual(1, MefSample5.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample5.InstanceCount);
		}

		[TestMethod]
		public void InheritedExportFromFromMefContainer_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample5");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample5") as IMefSample5;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample5>(obj);
			Assert.AreEqual(1, MefSample5.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample5.InstanceCount);
		}

		[TestMethod, ExpectedException(typeof(KeyNotFoundException))]
		public void PrivateInheritedExportFromMefCatalog_IsNotRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// component is available in MefCatalog
			var id = new ImportDefinition(def => def.Metadata.ContainsKey("ComponentInterface"), "UniqueName_MefSample6", ImportCardinality.ExactlyOne, false, false);
			var exports = MefCatalog.GetExports(id);
			Assert.IsNotNull(exports);
			Assert.AreEqual(1, exports.Count());

			// component is not registered in Zyan ComponentCatalog
			cat.GetRegistration("UniqueName_MefSample6");
		}

		[TestMethod, ExpectedException(typeof(KeyNotFoundException))]
		public void PrivateInheritedExportFromFromMefContainer_IsNotRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// component is available in MefContainer
			var obj = MefContainer.GetExport<IMefSample6>("UniqueName_MefSample6").Value;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample6>(obj);

			// component is not registered in Zyan ComponentCatalog
			cat.GetRegistration("UniqueName_MefSample6");
		}

		[TestMethod]
		public void ZyanInterfaceFromMefCatalog_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration(typeof(IMefSample7));
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent<IMefSample7>();
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample7>(obj);
			Assert.AreEqual(1, MefSample7.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample7.InstanceCount);
		}

		[TestMethod]
		public void ZyanInterfaceFromFromMefContainer_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration(typeof(IMefSample7));
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent<IMefSample7>();
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample7>(obj);
			Assert.AreEqual(1, MefSample7.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample7.InstanceCount);
		}

		[TestMethod]
		public void NamedZyanInterfaceFromMefCatalog_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample8");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample8") as IMefSample8;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample8>(obj);
			Assert.AreEqual(1, MefSample8.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample8.InstanceCount);
		}

		[TestMethod]
		public void NamedZyanInterfaceFromMefContainer_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample8");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample8") as IMefSample8;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample8>(obj);
			Assert.AreEqual(1, MefSample8.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample8.InstanceCount);
		}

		[TestMethod]
		public void SharedZyanComponentFromMefCatalog_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample9");
			Assert.IsNotNull(reg);
			Assert.AreEqual(ActivationType.Singleton, reg.ActivationType);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample9") as IMefSample;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample9>(obj);
			Assert.AreNotEqual(0, MefSample9.InstanceCount);

			// make sure component instance is not cleaned up
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreNotEqual(0, MefSample9.InstanceCount);

			// make sure component instance is not disposed with catalog
			cat.Dispose();
			Assert.AreNotEqual(0, MefSample9.InstanceCount);
		}

		[TestMethod]
		public void SharedZyanComponentFromMefContainer_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample9");
			Assert.IsNotNull(reg);
			Assert.AreEqual(ActivationType.Singleton, reg.ActivationType);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample9") as IMefSample;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample9>(obj);
			Assert.AreNotEqual(0, MefSample9.InstanceCount);

			// make sure component instance is not cleaned up
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreNotEqual(0, MefSample9.InstanceCount);

			// make sure component instance is not disposed with catalog
			cat.Dispose();
			Assert.AreNotEqual(0, MefSample9.InstanceCount);
		}

		[TestMethod]
		public void IEntitySourceFromMefContainer_IsRegisteredAsIs()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample10");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample10") as IEntitySource;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample10>(obj);
			Assert.AreEqual(1, MefSample10.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample10.InstanceCount);
		}

		[TestMethod]
		public void IObjectSourceFromMefContainer_IsRegisteredAsIs()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample11");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample11") as IObjectSource;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample11>(obj);
			Assert.AreEqual(1, MefSample11.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample11.InstanceCount);
		}

		[TestMethod]
		public void MixedComponent_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample12");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample12") as IMefSample12;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample12>(obj);
		}

		[TestMethod]
		public void ZyanComponentWithSharedPolicyAttribute_IsRegisteredAsSingleton()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration("UniqueName_MefSample13");
			Assert.IsNotNull(reg);
			Assert.AreEqual(ActivationType.Singleton, reg.ActivationType);

			// get component instance
			var obj = cat.GetComponent("UniqueName_MefSample13") as IMefSample;
			Assert.IsNotNull(obj);
			AssertEx.IsInstanceOf<MefSample13>(obj);
			var ms13 = (MefSample13)obj;
			var guid = ms13.Guid;

			// make sure component instance is not cleaned up
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(guid, ms13.Guid);

			// make sure component instance is not disposed with component catalog
			cat.Dispose();
			Assert.AreEqual(guid, ms13.Guid);
		}
	}
}
