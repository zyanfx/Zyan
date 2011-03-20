using System;
using System.Collections.Generic;
using System.Reflection;
using FastReflectionLib;

namespace GenericWrappers
{
	/// <summary>
	/// Holds, caches and invokes generic method with different type parameters
	/// </summary>
	public class GenericMethodHolder
	{
		/// <summary>
		/// Template generic method
		/// </summary>
		MethodInfo GenericMethod { get; set; }

		/// <summary>
		/// Typed generic method cache
		/// </summary>
		Dictionary<GenericParameterList, MethodInfo> GenericMethodCache { get; set; }

		/// <summary>
		/// Initializes GenericMethodHolder instance
		/// </summary>
		/// <param name="type">Type to reflect</param>
		/// <param name="name">Method name</param>
		/// <param name="genericCount">Generic parameter count</param>
		public GenericMethodHolder(Type type, string name, int genericCount)
			: this(type, name, genericCount, new Type[0])
		{ 
		}

		/// <summary>
		/// Initializes GenericMethodHolder instance
		/// </summary>
		/// <param name="type">Type to reflect</param>
		/// <param name="name">Method name</param>
		/// <param name="genericCount">Generic parameter count</param>
		/// <param name="paramTypes">Method parameter types</param>
		public GenericMethodHolder(Type type, string name, int genericCount, params Type[] paramTypes)
		{
			GenericMethodCache = new Dictionary<GenericParameterList, MethodInfo>();
			GenericMethod = Lookup(type, name, genericCount, paramTypes);
		}

		private MethodInfo Lookup(Type type, string name, int genericCount, params Type[] paramTypes)
		{
			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

			foreach (var method in methods)
			{
				if (method.Name == name && method.IsGenericMethodDefinition && 
					method.GetGenericArguments().Length == genericCount)
				{
					var parameters = method.GetParameters();
					if (parameters.Length == paramTypes.Length)
					{
						bool match = true;
						for (var i = 0; i < parameters.Length; i++)
						{
							if (paramTypes[i] != null && 
								!paramTypes[i].IsAssignableFrom(parameters[i].ParameterType))
							{
								match = false;
								break;
							}
						}

						if (match)
						{
							return method;
						}
					}
				}
			}

			throw new ApplicationException("Method not found: " + name);
		}

		public override string ToString()
		{
			return GenericMethod.ToString();
		}

		/// <summary>
		/// Invokes generic method
		/// </summary>
		/// <param name="instance">Object to invoke generic method on</param>
		/// <param name="typeParameters">Generic parameters</param>
		/// <param name="parameters">Method parameters</param>
		public object Invoke(object instance, Type[] typeParameters, params object[] parameters)
		{
			var typeList = new GenericParameterList(typeParameters);
			return Invoke(instance, typeList, parameters);
		}

		/// <summary>
		/// Invokes generic method
		/// </summary>
		/// <param name="instance">Object to invoke generic method on</param>
		/// <param name="typeList">Generic parameter list</param>
		/// <param name="parameters">Method parameters</param>
		public object Invoke(object instance, GenericParameterList typeList, params object[] parameters)
		{
			if (!GenericMethodCache.ContainsKey(typeList))
			{
				lock (GenericMethodCache)
				{
					if (!GenericMethodCache.ContainsKey(typeList))
					{
						GenericMethodCache[typeList] = GenericMethod.MakeGenericMethod(typeList.Types);
					}
				}
			}

			var method = GenericMethodCache[typeList];
			return method.FastInvoke(instance, parameters);
		}
	}
}
