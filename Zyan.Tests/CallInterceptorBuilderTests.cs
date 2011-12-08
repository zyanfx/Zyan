using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
	/// Test class for strong-typed call interceptor builder.
	/// </summary>
	[TestClass]
	public class CallInterceptorBuilderTests
	{
		public interface IInterceptableComponent
		{
			void Procedure();

			void Procedure(int arg1);

			void Procedure(string arg1, char arg2);

			void Procedure(decimal arg1, Guid arg2, DateTime arg3);

			void Procedure(long arg1, TimeSpan arg2, short arg3, IInterceptableComponent arg4);

			void Procedure(object arg1, ushort arg2, sbyte arg3, IDisposable arg4, ulong arg5);

			int Function();

			byte Function(char c);

			char Function(string a, int b);

			string Function(int a, DateTime b, TimeSpan c);

			IDisposable Function(bool a, byte b, char c, int d);
		}

		[TestMethod]
		public void Builder_ForAction()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Action(c => c.Procedure(), data => Console.WriteLine("Procedure called."));

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Procedure", interceptor.MemberName);
			Assert.AreEqual(0, interceptor.ParameterTypes.Length);
		}

		[TestMethod]
		public void Builder_ForActionWith1Argument()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Action<int>(
					(c, arg1) => c.Procedure(arg1),
					(data, arg1) => Console.WriteLine("Procedure called: {0}.", arg1));

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Procedure", interceptor.MemberName);
			Assert.AreEqual(1, interceptor.ParameterTypes.Length);
			Assert.AreEqual(typeof(int), interceptor.ParameterTypes[0]);
		}

		[TestMethod]
		public void Builder_ForActionWith2Arguments()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Action<string, char>(
					(c, arg1, arg2) => c.Procedure(arg1, arg2),
					(data, arg1, arg2) => Console.WriteLine("Procedure called: {0}, {1}.", arg1, arg2));

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Procedure", interceptor.MemberName);
			Assert.AreEqual(2, interceptor.ParameterTypes.Length);
			Assert.AreEqual(typeof(string), interceptor.ParameterTypes[0]);
			Assert.AreEqual(typeof(char), interceptor.ParameterTypes[1]);
		}

		[TestMethod]
		public void Builder_ForActionWith3Arguments()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Action<decimal, Guid, DateTime>(
					(c, arg1, arg2, arg3) => c.Procedure(arg1, arg2, arg3),
					(data, arg1, arg2, arg3) => Console.WriteLine("Procedure called: {0}, {1}, {3}.", arg1, arg2, arg3));

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Procedure", interceptor.MemberName);
			Assert.AreEqual(3, interceptor.ParameterTypes.Length);
			Assert.AreEqual(typeof(decimal), interceptor.ParameterTypes[0]);
			Assert.AreEqual(typeof(Guid), interceptor.ParameterTypes[1]);
			Assert.AreEqual(typeof(DateTime), interceptor.ParameterTypes[2]);
		}

		[TestMethod]
		public void Builder_ForActionWith4Arguments()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Action<long, TimeSpan, short, IInterceptableComponent>(
					(c, arg1, arg2, arg3, arg4) => c.Procedure(arg1, arg2, arg3, arg4),
					(data, arg1, arg2, arg3, arg4) => Console.WriteLine("Procedure called: {0}, {1}, {2}, {3}.", arg1, arg2, arg3, arg4));

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Procedure", interceptor.MemberName);
			Assert.AreEqual(4, interceptor.ParameterTypes.Length);
			Assert.AreEqual(typeof(long), interceptor.ParameterTypes[0]);
			Assert.AreEqual(typeof(TimeSpan), interceptor.ParameterTypes[1]);
			Assert.AreEqual(typeof(short), interceptor.ParameterTypes[2]);
			Assert.AreEqual(typeof(IInterceptableComponent), interceptor.ParameterTypes[3]);
		}

		[TestMethod]
		public void Builder_ForActionWith5Arguments()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Action<object, ushort, sbyte, IDisposable, ulong>(
					(c, arg1, arg2, arg3, arg4, arg5) => c.Procedure(arg1, arg2, arg3, arg4, arg5),
					(data, arg1, arg2, arg3, arg4, arg5) => Console.WriteLine("Procedure called: {0}, {1}, {2}, {3}, {4}.", arg1, arg2, arg3, arg4, arg5));

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Procedure", interceptor.MemberName);
			Assert.AreEqual(5, interceptor.ParameterTypes.Length);
			Assert.AreEqual(typeof(object), interceptor.ParameterTypes[0]);
			Assert.AreEqual(typeof(ushort), interceptor.ParameterTypes[1]);
			Assert.AreEqual(typeof(sbyte), interceptor.ParameterTypes[2]);
			Assert.AreEqual(typeof(IDisposable), interceptor.ParameterTypes[3]);
			Assert.AreEqual(typeof(ulong), interceptor.ParameterTypes[4]);
		}

		[TestMethod]
		public void Builder_ForFunc()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Func<int>(c => c.Function(), data => 0);

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Function", interceptor.MemberName);
			Assert.AreEqual(0, interceptor.ParameterTypes.Length);
		}

		[TestMethod]
		public void Builder_ForFuncWith1Argument()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Func<char, byte>(
					(c, ch) => c.Function(ch),
					(data, ch) => (byte)ch);

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Function", interceptor.MemberName);
			Assert.AreEqual(1, interceptor.ParameterTypes.Length);
			Assert.AreEqual(typeof(char), interceptor.ParameterTypes[0]);
		}

		[TestMethod]
		public void Builder_ForFuncWith2Arguments()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Func<string, int, char>(
					(c, s, i) => c.Function(s, i),
					(data, s, i) => s[0]);

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Function", interceptor.MemberName);
			Assert.AreEqual(2, interceptor.ParameterTypes.Length);
			Assert.AreEqual(typeof(string), interceptor.ParameterTypes[0]);
			Assert.AreEqual(typeof(int), interceptor.ParameterTypes[1]);
		}

		[TestMethod]
		public void Builder_ForFuncWith3Arguments()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Func<int, DateTime, TimeSpan, string>(
					(c, i, d, t) => c.Function(i, d, t),
					(data, i, d, t) => i.ToString());

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Function", interceptor.MemberName);
			Assert.AreEqual(3, interceptor.ParameterTypes.Length);
			Assert.AreEqual(typeof(int), interceptor.ParameterTypes[0]);
			Assert.AreEqual(typeof(DateTime), interceptor.ParameterTypes[1]);
			Assert.AreEqual(typeof(TimeSpan), interceptor.ParameterTypes[2]);
		}

		[TestMethod]
		public void Builder_ForFuncWith4Arguments()
		{
			var interceptor = CallInterceptor
				.For<IInterceptableComponent>()
				.Func<bool, byte, char, int, IDisposable>(
					(c, b, by, ch, i) => c.Function(b, by, ch, i),
					(data, b, by, ch, i) => new MemoryStream());

			Assert.IsNotNull(interceptor);
			Assert.AreEqual(MemberTypes.Method, interceptor.MemberType);
			Assert.AreEqual("Function", interceptor.MemberName);
			Assert.AreEqual(4, interceptor.ParameterTypes.Length);
			Assert.AreEqual(typeof(bool), interceptor.ParameterTypes[0]);
			Assert.AreEqual(typeof(byte), interceptor.ParameterTypes[1]);
			Assert.AreEqual(typeof(char), interceptor.ParameterTypes[2]);
			Assert.AreEqual(typeof(int), interceptor.ParameterTypes[3]);
		}
	}
}
