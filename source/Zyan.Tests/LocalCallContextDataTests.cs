using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Zyan.Communication;
using Zyan.Communication.Toolbox;
using Zyan.Communication.ChannelSinks.Compression;
using Zyan.Communication.Protocols.Wrapper;

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
	/// Test class for the local call context data store.
	///</summary>
	[TestClass]
	public class LocalCallContextDataTests : MarshalByRefObject
	{
		[TestMethod]
		public void OrdinalCallContextDataDoesntFlowWithExecutionContextInDeledateBeginInvoke()
		{
			const string SlotName = "Hello1";
			CallContext.SetData(SlotName, "World");

			var dataAccessible = false;
			var action = new Action(() =>
			{
				var data = CallContext.GetData(SlotName);
				dataAccessible = data != null;
			});

			// check if ordinal call context data is not accessible
			var result = action.BeginInvoke(null, null);
			result.AsyncWaitHandle.WaitOne();
			Assert.IsFalse(dataAccessible);
		}

		[TestMethod]
		public void OrdinalCallContextDataDoesntFlowWithExecutionContextInQueueUserWorkItem()
		{
			const string SlotName = "Hello2";
			CallContext.SetData(SlotName, "World");

			var dataAccessible = false;
			var resetEvent = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(x =>
			{
				var data = CallContext.GetData(SlotName);
				dataAccessible = data != null;
				resetEvent.Set();
			});

			// check if ordinal call context data is not accessible
			resetEvent.WaitOne();
			Assert.IsFalse(dataAccessible);
		}

		[TestMethod]
		public void OrdinalCallContextDataDoesntFlowWithExecutionContextInThreadStart()
		{
			const string SlotName = "Hello3";
			CallContext.SetData(SlotName, "World");

			var dataAccessible = false;
			var resetEvent = new ManualResetEvent(false);
			var thread = new Thread(x =>
			{
				var data = CallContext.GetData(SlotName);
				dataAccessible = data != null;
				resetEvent.Set();
			});

			// check if ordinal call context data is not accessible
			thread.Start();
			resetEvent.WaitOne();
			Assert.IsFalse(dataAccessible);
		}

		[TestMethod]
		public void OrdinalCallContextDataDoesntFlowWithExecutionContextInTaskStartNew()
		{
			const string SlotName = "Hello4";
			CallContext.SetData(SlotName, "World");

			var dataAccessible = false;
			var task = Task.Factory.StartNew(() =>
			{
				var data = CallContext.GetData(SlotName);
				dataAccessible = data != null;
			});

			// check if ordinal call context data is not accessible
			task.Wait();
			Assert.IsFalse(dataAccessible);
		}

		[TestMethod]
		public void LocalCallContextDataFlowsWithExecutionContextInDeledateBeginInvoke()
		{
			const string SlotName = "Hello5";
			LocalCallContextData.SetData(SlotName, "World");

			var dataAccessible = false;
			var action = new Action(() =>
			{
				var data = LocalCallContextData.GetData(SlotName);
				dataAccessible = data != null;
			});

			// check if ordinal call context data is accessible
			var result = action.BeginInvoke(null, null);
			result.AsyncWaitHandle.WaitOne();
			Assert.IsTrue(dataAccessible);
		}

		[TestMethod]
		public void LocalCallContextDataFlowsWithExecutionContextInQueueUserWorkItem()
		{
			const string SlotName = "Hello6";
			LocalCallContextData.SetData(SlotName, "World");

			var dataAccessible = false;
			var resetEvent = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(x =>
			{
				var data = LocalCallContextData.GetData(SlotName);
				dataAccessible = data != null;
				resetEvent.Set();
			});

			// check if ordinal call context data is accessible
			resetEvent.WaitOne();
			Assert.IsTrue(dataAccessible);
		}

		[TestMethod]
		public void LocalCallContextDataFlowsWithExecutionContextInThreadStart()
		{
			const string SlotName = "Hello7";
			LocalCallContextData.SetData(SlotName, "World");

			var dataAccessible = false;
			var resetEvent = new ManualResetEvent(false);
			var thread = new Thread(x =>
			{
				var data = LocalCallContextData.GetData(SlotName);
				dataAccessible = data != null;
				resetEvent.Set();
			});

			// check if ordinal call context data is accessible
			thread.Start();
			resetEvent.WaitOne();
			Assert.IsTrue(dataAccessible);
		}

		[TestMethod]
		public void LocalCallContextDataFlowsWithExecutionContextInTaskStartNew()
		{
			const string SlotName = "Hello8";
			LocalCallContextData.SetData(SlotName, "World");

			var dataAccessible = false;
			var resetEvent = new ManualResetEvent(false);
			var task = Task.Factory.StartNew(() =>
			{
				var data = LocalCallContextData.GetData(SlotName);
				dataAccessible = data != null;
				resetEvent.Set();
			});

			// check if ordinal call context data is accessible
			task.Wait();
			Assert.IsTrue(dataAccessible);
		}

		[TestMethod]
		public void LocalCallContextDataDoestnLeaveApplicationDomain()
		{
			var domainSetup = new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.BaseDirectory };
			var otherDomain = AppDomain.CreateDomain("Sandbox", null, domainSetup);
			otherDomain.Load(typeof(ZyanConnection).Assembly.GetName());
			otherDomain.Load(typeof(LocalCallContextDataTests).Assembly.GetName());

			try
			{
				const string SlotName = "Hello9";
				LocalCallContextData.SetData(SlotName, "World");
				var dataAccessible = LocalCallContextData.GetData(SlotName) != null;
				Assert.IsTrue(dataAccessible);

				otherDomain.DoCallBack(() =>
				{
					var data = LocalCallContextData.GetData(SlotName);
					var accessible = data != null;
					AppDomain.CurrentDomain.SetData(SlotName, accessible);
				});

				// check if ordinal call context data is not accessible
				dataAccessible = (bool)otherDomain.GetData(SlotName);
				Assert.IsFalse(dataAccessible);

				// check if the callback doesn't wipe out the data
				dataAccessible = LocalCallContextData.GetData(SlotName) != null;
				Assert.IsTrue(dataAccessible);
			}
			finally
			{
				AppDomain.Unload(otherDomain);
			}
		}
	}
}
