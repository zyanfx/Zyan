using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Protocols.Ipc;

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
	using AssertFailedException = NUnit.Framework.AssertionException;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for ZyanProxy methods.
	///</summary>
	[TestClass]
	public class ZyanProxyTests
	{
		#region Interfaces and components

		/// <summary>
		/// Sample server interface
		/// </summary>
		public interface ISampleServer
		{
			event EventHandler TestEvent;

			void RaiseTestEvent();
		}

		/// <summary>
		/// Sample server implementation
		/// </summary>
		public class SampleServer : ISampleServer
		{
			public override int GetHashCode()
			{
				throw new AssertFailedException("GetHashCode() method is called remotely.");
			}

			public override bool Equals(object obj)
			{
				throw new AssertFailedException("Equals() method is called remotely.");
			}

			public override string ToString()
			{
				throw new AssertFailedException("ToString() method is called remotely.");
			}

			public event EventHandler TestEvent;

			public void RaiseTestEvent()
			{
				if (TestEvent != null)
				{
					TestEvent(null, EventArgs.Empty);
				}
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
		}

		[ClassInitialize]
		public static void StartServer(TestContext ctx)
		{
			var serverSetup = new IpcBinaryServerProtocolSetup("ZyanProxyTest");
			ZyanHost = new ZyanComponentHost("ZyanProxyServer", serverSetup);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer>(ActivationType.Singleton);

			var clientSetup = new IpcBinaryClientProtocolSetup();
			ZyanConnection = new ZyanConnection("ipc://ZyanProxyTest/ZyanProxyServer", clientSetup);
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		[TestMethod]
		public void GetType_ShouldReturnISampleServer()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var type = proxy.GetType();

			// under Mono, type is always System.MarshalByRefObject
			// under .NET, type is ISampleServer

			Assert.IsNotNull(type);
			Assert.AreNotEqual(typeof(SampleServer), type);
		}

		[TestMethod]
		public void GetHashCode_ShouldReturnNonZeroValue()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var hash = proxy.GetHashCode();

			Assert.AreNotEqual(0, hash);
		}

		[TestMethod]
		public void EqualsForSameProxy_ShouldReturnTrue()
		{
			var proxy1 = ZyanConnection.CreateProxy<ISampleServer>();
			var proxy2 = ZyanConnection.CreateProxy<ISampleServer>();

			Assert.IsTrue(proxy1.Equals(proxy2));
		}

		[TestMethod]
		public void EqualsForNonProxy_ShouldReturnFalse()
		{
			var proxy1 = ZyanConnection.CreateProxy<ISampleServer>();
			var proxy2 = new object();

			Assert.IsFalse(proxy1.Equals(proxy2));
		}

		[TestMethod]
		public void ToString_ShouldReturnProxyDescription()
		{
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			var toString = proxy.ToString();

			Assert.AreEqual("ipc://ZyanProxyTest/ZyanProxyServer/Zyan.Tests.ZyanProxyTests+ISampleServer", toString);
		}
	}
}
