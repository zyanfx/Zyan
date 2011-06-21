using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zyan.Communication;
using Zyan.Communication.Composition;
using System.Collections.Generic;

namespace Zyan.Tests
{
	/// <summary>
	/// Test class for MEF integration
	///</summary>
	[TestClass]
	public class MefTests
	{
		#region Interfaces and classes

		/// <summary>
		/// Sample interface
		/// </summary>
		public interface IMefSample
		{
		}

		/// <summary>
		/// Recommended component registration: ZyanComponent attribute, NonShared creation policy
		/// </summary>
		[ZyanComponent(typeof(IMefSample))]
		[PartCreationPolicy(CreationPolicy.NonShared)]
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
		/// Zyan-agnostic component registration: standard Export and ExportMetadata attributes
		/// </summary>
		[Export("SomeUniqueContractName", typeof(IMefSample))]
		[ExportMetadata("ComponentInterface", typeof(IMefSample))]
		[PartCreationPolicy(CreationPolicy.NonShared)]
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
		/// This component is not published by Zyan, but is still registered in MEF catalog.
		/// It may be considered as internal service which is only available on the server-side.
		/// </summary>
		[Export("PrivateServiceUniqueName", typeof(IMefSample))]
		[ExportMetadata("ComponentInterface", typeof(IMefSample))]
		[ExportMetadata("IsPublished", false)]
		[PartCreationPolicy(CreationPolicy.NonShared)]
		public class MefSample3 : IMefSample
		{
		}

		#endregion

		public TestContext TestContext { get; set; }

		static ComposablePartCatalog MefCatalog { get; set; }

		static CompositionContainer MefContainer { get; set; }

		[ClassInitialize]
		public static void DiscoverComposableParts(TestContext ctx)
		{
			MefCatalog = new AssemblyCatalog(typeof(MefTests).Assembly);
			MefContainer = new CompositionContainer(MefCatalog);
		}

		[TestMethod]
		public void ZyanComponentFromMefCatalog_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration(typeof(IMefSample));
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent<IMefSample>();
			Assert.IsNotNull(obj);
			Assert.IsInstanceOfType(obj, typeof(MefSample1));
			Assert.AreEqual(1, MefSample1.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample1.InstanceCount);
		}

		[TestMethod]
		public void ZyanComponentFromMefContainer_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration(typeof(IMefSample));
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent<IMefSample>();
			Assert.IsNotNull(obj);
			Assert.IsInstanceOfType(obj, typeof(MefSample1));
			Assert.AreEqual(1, MefSample1.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample1.InstanceCount);
		}

		[TestMethod]
		public void ExportedPartFromMefCatalog_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// get component registration
			var reg = cat.GetRegistration("SomeUniqueContractName");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("SomeUniqueContractName") as IMefSample;
			Assert.IsNotNull(obj);
			Assert.IsInstanceOfType(obj, typeof(MefSample2));
			Assert.AreEqual(1, MefSample2.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample2.InstanceCount);
		}

		[TestMethod]
		public void ExportedPartFromFromMefContainer_IsRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// get component registration
			var reg = cat.GetRegistration("SomeUniqueContractName");
			Assert.IsNotNull(reg);

			// get component instance
			var obj = cat.GetComponent("SomeUniqueContractName") as IMefSample;
			Assert.IsNotNull(obj);
			Assert.IsInstanceOfType(obj, typeof(MefSample2));
			Assert.AreEqual(1, MefSample2.InstanceCount);

			// clean up component instance
			cat.CleanUpComponentInstance(reg, obj);
			Assert.AreEqual(0, MefSample2.InstanceCount);
		}

		[TestMethod, ExpectedException(typeof(KeyNotFoundException))]
		public void PrivateExportedPartFromMefCatalog_IsNotRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefCatalog);

			// component is available in MefCatalog
			var id = new ImportDefinition(def => def.Metadata.ContainsKey("ComponentInterface"), "PrivateServiceUniqueName", ImportCardinality.ExactlyOne, false, false);
			var exports = MefCatalog.GetExports(id);
			Assert.IsNotNull(exports);
			Assert.AreEqual(1, exports.Count());

			// component is not registered in Zyan ComponentCatalog
			var reg = cat.GetRegistration("PrivateServiceUniqueName");
		}

		[TestMethod, ExpectedException(typeof(KeyNotFoundException))]
		public void PrivateExportedPartFromFromMefContainer_IsNotRegistered()
		{
			var cat = new ComponentCatalog();
			cat.RegisterComponents(MefContainer);

			// component is available in MefContainer
			var obj = MefContainer.GetExport<IMefSample>("PrivateServiceUniqueName");
			Assert.IsNotNull(obj);
			Assert.IsInstanceOfType(obj, typeof(MefSample3));

			// component is not registered in Zyan ComponentCatalog
			var reg = cat.GetRegistration("PrivateServiceUniqueName");
		}
	}
}
