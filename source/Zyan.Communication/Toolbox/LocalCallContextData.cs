using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Threading;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Holds data for the logical call context.
	/// Prevents the data from leaving the current application domain.
	/// </summary>
	[Serializable]
	internal sealed class LocalCallContextData : ISerializable, ILogicalThreadAffinative
	{
		private LocalCallContextData()
		{
		}

		/// <summary>
		/// Gets or sets the value stored in a LogicalCallContext.
		/// </summary>
		/// <remarks>
		/// The data doesn't leak to another application domain.
		/// </remarks>
		private object Value { get; set; }

		/// <summary>
		/// Gets or sets the name of a LogicalCallContext slot.
		/// </summary>
		private string Name { get; set; }

		[ThreadStatic]
		private static Dictionary<string, object> threadStaticValues;

		private static Dictionary<string, object> ThreadStaticValues
		{
			get { return threadStaticValues ?? (threadStaticValues = new Dictionary<string, object>()); }
		}

		/// <summary>
		/// Stores the instance data.
		/// </summary>
		/// <param name="info">Serialization info.</param>
		/// <param name="context">Streaming context.</param>
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Name", Name);

			// instead of saving Value to the SerializationInfo, we save it to TLS before making a remote call
			ThreadStaticValues[Name ?? string.Empty] = Value;
		}

		public LocalCallContextData(SerializationInfo info, StreamingContext context)
		{
			// backward-compatible deserialization
			foreach (SerializationEntry entry in info)
			{
				if (entry.Name == "Name")
				{
					Name = (string)entry.Value;
					break;
				}
			}

			// restore the value from TLS after making the remote call.
			object value;
			if (ThreadStaticValues.TryGetValue(Name ?? string.Empty, out value))
			{
				Value = value;
				ThreadStaticValues.Remove(Name ?? string.Empty);
			}
		}

		/// <summary>
		/// Retrieves an object with the specified name from the call context.
		/// </summary>
		/// <typeparam name="T">The type of the object to retrieve.</typeparam>
		/// <param name="name">The name of the object to retrieve.</param>
		/// <param name="defaultValue">The default value to return if the object is not found.</param>
		/// <returns>The value of the object.</returns>
		public static T GetData<T>(string name, T defaultValue = default(T))
		{
			var data = GetData(name) ?? defaultValue;
			return (T)data;
		}

		/// <summary>
		/// Retrieves an object with the specified name from the call context.
		/// </summary>
		/// <param name="name">The name of the object to retrieve.</param>
		/// <returns>The value of the object.</returns>
		public static object GetData(string name)
		{
			var data = CallContext.GetData(name) as LocalCallContextData;
			if (data == null)
			{
				return null;
			}

			return data.Value;
		}

		/// <summary>
		/// Stores the given object and associates it with the specified name.
		/// </summary>
		/// <param name="name">The name of the object to retrieve.</param>
		/// <param name="value">The value of the object.</param>
		public static void SetData(string name, object value)
		{
			CallContext.SetData(name, new LocalCallContextData
			{
				Value = value,
				Name = name
			});
		}
	}
}
