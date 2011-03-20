using System;
using System.Linq;
using System.Text;

namespace GenericWrappers
{
	/// <summary>
	/// Represents list of generic type parameters for MethodHolder
	/// </summary>
	public class GenericParameterList
	{
		/// <summary>
		/// Generic parameter types
		/// </summary>
		public Type[] Types { get; private set; }

		/// <summary>
		/// String key for dictionary lookup
		/// </summary>
		string Key { get; set; }

		/// <summary>
		/// Initializes GenericParameterList instance
		/// </summary>
		/// <param name="types">Generic parameter list</param>
		public GenericParameterList(params Type[] types)
		{
			Types = types;

			var sb = new StringBuilder();
			foreach (var t in types)
			{
				sb.Append(t.FullName);
			}

			Key = sb.ToString();
		}

		public override bool Equals(object obj)
		{
			var list = obj as GenericParameterList;
			if (list == null)
			{
				return false;
			}

			return Key.Equals(list.Key);
		}

		public override int GetHashCode()
		{
			return Key.GetHashCode();
		}
	}
}
