using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.CSharp;
using Zyan.Communication;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Toolbox.Compression;
using Zyan.Communication.ChannelSinks.Compression;

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
	/// Test class for type helper.
	///</summary>
	[TestClass]
	public class TypeHelperTests
	{
		#region Dynamically-compiled secret assembly

		private const string SecretAssemblyName = "SecretAssembly";

		private const string SecretClassName = "SecretClass";

		private static Lazy<Assembly> SecretAssembly = new Lazy<Assembly>(() =>
		{
			var sourceCode = @"
				using System;

				[Serializable]
				internal class " + SecretClassName + @"
				{
					public override bool Equals(object obj)
					{
						return obj is " + SecretClassName + @";
					}
				}";

			var compiler = new CSharpCodeProvider();
			var options = new CompilerParameters
			{
				GenerateExecutable = false,
				OutputAssembly = SecretAssemblyName
			};

			var result = compiler.CompileAssemblyFromSource(options, sourceCode);
			return result.CompiledAssembly;
		});

		private static Lazy<string> SecretAssemblyFullName = new Lazy<string>(() => SecretAssembly.Value.GetName().FullName);

		private static Lazy<Type> SecretClass = new Lazy<Type>(() => SecretAssembly.Value.GetType(SecretClassName));

		private static Lazy<string> SecretClassFullName = new Lazy<string>(() => SecretClass.Value.AssemblyQualifiedName);

		[ClassInitialize]
		public static void ValidateSecretAssembly(TestContext dummy)
		{
			Assert.IsNotNull(SecretAssembly.Value);
			Assert.IsNotNull(SecretClass.Value);

			Assert.IsFalse(string.IsNullOrWhiteSpace(SecretAssemblyFullName.Value));
			Assert.IsFalse(string.IsNullOrWhiteSpace(SecretClassFullName.Value));

			Assert.IsTrue(SecretAssemblyFullName.Value.StartsWith(SecretAssemblyName));
			Assert.IsTrue(SecretClassFullName.Value.StartsWith(SecretClassName));
		}

		[ClassInitializeNonStatic]
		public void ValidateSecretAssemblyNonStatic()
		{
			ValidateSecretAssembly(null);
		}

		#endregion

		[TestMethod]
		public void TypeHelper_ReturnsStaticallyLinkedType()
		{
			var typeName = typeof(ZyanDispatcher).AssemblyQualifiedName;
			var type1 = Type.GetType(typeName);
			var type2 = TypeHelper.GetType(typeName);

			Assert.IsNotNull(type1);
			Assert.IsNotNull(type2);

			Assert.AreSame(type1, typeof(ZyanDispatcher));
			Assert.AreSame(type2, typeof(ZyanDispatcher));
		}

		[TestMethod]
		public void TypeHelper_ReturnsTypeFromDynamicAssemblyUsingFullAssemblyName()
		{
			var typeName = SecretClass.Value.AssemblyQualifiedName;
			var type = TypeHelper.GetType(typeName);

			Assert.IsNotNull(type);
			Assert.AreSame(type, SecretClass.Value);
		}

		[TestMethod]
		public void TypeHelper_ReturnsTypeFromDynamicAssemblyUsingPartialAssemblyName()
		{
			var typeName = SecretClassName + ", " + SecretAssemblyName;
			var type = TypeHelper.GetType(typeName);

			Assert.IsNotNull(type);
			Assert.AreSame(type, SecretClass.Value);
		}

		[TestMethod]
		public void TypeHelper_ReturnsTypeFromDynamicAssemblyUsingFullAssemblyNameWithBadCasing()
		{
			// convert assembly name to the upper case
			var typeName = SecretClass.Value.AssemblyQualifiedName;
			var indexOfComma = typeName.IndexOf(",");
			var asmName = typeName.Substring(indexOfComma + 1);
			typeName = typeName.Substring(0, indexOfComma + 1) + asmName.ToUpper();

			var type = TypeHelper.GetType(typeName);
			Assert.IsNotNull(type);
			Assert.AreSame(type, SecretClass.Value);
		}

		[TestMethod]
		public void TypeHelper_ReturnsTypeFromDynamicAssemblyUsingPartialAssemblyNameWithBadCasing()
		{
			var typeName = SecretClassName + ", " + SecretAssemblyName.ToUpper();
			var type = TypeHelper.GetType(typeName);

			Assert.IsNotNull(type);
			Assert.AreSame(type, SecretClass.Value);
		}

		[TestMethod]
		public void TypeHelper_ReturnsStaticallyLinkedOpenGenericType()
		{
			var type1 = typeof(Dictionary<,>);
			var type2 = TypeHelper.GetType(type1.AssemblyQualifiedName);

			Assert.IsNotNull(type2);
			Assert.AreSame(type1, type2);
		}

		[TestMethod]
		public void TypeHelper_ReturnsStaticallyLinkedGenericTypeWithArguments()
		{
			var type1 = typeof(Dictionary<Dictionary<string, List<int>>, Dictionary<List<string>, int>>);
			var type2 = TypeHelper.GetType(type1.AssemblyQualifiedName);

			Assert.IsNotNull(type2);
			Assert.AreSame(type1, type2);
		}

		[TestMethod]
		public void TypeHelper_ReturnsDynamicallyLinkedGenericTypeWithSingleArgument()
		{
			var type1 = typeof(List<>).MakeGenericType(SecretClass.Value);
			var type2 = TypeHelper.GetType(type1.AssemblyQualifiedName);

			Assert.IsNotNull(type2);
			Assert.AreSame(type1, type2);
		}

		[TestMethod]
		public void TypeHelper_ReturnsDynamicallyLinkedGenericTypeWithComplexArguments()
		{
			var type1 = typeof(Dictionary<,>).MakeGenericType(typeof(Dictionary<List<string>, int>), typeof(Dictionary<,>).MakeGenericType(typeof(List<string>), SecretClass.Value));
			var type2 = TypeHelper.GetType(type1.AssemblyQualifiedName);

			Assert.IsNotNull(type2);
			Assert.AreSame(type1, type2);
		}

		[TestMethod, ExpectedException(typeof(SerializationException))]
		public void BinaryFormatter_DeserializationProblems()
		{
			var obj = Activator.CreateInstance(SecretClass.Value);
			var fmt = new BinaryFormatter();

			using (var ms = new MemoryStream())
			{
				fmt.Serialize(ms, obj);

				Assert.IsTrue(ms.Length > 0);
				ms.Seek(0, SeekOrigin.Begin);

				var newObj = fmt.Deserialize(ms);
				Assert.IsNotNull(newObj);
			}
		}

		[TestMethod]
		public void BinaryFormatter_DynamicBinderAllowsDeserialization()
		{
			var obj = Activator.CreateInstance(SecretClass.Value);
			var fmt = new BinaryFormatter();
			fmt.Binder = new DynamicTypeBinder();

			using (var ms = new MemoryStream())
			{
				fmt.Serialize(ms, obj);

				Assert.IsTrue(ms.Length > 0);
				ms.Seek(0, SeekOrigin.Begin);

				var newObj = fmt.Deserialize(ms);
				Assert.IsNotNull(newObj);
				Assert.AreEqual(obj, newObj);
			}
		}

		[TestMethod]
		public void DefaultValuesTest()
		{
			var obj = typeof(void).GetDefaultValue();
			Assert.IsNull(obj);

			obj = typeof(string).GetDefaultValue();
			Assert.IsNull(obj);

			obj = typeof(ZyanConnection).GetDefaultValue();
			Assert.IsNull(obj);

			obj = typeof(int).GetDefaultValue();
			Assert.IsNotNull(obj);
			Assert.AreEqual(default(int), obj);

			obj = typeof(DateTime).GetDefaultValue();
			Assert.IsNotNull(obj);
			Assert.AreEqual(default(DateTime), obj);
		}
	}
}
