using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Custom serialization binder that uses TypeHelper to resolve types.
	/// </summary>
	public class DynamicTypeBinder : SerializationBinder
	{
		/// <summary>
		/// When overridden in a derived class, controls the binding of a serialized object to a type.
		/// </summary>
		/// <param name="assemblyName">Specifies the <see cref="T:System.Reflection.Assembly"/> name of the serialized object.</param>
		/// <param name="typeName">Specifies the <see cref="T:System.Type"/> name of the serialized object.</param>
		/// <returns>
		/// The type of the object the formatter creates a new instance of.
		/// </returns>
		public override Type BindToType(string assemblyName, string typeName)
		{
			return TypeHelper.GetType(typeName + ", " + assemblyName);
		}
	}
}
