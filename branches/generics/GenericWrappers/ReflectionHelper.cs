/*
// Exepack.NET compressor
// http://www.codeplex.com/exepack
//
// Reflection helper methods
// Written by Y [14-02-09]
// Copyright (c) 2008-2010 Alexey Yakovlev
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GenericWrappers
{
	/// <summary>
	/// Strong-typed methods for common reflection-related tasks
	/// </summary>
	public static class ReflectionHelper
	{
		public static T GetCustomAttribute<T>(this MemberInfo mi, bool inherit) where T : Attribute
		{
			object[] attributes = mi.GetCustomAttributes(typeof(T), inherit);

			if (attributes.Length == 0)
				throw new IndexOutOfRangeException(String.Format("Method {0} is not decorated with {1} attribute", mi.Name, typeof(T).ToString()));

			return (T)attributes[0];
		}

		public static T[] GetCustomAttributes<T>(this MemberInfo mi, bool inherit) where T : Attribute
		{
			object[] attributes = mi.GetCustomAttributes(typeof(T), inherit);
			T[] result = new T[attributes.Length];
			Array.Copy(attributes, result, result.Length);
			return result;
		}

		public static T[] GetCustomAttributes<T>(this MemberInfo mi) where T : Attribute
		{
			return mi.GetCustomAttributes<T>(false);
		}

		public static T GetCustomAttribute<T>(this MemberInfo mi) where T : Attribute
		{
			return mi.GetCustomAttribute<T>(false);
		}

		// where T : Delegate — not supported by C#

		public static T CreateDelegate<T>(this MethodInfo mi, object instance) where T : class // Delegate
		{
			return (T)(object)Delegate.CreateDelegate(typeof(T), instance, mi);
		}

		public static T CreateDelegate<T>(this MethodInfo mi) where T : class // Delegate
		{
			return (T)(object)Delegate.CreateDelegate(typeof(T), null, mi);
		}
	}
}
