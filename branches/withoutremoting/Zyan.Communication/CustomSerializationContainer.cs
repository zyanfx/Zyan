using System;

namespace Zyan.Communication
{
	/// <summary>
	/// Container for custom serialized objects.
	/// </summary>
	[Serializable]
	public class CustomSerializationContainer
	{
		/// <summary>
		/// Creates a new instance of the CustomSerializationContainer class.
		/// </summary>
		public CustomSerializationContainer()
		{
		}

		/// <summary>
		/// Creates a new instance of the CustomSerializationContainer class.
		/// </summary>
		/// <param name="handledType">Handled type</param>
		/// <param name="dataType">Actual type</param>
		/// <param name="data">Raw data</param>
		public CustomSerializationContainer(Type handledType, Type dataType, byte[] data)
		{
			HandledType = handledType;
			DataType = dataType;
			Data = data;
		}

		/// <summary>
		/// Gets or sets the handled type.
		/// </summary>
		public Type HandledType
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the actual type.
		/// </summary>
		public Type DataType
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the serialized raw data.
		/// </summary>
		public byte[] Data
		{
			get;
			set;
		}
	}
}
