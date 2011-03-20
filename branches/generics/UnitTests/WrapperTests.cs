using System;
using NUnit.Framework;
using UnitTests.Model;
using Zyan.Communication;
using Zyan.Communication.Protocols.Ipc;

namespace UnitTests
{
	[TestFixture]
	public class WrapperTests
	{
		[Test]
		public void TestGenericClass()
		{
			var test = new GenericClass();
			var prioritySet = false;

			// GetDefault
			Assert.AreEqual(default(int), test.GetDefault<int>());
			Assert.AreEqual(default(string), test.GetDefault<string>());
			Assert.AreEqual(default(Guid), test.GetDefault<Guid>());

			// Equals
			Assert.IsTrue(test.Equals(123, 123));
			Assert.IsFalse(test.Equals("Some", null));

			// GetVersion
			Assert.AreEqual("DoSomething wasn't called yet", test.GetVersion());

			// DoSomething
			test.DoSomething(123, 'x', "y");
			Assert.AreEqual("DoSomething: A = 123, B = x, C = y", test.GetVersion());

			// Compute
			var dt = test.Compute<Guid, DateTime>(Guid.Empty, 123, "123");
			Assert.AreEqual(default(DateTime), dt);

			// CreateGuid
			var guid = test.CreateGuid(dt, 12345);
			Assert.AreEqual("00003039-0001-0001-0000-000000000000", guid.ToString());

			// LastDate
			Assert.AreEqual(dt, test.LastDate);
			test.LastDate = dt = DateTime.Now;
			Assert.AreEqual(dt, test.LastDate);

			// Name
			Assert.AreEqual("GenericClass, priority = 123", test.Name);

			// add OnPrioritySet
			EventHandler<EventArgs> handler = (s, a) => prioritySet = true;
			test.OnPrioritySet += handler;
			Assert.IsFalse(prioritySet);

			// Priority
			test.Priority = 321;
			Assert.IsTrue(prioritySet);
			Assert.AreEqual("GenericClass, priority = 321", test.Name);

			// remove OnPrioritySet
			prioritySet = false;
			test.OnPrioritySet -= handler;
			test.Priority = 111;
			Assert.IsFalse(prioritySet);
		}

		[Test]
		public void TestNonGenericWrapper()
		{
			var testWrapper = new NonGenericWrapper(new GenericClass());
			var prioritySet = false;

			// GetDefault<T>
			Assert.AreEqual(default(int), testWrapper.GetDefault_T(typeof(int)));
			Assert.AreEqual(default(string), testWrapper.GetDefault_T(typeof(string)));
			Assert.AreEqual(default(Guid), testWrapper.GetDefault_T(typeof(Guid)));

			// Equals<T>
			Assert.IsTrue(testWrapper.Equals_T(typeof(int), 123, 123));
			Assert.IsFalse(testWrapper.Equals_T(typeof(string), "Some", null));

			// GetVersion
			Assert.AreEqual("DoSomething wasn't called yet", testWrapper.GetVersion());

			// DoSomething
			testWrapper.DoSomething_A_B_C(typeof(int), typeof(char), typeof(string), 123, 'x', "y");
			Assert.AreEqual("DoSomething: A = 123, B = x, C = y", testWrapper.GetVersion());

			// Compute
			var dt = (DateTime)testWrapper.Compute_A_B(typeof(Guid), typeof(DateTime), Guid.Empty, 123, "123");
			Assert.AreEqual(default(DateTime), dt);

			// CreateGuid
			var guid = testWrapper.CreateGuid(dt, 12345);
			Assert.AreEqual("00003039-0001-0001-0000-000000000000", guid.ToString());

			// LastDate
			Assert.AreEqual(dt, testWrapper.LastDate);
			testWrapper.LastDate = dt = DateTime.Now;
			Assert.AreEqual(dt, testWrapper.LastDate);

			// Name
			Assert.AreEqual("GenericClass, priority = 123", testWrapper.Name);

			// add OnPrioritySet
			EventHandler<EventArgs> handler = (s, a) => prioritySet = true;
			testWrapper.OnPrioritySet += handler;
			Assert.IsFalse(prioritySet);

			// Priority
			testWrapper.Priority = 321;
			Assert.IsTrue(prioritySet);
			Assert.AreEqual("GenericClass, priority = 321", testWrapper.Name);

			// remove OnPrioritySet
			prioritySet = false;
			testWrapper.OnPrioritySet -= handler;
			testWrapper.Priority = 111;
			Assert.IsFalse(prioritySet);
		}

