using System;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Collections.Generic;

// TODO: Localize exceptions and trace messages
namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Various extension methods.
	/// </summary>
	public static partial class Extensions
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
				// save thread-static CurrentSession variable
				var savedSession = ServerSession.CurrentSession;

				ThreadPool.QueueUserWorkItem(t =>
				{
					// restore current session in a new worker thread
					ServerSession.CurrentSession = savedSession;

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
		/// Creates invocation delegate for the static method represented by the given MethodInfo.
		/// </summary>
		public static T CreateDelegate<T>(this MethodInfo method) where T : class
		{
			return Delegate.CreateDelegate(typeof(T), method) as T;
		}

		/// <summary>
		/// Creates invocation delegate for the non-static method represented by the given MethodInfo.
		/// </summary>
		public static T CreateDelegate<T>(this MethodInfo method, object instance) where T : class
		{
			return Delegate.CreateDelegate(typeof(T), instance, method) as T;
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

		private static Dictionary<Type, object> defaultValues = new Dictionary<Type, object>();

		/// <summary>
		/// Gets the default value for the given type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>default() for the type.</returns>
		public static object GetDefaultValue(this Type type)
		{
			if (type == typeof(void) || !type.IsValueType)
			{
				return null;
			}

			var result = default(object);
			lock (defaultValues)
			{
				if (!defaultValues.TryGetValue(type, out result))
				{
					result = Activator.CreateInstance(type);
					defaultValues[type] = result;
				}
			}

			return result;
		}

		/// <summary>
		/// Returns method signature, similar to MethodInfo.ToString().
		/// </summary>
		/// <param name="mi">MethodInfo to convert.</param>
		/// <returns>String representation of method signature, equal on .NET and Mono platforms.</returns>
		public static string GetSignature(this MethodInfo mi)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("{0} {1}{2}(", mi.ReturnType.Name, mi.Name,
				mi.IsGenericMethod ? "`" + mi.GetGenericArguments().Length : "");

			bool first = true;
			foreach (var pt in mi.GetParameters().Select(pi => pi.ParameterType))
			{
				if (!first)
					sb.Append(", ");
				sb.Append(pt.FullName);
				first = false;
			}

			sb.Append(")");
			return sb.ToString();
		}
	}
}
