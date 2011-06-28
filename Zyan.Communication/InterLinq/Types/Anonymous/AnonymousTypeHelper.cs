using System;
using System.Linq;
using System.Collections.Generic;

namespace Zyan.InterLinq.Types.Anonymous
{
	/// <summary>
	/// A static class that contains static methods to recognize some special types like anonymous types and
	/// display classes.
	/// </summary>
	internal static class AnonymousTypeHelper
	{
		/// <summary>
		/// Returns true if the given <see cref="Type"/> is an anonymous type.
		/// </summary>
		/// <param name="t"><see cref="Type"/> to test.</param>
		/// <returns>True, if the type is an anonymous type. False, if not.</returns>
		/// <remarks>
		/// A anonymous type is not really marked as anonymous.
		/// The only way to recognize it is it's name.
		/// Maybe in future versions they will be marked.
		/// </remarks>
		public static bool IsAnonymous(this Type t)
		{
			return t.Name.StartsWith("<>f__AnonymousType");
		}

		/// <summary>
		/// Returns true if the given <see cref="Type"/> is a display class.
		/// </summary>
		/// <param name="t"><see cref="Type"/> to test.</param>
		/// <returns>True, if the type is a display class. False, if not.</returns>
		/// <remarks>
		/// A display class is not really marked as display class.
		/// The only way to recognize it is it's name.
		/// Maybe in future versions they will be marked.
		/// </remarks>
		public static bool IsDisplayClass(this Type t)
		{
			return t.Name.StartsWith("<>c__DisplayClass");
		}

		/// <summary>
		/// Returns true if the given <see cref="Type"/> is a <see cref="IGrouping{TKey, TElement}"/> class.
		/// </summary>
		/// <param name="t"><see cref="Type"/> to test.</param>
		/// <returns>Returns true if the given <see cref="Type"/> is a <see cref="IGrouping{TKey, TElement}"/> class.</returns>
		public static bool IsIGrouping(this Type t)
		{
			if (!t.IsGenericType)
			{
				return false;
			}
			if (t.GetGenericTypeDefinition() == typeof(IGrouping<,>))
			{
				return true;
			}
			foreach (Type interfaceType in t.GetInterfaces())
			{
				if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true if the given <see cref="Type"/> is a <see cref="IEnumerator{t}"/> class.
		/// </summary>
		/// <param name="t"><see cref="Type"/> to test.</param>
		/// <returns>Returns true if the given <see cref="Type"/> is a <see cref="IEnumerator{t}"/> class.</returns>
		public static bool IsEnumerator(this Type t)
		{
			if (!t.IsGenericType)
			{
				return false;
			}
			if (t.GetGenericTypeDefinition() == typeof(IEnumerator<>))
			{
				return true;
			}
			foreach (Type interfaceType in t.GetInterfaces())
			{
				if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerator<>))
				{
					return true;
				}
			}
			return false;
		}
	}
}
