using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Runtime.Remoting;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Composition;
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
	using ClassCleanupNonStatic = DummyAttribute;
	using ClassInitializeNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for client-side MEF integration.
	///</summary>
	[TestClass]
	public class MefClientTests
	{
		#region Interfaces and components

		public interface IMefClientSample
		{
			string GetVersion();

			T GetValue<T>(string value);
		}

		public class MefClientSample : IMefClientSample
		{
			public const string Version = "1.2.3.4";

			public string GetVersion()
			{
				return Version;
			}

			public T GetValue<T>(string value)
			{
				return (T)(object)Convert.ChangeType(value, typeof(T));
			}
		}

		public class MefClientSample2 : IMefClientSample
		{
			public const string Version = "5.6.7.8";

			public string GetVersion()
			{
				return Version;
			}

			public T GetValue<T>(string value)
			{
				return (T)(object)Convert.ChangeType(value.Substring(0, 1), typeof(T));
			}
		}

		public class MefClientConsumer
		{
			[Import]
			public IMefClientSample Sample { get; set; }

			[Import("AnotherComponent")]
			public IMefClientSample Sample2 { get; set; }
		}

		#endregion

		public TestContext TestContext { get; set; }

		static ZyanComponentHost ZyanHost { get; set; }

		static ZyanConnection ZyanConnection { get; set; }

		static ComposablePartCatalog MefCatalog { get; set; }

		static CompositionContainer MefContainer { get; set; }

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			StartServer(null);
		}

		[ClassInitialize]
		public static void StartServer(TestContext ctx)
		{
			var serverSetup = new IpcBinaryServerProtocolSetup("MefClientTest");
			ZyanHost = new ZyanComponentHost("MefClientServer", serverSetup);
			ZyanHost.RegisterComponent<IMefClientSample, MefClientSample>();
			ZyanHost.RegisterComponent<IMefClientSample, MefClientSample2>("AnotherComponent");

			var clientSetup = new IpcBinaryClientProtocolSetup();
			ZyanConnection = new ZyanConnection("ipc://MefClientTest/MefClientServer", clientSetup);

			MefCatalog = new ZyanCatalog(ZyanConnection);
			MefContainer = new CompositionContainer(MefCatalog);
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		[TestMethod]
		public void MefClientSample_IsDiscovered()
		{
			var consumer = new MefClientConsumer();
			MefContainer.ComposeParts(consumer);

			Assert.IsNotNull(consumer.Sample);
			Assert.IsTrue(RemotingServices.IsTransparentProxy(consumer.Sample));
			AssertEx.IsInstanceOf<IMefClientSample>(consumer.Sample);

			var version = consumer.Sample.GetVersion();
			Assert.AreEqual(MefClientSample.Version, version);

			var value = consumer.Sample.GetValue<int>("98765");
			Assert.AreEqual(98765, value);
		}

		[TestMethod]
		public void MefClientSample2_IsDiscovered()
		{
			var consumer = new MefClientConsumer();
			MefContainer.ComposeParts(consumer);

			Assert.IsNotNull(consumer.Sample2);
			Assert.IsTrue(RemotingServices.IsTransparentProxy(consumer.Sample2));
			AssertEx.IsInstanceOf<IMefClientSample>(consumer.Sample2);

			var version = consumer.Sample2.GetVersion();
			Assert.AreEqual(MefClientSample2.Version, version);

			var value = consumer.Sample2.GetValue<int>("98765");
			Assert.AreEqual(9, value);
		}
	}
}
