using System;
using System.Collections.Generic;
using System.Linq;
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

		private class CurrentQuerySession
		{
			public ComponentRegistration Registration { get; set; }

			public object Instance { get; set; }
		}

		[ThreadStatic]
		private static CurrentQuerySession querySession;

		public bool StartSession()
		{
			// get component instance and save to the current session
			var registration = Catalog.GetRegistration(ComponentName);
			querySession = new CurrentQuerySession
			{
				Registration = registration,
				Instance = Catalog.GetComponentInstance(registration),
			};

			return true;
		}

		public bool CloseSession()
		{
			// cleanup component instance and release the current session
			if (querySession != null)
			{
				var instance = querySession.Instance;
				var registration = querySession.Registration;
				if (instance != null && registration != null && registration.ActivationType == ActivationType.SingleCall)
				{
					Catalog.CleanUpComponentInstance(registration, instance);
					querySession.Instance = null;
				}

				querySession = null;
			}

			return true;
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
			if (querySession == null)
			{
				throw new InvalidOperationException("Session is not started. ZyanMethodQueryHandler requires that StartSession method is called before Get<T>.");
			}

			// get component instance created by
			var instance = querySession.Instance;
			var instanceType = instance.GetType();

			// create generic method
			var genericMethodInfo = instanceType.GetMethod(MethodInfo.Name, new[] { typeof(T) }, new Type[0]);
			if (genericMethodInfo == null)
			{
				var methodSignature = MessageHelpers.GetMethodSignature(instanceType, MethodInfo.Name, new Type[0]);
				var exceptionMessage = string.Format(LanguageResource.MissingMethodException_MethodNotFound, methodSignature);
				throw new MissingMethodException(exceptionMessage);
			}

			// invoke Get<T> method and return IQueryable<T>
			object result = genericMethodInfo.Invoke(instance, new object[0]);
			if (result is IQueryable<T>)
			{
				return result as IQueryable<T>;
			}

			if (result is IEnumerable<T>)
			{
				return (result as IEnumerable<T>).AsQueryable();
			}

			return null;
		}
	}
}