		[Test]
		public void TestGenericWrapper()
		{
			var test = new GenericWrapper(new NonGenericWrapper(new GenericClass()));
			var prioritySet = false;

			// GetDefault
			Assert.AreEqual(default(int), test.GetDefault<int>());
			Assert.AreEqual(default(string), test.GetDefault<string>());
			Assert.AreEqual(default(Guid), test.GetDefault<Guid>());

			// Equals
			Assert.IsTrue(test.Equals(123, 123));
			Assert.IsFalse(test.Equals("Some", null));

			// GetVersion
			Assert.AreEqual("DoSomething wasn't called yet", test.GetVersion());

			// DoSomething
			test.DoSomething(123, 'x', "y");
			Assert.AreEqual("DoSomething: A = 123, B = x, C = y", test.GetVersion());

			// Compute
			var dt = test.Compute<Guid, DateTime>(Guid.Empty, 123, "123");
			Assert.AreEqual(default(DateTime), dt);

			// CreateGuid
			var guid = test.CreateGuid(dt, 12345);
			Assert.AreEqual("00003039-0001-0001-0000-000000000000", guid.ToString());

			// LastDate
			Assert.AreEqual(dt, test.LastDate);
			test.LastDate = dt = DateTime.Now;
			Assert.AreEqual(dt, test.LastDate);

			// Name
			Assert.AreEqual("GenericClass, priority = 123", test.Name);

			// add OnPrioritySet
			EventHandler<EventArgs> handler = (s, a) => prioritySet = true;
			test.OnPrioritySet += handler;
			Assert.IsFalse(prioritySet);

			// Priority
			test.Priority = 321;
			Assert.IsTrue(prioritySet);
			Assert.AreEqual("GenericClass, priority = 321", test.Name);

			// remove OnPrioritySet
			prioritySet = false;
			test.OnPrioritySet -= handler;
			test.Priority = 111;
			Assert.IsFalse(prioritySet);
		}

		[Test]
		public void TestGenericWrapperOverZyan()
		{
			var serverProto = new IpcBinaryServerProtocolSetup("Test");
			var clientProto = new IpcBinaryClientProtocolSetup();

			var host = new ZyanComponentHost("TestServer", serverProto);
			host.RegisterComponent<INonGenericInterface>(() => new NonGenericWrapper(new GenericClass()), ActivationType.Singleton);

			var conn = new ZyanConnection("ipc://Test/TestServer");
			var proxy = conn.CreateProxy<INonGenericInterface>();

			// same as usual
			var test = new GenericWrapper(proxy);
			var prioritySet = false;

			// GetDefault
			Assert.AreEqual(default(int), test.GetDefault<int>());
			Assert.AreEqual(default(string), test.GetDefault<string>());
			Assert.AreEqual(default(Guid), test.GetDefault<Guid>());

			// Equals
			Assert.IsTrue(test.Equals(123, 123));
			Assert.IsFalse(test.Equals("Some", null));

			// GetVersion
			Assert.AreEqual("DoSomething wasn't called yet", test.GetVersion());

			// DoSomething
			test.DoSomething(123, 'x', "y");
			Assert.AreEqual("DoSomething: A = 123, B = x, C = y", test.GetVersion());

			// Compute
			var dt = test.Compute<Guid, DateTime>(Guid.Empty, 123, "123");
			Assert.AreEqual(default(DateTime), dt);

			// CreateGuid
			var guid = test.CreateGuid(dt, 12345);
			Assert.AreEqual("00003039-0001-0001-0000-000000000000", guid.ToString());

			// LastDate
			Assert.AreEqual(dt, test.LastDate);
			test.LastDate = dt = DateTime.Now;
			Assert.AreEqual(dt, test.LastDate);

			// Name
			Assert.AreEqual("GenericClass, priority = 123", test.Name);

			// add OnPrioritySet
			EventHandler<EventArgs> handler = (s, a) => prioritySet = true;
			test.OnPrioritySet += handler;
			Assert.IsFalse(prioritySet);

			// Priority
			test.Priority = 321;
			Assert.IsTrue(prioritySet);
			Assert.AreEqual("GenericClass, priority = 321", test.Name);

			// remove OnPrioritySet
			prioritySet = false;
			test.OnPrioritySet -= handler;
			test.Priority = 111;
			Assert.IsFalse(prioritySet);
		}
	}
}
