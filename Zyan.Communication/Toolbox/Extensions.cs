using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Various extension methods
	/// TODO: Localize exceptions and trace messages
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Converts exception and all of its inner exceptions to string
		/// </summary>
		public static string Dump(this Exception ex, string title)
		{
			var sb = new StringBuilder(title);

			while (ex != null)
			{
				sb.AppendFormat("{0} ({1})", ex.Message, ex.GetType());
				sb.AppendLine();
				sb.AppendLine("Stack trace:");
				sb.AppendLine(ex.StackTrace);

				ex = ex.InnerException;
				if (ex != null)
				{
					sb.AppendLine();
					sb.Append("Inner exception: ");
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Returns true if method in one-way
		/// </summary>
		public static bool IsOneWay(this MethodInfo mi)
		{
			// check for OneWay attribute
			if (!RemotingServices.IsOneWay(mi))
			{
				return false;
			}

			// return type should be void
			if (mi.ReturnType != typeof(void))
			{
				Trace.WriteLine("Warning: non-void method is decorated with OneWay attribute: " + mi);
				return false;
			}

			// ref and out parameters are not allowed
			foreach (var arg in mi.GetParameters())
			{
				if (arg.IsOut || arg.ParameterType.IsByRef)
				{
					Trace.WriteLine("Warning: ret and out parameters are not allowed in OneWay method: " + mi);
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Invokes the method represented by current instance using the supplied parameters in either one-way or normal mode
		/// </summary>
		public static object Invoke(this MethodInfo mi, object instance, object[] args, bool oneWay)
		{
			if (oneWay)
			{
				ThreadPool.QueueUserWorkItem(t =>
				{
					try
					{
						// one-way method cannot throw any exceptions, so we ignore any
						mi.Invoke(instance, args);
					}
					catch (Exception ex)
					{
						Trace.WriteLine(ex.Dump("Unhandled exception in one-way method: "));
					}
				});

				return null;
			}

			return mi.Invoke(instance, args);
		}

		/// <summary>
		/// Creates invocation delegate for the method represented by given MethodInfo
		/// </summary>
		public static T CreateDelegate<T>(this MethodInfo method) where T : class
		{
			return Delegate.CreateDelegate(typeof(T), method) as T;
		}

		/// <summary>
		/// Searches for the specified public method whose parameters match the specified argument types.
		/// </summary>
		/// <param name="type">Type to inspect</param>
		/// <param name="methodName">Method name</param>
		/// <param name="genericArguments">List of generic arguments</param>
		/// <param name="argumentTypes">Argument types</param>
		/// <returns>MethodInfo it method is found, otherwise, null</returns>
		public static MethodInfo GetMethod(this Type type, string methodName, Type[] genericArguments, Type[] argumentTypes)
		{
			// TODO:
			// - cache type.GetMethod() results
			// - cache created generic methods

			// search for ordinal method
			if (genericArguments == null || genericArguments.Length == 0)
			{
				return type.GetMethod(methodName, argumentTypes);
			}

			// search for generic method definition
			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
			foreach (var mi in methods)
			{
				if (mi.Name != methodName || !mi.IsGenericMethodDefinition)
					continue;

				var parameters = mi.GetParameters();
				if (mi.GetParameters().Length != argumentTypes.Length)
					continue;

				var genArgs = mi.GetGenericArguments();
				if (genArgs == null || genArgs.Length != genericArguments.Length)
					continue;

				// index generic arguments
				var dict = new Dictionary<Type, Type>();
				for (var i = 0; i < genArgs.Length; i++)
				{
					dict[genArgs[i]] = genericArguments[i];
				}

				// match parameter types
				bool argumentsMatch = true;
				for (var i = 0; i < parameters.Length; i++)
				{
					// convert generic argument into real type
					Type paramType;
					if (!dict.TryGetValue(parameters[i].ParameterType, out paramType))
						paramType = parameters[i].ParameterType;

					if (!paramType.IsAssignableFrom(argumentTypes[i]))
					{
						argumentsMatch = false;
						break;
					}
				}

				if (!argumentsMatch)
					continue;

				// create generic method
				return mi.MakeGenericMethod(genericArguments);
			}

			// nothing is found
			return null;
		}
	}
}
