using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace GenericWrappers
{
	/// <summary>
	/// Generates non-generic wrapper for interface with generic methods
	/// </summary>
	public class GenericInterfaceWrapper
	{
		const string AssemblyName = "GenericInterfaceWrapperAssembly";
		const string ModuleName = "Interfaces";

		static GenericInterfaceWrapper()
		{
			WrappedTypeCache = new Dictionary<Type,Type>();
			DynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
				new AssemblyName(AssemblyName), AssemblyBuilderAccess.RunAndSave);
			DynamicModule = DynamicAssembly.DefineDynamicModule(ModuleName);
		}

		static Dictionary<Type, Type> WrappedTypeCache { get; set; }

		static AssemblyBuilder DynamicAssembly { get; set; }

		static ModuleBuilder DynamicModule { get; set; }

		/// <summary>
		/// Returns wrapped name for a method
		/// </summary>
		public static string GetWrappedName(MethodInfo mi)
		{
			if (!mi.IsGenericMethod)
			{
				return mi.Name;
			}

			// "SomeMethod"<T, X> -> "SomeMethod<T, X>"
			var sb = new StringBuilder();
			foreach (var genPar in mi.GetGenericArguments())
			{
				if (sb.Length > 0)
					sb.Append(", ");
				sb.Append(genPar.Name);
			}

			sb.Insert(0, mi.Name + "<");
			sb.Append(">");
			return sb.ToString();
		}

		public static Type Wrap(Type source)
		{
			if (!WrappedTypeCache.ContainsKey(source))
			{
				lock (WrappedTypeCache)
				{
					if (!WrappedTypeCache.ContainsKey(source))
					{
						WrappedTypeCache[source] = CreateWrappedInterface(source);
					}
				}
			}

			return WrappedTypeCache[source];
		}

		private static Type CreateWrappedInterface(Type source)
		{
			var type = DynamicModule.DefineType(source.FullName, TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Public);
			CreateMembers(source, type);
			return type.CreateType();
		}

		private static void CreateMembers(Type source, TypeBuilder dest)
		{
			// copy methods
			foreach (var mi in source.GetMethods())
			{
				// skip events and properties
				if (mi.IsSpecialName)
				{
					continue;
				}

				if (!mi.IsGenericMethod)
				{
					CreateMethodCopy(dest, mi);
					continue;
				}

				CreateWrappedMethod(dest, mi);
			}

			foreach (var pi in source.GetProperties())
			{
				CreateProperty(dest, pi);
			}

			foreach (var ev in source.GetEvents())
			{
				CreateEvent(dest, ev);
			}
		}

		private static EventBuilder CreateEvent(TypeBuilder dest, EventInfo ev)
		{
			MethodBuilder add = null, remove = null;

			var mi = ev.GetAddMethod();
			if (mi != null)
			{
				add = CreateMethodCopy(dest, mi);
			}

			mi = ev.GetRemoveMethod();
			if (mi != null)
			{
				remove = CreateMethodCopy(dest, mi);
			}

			var evt = dest.DefineEvent(ev.Name, ev.Attributes, ev.EventHandlerType);
			if (add != null)
			{
				evt.SetAddOnMethod(add);
			}

			if (remove != null)
			{
				evt.SetRemoveOnMethod(remove);
			}

			return evt;
		}

		private static PropertyBuilder CreateProperty(TypeBuilder dest, PropertyInfo pi)
		{
			MethodBuilder get = null, set = null;

			var mi = pi.GetGetMethod();
			if (mi != null)
			{
				get = CreateMethodCopy(dest, mi);
			}

			mi = pi.GetSetMethod();
			if (mi != null)
			{
				set = CreateMethodCopy(dest, mi);
			}

			var prop = dest.DefineProperty(pi.Name, pi.Attributes, pi.PropertyType, new Type[0]);
			if (get != null)
			{
				prop.SetGetMethod(get);
			}

			if (set != null)
			{
				prop.SetSetMethod(set);
			}

			return prop;
		}

		private static MethodBuilder CreateMethodCopy(TypeBuilder dest, MethodInfo method)
		{
			// non-generic methods are copied as is
			var parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
			return dest.DefineMethod(method.Name, method.Attributes, method.CallingConvention, method.ReturnType, parameters);
		}

		private static MethodBuilder CreateWrappedMethod(TypeBuilder dest, MethodInfo method)
		{
			// replace generic arguments with Type parameters
			var newName = GetWrappedName(method);
			var parameters = method.GetGenericArguments()
				.Select(genericArgument => typeof(Type))
				.Concat(method.GetParameters().Select(p => p.ParameterType.IsGenericParameter ? typeof(object) : p.ParameterType))
				.ToArray();

			// replace generic return type with object
			var returnType = method.ReturnType.IsGenericParameter ? typeof(object) : method.ReturnType;
			return dest.DefineMethod(newName, method.Attributes, method.CallingConvention, returnType, parameters);
		}
	}
}
