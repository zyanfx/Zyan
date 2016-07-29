using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Zyan.Communication;
using Zyan.Communication.Toolbox;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Wraps component method returning IEnumerable{T} or IQueryable{T}.
	/// </summary>
	internal class ZyanMethodQueryHandler : IQueryHandler
	{
		public IComponentCatalog Catalog { get; private set; }

		public string ComponentName { get; private set; }

		public MethodInfo MethodInfo { get; private set; }

		public string MethodQueryHandlerName
		{
			get { return GetMethodQueryHandlerName(ComponentName, MethodInfo); }
		}

		public static string GetMethodQueryHandlerName(string componentName, MethodInfo method)
		{
			return componentName + "." + method.GetSignature();
		}

		static MethodInfo getMethodInfo = typeof(ZyanMethodQueryHandler).GetMethod("Get", new Type[0]);

		public ZyanMethodQueryHandler(IComponentCatalog catalog, string componentName, MethodInfo method)
		{
			Catalog = catalog;
			ComponentName = componentName;
			MethodInfo = method;
		}

		public IQueryable Get(Type type)
		{
			// create generic version of this method and invoke it
			var genericMethodInfo = getMethodInfo.MakeGenericMethod(type);
			var result = genericMethodInfo.Invoke(this, new object[] { });
			return (IQueryable)result;
		}

		public IQueryable<T> Get<T>() where T : class
		{
			// get component instance
			var registration = Catalog.GetRegistration(ComponentName);
			var instance = Catalog.GetComponentInstance(registration);
			var type = instance.GetType();

			try
			{
				// create generic method
				var genericMethodInfo = type.GetMethod(MethodInfo.Name, new[] { typeof(T) }, new Type[0]);
				if (genericMethodInfo == null)
				{
					var methodSignature = MessageHelpers.GetMethodSignature(type, MethodInfo.Name, new Type[0]);
					var exceptionMessage = String.Format(LanguageResource.MissingMethodException_MethodNotFound, methodSignature);
					throw new MissingMethodException(exceptionMessage);
				}

				// invoke Get<T> method and return IQueryable<T>
				object result = genericMethodInfo.Invoke(instance, new object[0]);
				if (result is IQueryable<T>)
					return result as IQueryable<T>;
				if (result is IEnumerable<T>)
					return (result as IEnumerable<T>).AsQueryable();
				return null;
			}
			finally
			{
				if (instance != null && registration.ActivationType == ActivationType.SingleCall)
				{
					Catalog.CleanUpComponentInstance(registration, instance);
					instance = null;
				}
			}
		}

		public bool StartSession()
		{
			return true; // TODO
		}

		public bool CloseSession()
		{
			return true; // TODO
		}
	}
}
