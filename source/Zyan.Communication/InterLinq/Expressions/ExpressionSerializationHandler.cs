using System;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Zyan.Communication;

namespace Zyan.InterLinq.Expressions
{
	/// <summary>
	/// Serialization handler for Linq expressions
	/// </summary>
	public class ExpressionSerializationHandler : ISerializationHandler
	{
		private BinaryFormatter Formatter { get; set; }
#if !FX3
		private class ShortenerBinder : SerializationBinder
		{
			private struct FullNameShortName
			{
				public FullNameShortName(string f, string s)
				{
					FullName = f;
					ShortName = s;
				}

				public string FullName { get; }
				public string ShortName { get; }
			}

			private static FullNameShortName[] KnownAssemblies = new[]
			{
				new FullNameShortName(typeof(ZyanConnection).Assembly.FullName, "!"), // Zyan.Communication
				new FullNameShortName(typeof(ExpressionType).Assembly.FullName, "@"), // System.Core
				new FullNameShortName(typeof(string).Assembly.FullName, "#") // mscorlib
			};

			private static FullNameShortName[] ZyanNamespaces = new[]
			{
				new FullNameShortName("Zyan.InterLinq.Expressions.SerializableTypes.", "!"),
				new FullNameShortName("Zyan.InterLinq.Types.Anonymous.", "@"),
				new FullNameShortName("Zyan.InterLinq.Communication.", "#"),
				new FullNameShortName("Zyan.InterLinq.Expressions.", "$"),
				new FullNameShortName("Zyan.InterLinq.Types.", "%"),
				new FullNameShortName("Zyan.Communication.", "^"),
				new FullNameShortName("Zyan.InterLinq.", "&"),
				new FullNameShortName("Zyan.", "*")
			};

			public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
			{
				var isgen = serializedType.IsGenericType;

				// make sure to fall back to defaults
				assemblyName = typeName = null;

				// replace the assembly name with the placeholder
				foreach (var asm in KnownAssemblies)
				{
					if (serializedType.Assembly.FullName == asm.FullName)
					{
						assemblyName = asm.ShortName;
					}
				}

				// replace the namespace part with the placeholder
				if (serializedType.FullName.StartsWith("Zyan."))
				{
					typeName = serializedType.FullName;
					foreach (var nsp in ZyanNamespaces)
					{
						if (typeName.StartsWith(nsp.FullName))
						{
							typeName = nsp.ShortName + typeName.Substring(nsp.FullName.Length);
							return;
						}
					}
				}
			}

			public override Type BindToType(string assemblyName, string typeName)
			{
				// replace the placeholder with the assembly name
				foreach (var asm in KnownAssemblies)
				{
					if (assemblyName == asm.ShortName)
					{
						assemblyName = asm.FullName;
					}
				}

				// replace the type name placeholder with the real type name
				foreach (var nsp in ZyanNamespaces)
				{
					if (typeName.StartsWith(nsp.ShortName))
					{
						typeName = nsp.FullName + typeName.Substring(nsp.ShortName.Length);
						break;
					}
				}

				return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName));
			}
		}

		/// <summary>
		/// Initializes Linq expressions serialization handler
		/// </summary>
		public ExpressionSerializationHandler()
		{
			Formatter = new BinaryFormatter
			{
				AssemblyFormat = FormatterAssemblyStyle.Simple,
				FilterLevel = TypeFilterLevel.Low,
				Binder = new ShortenerBinder()
			};
		}
#else
		/// <summary>
		/// Initializes Linq expressions serialization handler
		/// </summary>
		public ExpressionSerializationHandler()
		{
			Formatter = new BinaryFormatter
			{
				AssemblyFormat = FormatterAssemblyStyle.Simple,
				FilterLevel = TypeFilterLevel.Low
			};
		}
#endif
		/// <summary>
		/// Serializes Linq expression into raw byte array
		/// </summary>
		/// <param name="data">Linq expression</param>
		/// <returns>Raw data</returns>
		public byte[] Serialize(object data)
		{
			var expression = data as Expression;
			if (expression == null)
			{
				return new byte[0];
			}

			var sx = expression.MakeSerializable();
			using (var ms = new MemoryStream())
			{
				Formatter.Serialize(ms, sx);
				return ms.ToArray();
			}
		}

		/// <summary>
		/// Deserializes raw byte array
		/// </summary>
		/// <param name="dataType"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public object Deserialize(Type dataType, byte[] data)
		{
			if (!typeof(Expression).IsAssignableFrom(dataType))
			{
				throw new InvalidOperationException();
			}

			if (data == null || data.Length < 1)
			{
				return null;
			}

			using (var ms = new MemoryStream(data))
			{
				var sx = Formatter.Deserialize(ms) as SerializableExpression;
				return sx.Deserialize();
			}
		}
	}
}
