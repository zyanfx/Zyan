using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Provides improved type search facilities.
	/// </summary>
	public static class TypeHelper
	{
		private const char OpenBracket = '[';
		private const char ClosedBracket = ']';
		private const char Comma = ',';

		/// <summary>
		/// Gets the <see cref="Type"/> with the specified name, performing a case-sensitive search.
		/// Looks for the specified assembly in the current <see cref="AppDomain"/> assembly list.
		/// </summary>
		/// <param name="fullName">The assembly-qualified name of the type to get.</param>
		/// <returns>The type with the specified name, if found; otherwise, null.</returns>
		public static Type GetType(string fullName)
		{
			return GetType(fullName, false);
		}

		/// <summary>
		/// Gets the <see cref="Type"/> with the specified name, performing a case-sensitive search.
		/// Looks for the specified assembly in the current <see cref="AppDomain"/> assembly list.
		/// </summary>
		/// <param name="fullName">The assembly-qualified name of the type to get.</param>
		/// <param name="throwOnError">True to throw the <see cref="TypeLoadException"/> if the type can not be found.</param>
		/// <returns>The type with the specified name. If the type is not found, the throwOnError 
		/// parameter specifies whether null is returned or an exception is thrown.</returns>
		public static Type GetType(string fullName, bool throwOnError)
		{
			if (string.IsNullOrEmpty(fullName))
			{
				throw new ArgumentNullException("fullName");
			}

			var result = Type.GetType(fullName);
			if (result != null)
			{
				return result;
			}

			// try to find the loaded assembly in the current appication domain
			if (fullName.Contains(Comma))
			{
				// extract assembly name
				var bracketIndex = fullName.LastIndexOf(ClosedBracket);
				var commaIndex = fullName.IndexOf(Comma, bracketIndex < 0 ? 0 : bracketIndex); // first comma after the last closed bracket
				var typeName = fullName.Substring(0, commaIndex).Trim();
				var fullAssemblyName = fullName.Substring(commaIndex + 1).Trim();
				var asmName = fullAssemblyName.Split(Comma).FirstOrDefault();

				// extract generic arguments recursively
				Type[] genericArguments = null;
				bracketIndex = fullName.IndexOf(OpenBracket);
				if (bracketIndex > 0)
				{
					var genArgs = typeName.Substring(bracketIndex + 1, typeName.Length - bracketIndex - 2).Trim();
					genericArguments = ExtractGenericArguments(genArgs, throwOnError).ToArray();
					typeName = typeName.Substring(0, bracketIndex).Trim();
				}

				// index loaded assemblies by their names
				var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToLookup(a => a.GetName().Name.ToLower());
				if (loadedAssemblies.Contains(asmName.ToLower()))
				{
					// try to find the closest match
					var asmList = loadedAssemblies[asmName.ToLower()];
					var assembly =
						asmList.Where(a => a.GetName().FullName == fullAssemblyName).FirstOrDefault() ??
						asmList.Where(a => a.GetName().FullName.ToLower() == fullAssemblyName.ToLower()).FirstOrDefault() ??
						asmList.Where(a => a.GetName().Name == asmName).FirstOrDefault() ??
						asmList.Where(a => a.GetName().Name.ToLower() == asmName.ToLower()).FirstOrDefault();

					if (assembly != null)
					{
						result = assembly.GetType(typeName);
					}
				}

				// try to load assembly using standard name resoluion policy
				if (result == null)
				{
					try
					{
						var asm = Assembly.Load(fullAssemblyName);
						result = asm.GetType(typeName);
					}
					catch // assembly or type cannot be loaded
					{
					}
				}

				// make generic type
				if (result != null && result.IsGenericType && genericArguments != null && 
					genericArguments.Length == result.GetGenericArguments().Length)
				{
					result = result.MakeGenericType(genericArguments);
				}
			}

			// throw exception if type is not found
			if (result == null && throwOnError)
			{
				throw new TypeLoadException("Type not found: " + fullName);
			}

			return result;
		}

		private static IEnumerable<Type> ExtractGenericArguments(string genericArguments, bool throwOnError)
		{
			var count = 0;
			var argument = new StringBuilder();

			foreach (char c in genericArguments)
			{
				argument.Append(c);

				switch (c)
				{
					case OpenBracket:
						count++;
						break;

					case ClosedBracket:
						count--;
						break;
				}

				if (count == 0)
				{
					var typeName = argument.ToString().Trim(OpenBracket, ClosedBracket, Comma);
					if (typeName.Length > 0)
					{
						yield return GetType(typeName, throwOnError);
					}

					argument.Length = 0;
				}
			}
		}
	}
}
