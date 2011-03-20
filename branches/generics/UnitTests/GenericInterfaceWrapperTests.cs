using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using GenericWrappers;
using UnitTests.Model;

namespace UnitTests
{
	[TestFixture]
	public class GenericInterfaceWrapperTests
	{
		[Test]
		public void WrappedInterfaceIsNotSame()
		{
			var source = typeof(IGenericInterface);
			var wrapped = GenericInterfaceWrapper.Wrap(source);

			Assert.IsNotNull(wrapped);
			Assert.AreNotEqual(source, wrapped);
			Assert.AreEqual(source.FullName, wrapped.FullName);
		}

		[Test]
		public void WrappedInterfaceHasSameNonGenericMethods()
		{
			var source = typeof(IGenericInterface);
			var wrapped = GenericInterfaceWrapper.Wrap(source);

			foreach (var sm in source.GetMethods())
			{
				if (!sm.IsGenericMethod)
				{
					var wm = wrapped.GetMethod(sm.Name);
					Assert.IsNotNull(wm);
					Assert.AreEqual(sm.ToString(), wm.ToString());
				}
			}
		}

		[Test]
		public void WrappedMethodNameTest1()
		{
			var mi = typeof(IGenericInterface).GetMethod("GetVersion");
			Assert.IsNotNull(mi);

			var newName = GenericInterfaceWrapper.GetWrappedName(mi);
			Assert.AreEqual("GetVersion", newName); // same as above
		}

		[Test]
		public void WrappedMethodNameTest2()
		{
			var mi = typeof(IGenericInterface).GetMethod("DoSomething");
			Assert.IsNotNull(mi);

			var newName = GenericInterfaceWrapper.GetWrappedName(mi);
			Assert.AreEqual("DoSomething<A, B, C>", newName);
		}

		[Test]
		public void WrappedInterfaceHasWrappedGenericMethods1()
		{
			var source = typeof(IGenericInterface);
			var wrapped = GenericInterfaceWrapper.Wrap(source);

			var mi = source.GetMethod("Compute");
			var newName = GenericInterfaceWrapper.GetWrappedName(mi);
			mi = wrapped.GetMethod(newName);
			Assert.IsNotNull(mi);

			var parameters = mi.GetParameters();
			Assert.AreEqual(5, parameters.Length);
			Assert.AreEqual(typeof(object), mi.ReturnType);

			// generic parameters
			Assert.AreEqual(typeof(Type), parameters[0].ParameterType);
			Assert.AreEqual(typeof(Type), parameters[1].ParameterType);

			// method arguments
			Assert.AreEqual(typeof(object), parameters[2].ParameterType);
			Assert.AreEqual(typeof(int), parameters[3].ParameterType);
			Assert.AreEqual(typeof(string), parameters[4].ParameterType);
		}

		[Test]
		public void WrappedInterfaceHasWrappedGenericMethods2()
		{
			var source = typeof(IGenericInterface);
			var wrapped = GenericInterfaceWrapper.Wrap(source);

			var mi = source.GetMethod("DoSomething");
			var newName = GenericInterfaceWrapper.GetWrappedName(mi);
			mi = wrapped.GetMethod(newName);
			Assert.IsNotNull(mi);

			var parameters = mi.GetParameters();
			Assert.AreEqual(6, parameters.Length);
			Assert.AreEqual(typeof(void), mi.ReturnType);

			// generic parameters
			Assert.AreEqual(typeof(Type), parameters[0].ParameterType);
			Assert.AreEqual(typeof(Type), parameters[1].ParameterType);
			Assert.AreEqual(typeof(Type), parameters[2].ParameterType);

			// method arguments
			Assert.AreEqual(typeof(object), parameters[3].ParameterType);
			Assert.AreEqual(typeof(object), parameters[4].ParameterType);
			Assert.AreEqual(typeof(object), parameters[5].ParameterType);
		}

		[Test]
		public void WrappedInterfaceHasProperties1()
		{
			var source = typeof(IGenericInterface);
			var wrapped = GenericInterfaceWrapper.Wrap(source);

			var pi1 = source.GetProperty("LastDate");
			Assert.IsNotNull(pi1);
			Assert.IsNotNull(pi1.GetGetMethod());
			Assert.IsNotNull(pi1.GetSetMethod());
			Assert.AreEqual(pi1.PropertyType, typeof(DateTime));

			var pi2 = wrapped.GetProperty("LastDate");
			Assert.IsNotNull(pi2);
			Assert.IsNotNull(pi2.GetGetMethod());
			Assert.IsNotNull(pi2.GetSetMethod());
			Assert.AreEqual(pi2.PropertyType, typeof(DateTime));
		}

		[Test]
		public void WrappedInterfaceHasProperties2()
		{
			var source = typeof(IGenericInterface);
			var wrapped = GenericInterfaceWrapper.Wrap(source);

			var pi1 = source.GetProperty("Name");
			Assert.IsNotNull(pi1);
			Assert.IsNotNull(pi1.GetGetMethod());
			Assert.IsNull(pi1.GetSetMethod());
			Assert.AreEqual(pi1.PropertyType, typeof(string));

			var pi2 = wrapped.GetProperty("Name");
			Assert.IsNotNull(pi2);
			Assert.IsNotNull(pi2.GetGetMethod());
			Assert.IsNull(pi2.GetSetMethod());
			Assert.AreEqual(pi2.PropertyType, typeof(string));
		}

		[Test]
		public void WrappedInterfaceHasProperties3()
		{
			var source = typeof(IGenericInterface);
			var wrapped = GenericInterfaceWrapper.Wrap(source);

			var pi1 = source.GetProperty("Priority");
			Assert.IsNotNull(pi1);
			Assert.IsNull(pi1.GetGetMethod());
			Assert.IsNotNull(pi1.GetSetMethod());
			Assert.AreEqual(pi1.PropertyType, typeof(int));

			var pi2 = wrapped.GetProperty("Priority");
			Assert.IsNotNull(pi2);
			Assert.IsNull(pi2.GetGetMethod());
			Assert.IsNotNull(pi2.GetSetMethod());
			Assert.AreEqual(pi2.PropertyType, typeof(int));
		}

		[Test]
		public void WrappedInterfaceHasEvents()
		{
			var source = typeof(IGenericInterface);
			var wrapped = GenericInterfaceWrapper.Wrap(source);

			var ev1 = source.GetEvent("OnPrioritySet");
			Assert.IsNotNull(ev1);
			Assert.IsNotNull(ev1.GetAddMethod());
			Assert.IsNotNull(ev1.GetRemoveMethod());
			Assert.AreEqual(ev1.EventHandlerType, typeof(EventHandler<EventArgs>));

			var ev2 = wrapped.GetEvent("OnPrioritySet");
			Assert.IsNotNull(ev2);
			Assert.IsNotNull(ev2.GetAddMethod());
			Assert.IsNotNull(ev2.GetRemoveMethod());
			Assert.AreEqual(ev2.EventHandlerType, typeof(EventHandler<EventArgs>));
		}
	}
}
