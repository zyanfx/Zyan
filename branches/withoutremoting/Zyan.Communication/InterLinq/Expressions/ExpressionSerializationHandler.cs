using System;
using System.IO;
using System.Linq.Expressions;
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
		BinaryFormatter Formatter { get; set; }

		/// <summary>
		/// Initializes Linq expressions serialization handler
		/// </summary>
		public ExpressionSerializationHandler()
		{
			Formatter = new BinaryFormatter();
			Formatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
			Formatter.FilterLevel = TypeFilterLevel.Low;
		}

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
