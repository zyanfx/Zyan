using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Holds data for the logical call context.
	/// Prevents data from leaving the current application domain.
	/// </summary>
	[Serializable]
	internal sealed class LocalCallContextData : ILogicalThreadAffinative
	{
		private LocalCallContextData()
		{ 
		}

		// prevent data from leaking to another application domain
		[NonSerialized]
		private object Value;

		/// <summary>
		/// Retrieves an object with the specified name from the call context.
		/// </summary>
		/// <typeparam name="T">The type of the object to retrieve.</typeparam>
		/// <param name="name">The name of the object to retrieve.</param>
		/// <param name="defaultValue">The default value to return if the object is not found.</param>
		/// <returns>The value of the object.</returns>
		public static T GetData<T>(string name, T defaultValue = default(T))
		{
			return (T)GetData(name);
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
			CallContext.SetData(name, new LocalCallContextData { Value = value });
		}
	}
}
