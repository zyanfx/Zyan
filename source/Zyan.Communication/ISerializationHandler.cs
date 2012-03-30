using System;

namespace Zyan.Communication
{
	/// <summary>
	/// Interface for custom serialization handling.
	/// </summary>
	public interface ISerializationHandler
	{
		/// <summary>
		/// Serializes an object.
		/// </summary>
		/// <param name="data">Object</param>
		/// <returns>Serialized raw data</returns>
		byte[] Serialize(object data);

		/// <summary>
		/// Deserializes raw data back into an object of a specified type.
		/// </summary>
		/// <param name="dataType">Type for deserialization</param>
        /// <param name="data">Serialized raw data</param>
		/// <returns>Object</returns>
		object Deserialize(Type dataType, byte[] data);
	}
}
