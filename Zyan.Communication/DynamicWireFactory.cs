using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication
{
	// Factory method for dynamic wires.
	using DynamicWireFactoryMethod = Func<bool, DynamicWireBase>;

	/// <summary>
	/// Factory class for creation of dynamic wires.
	/// </summary>
	internal sealed class DynamicWireFactory
	{
		#region Singleton implementation

		// Locking object
		private static object _singletonLockObject = new object();

		// Singleton instance
		private static volatile DynamicWireFactory _singleton = null;

		/// <summary>
		/// Gets a singleton instance of the DynamicWirefactory class.
		/// </summary>
		public static DynamicWireFactory Instance
		{
			get
			{
				if (_singleton == null)
				{
					lock (_singletonLockObject)
					{
						if (_singleton == null)
							_singleton = new DynamicWireFactory();
					}
				}

				return _singleton;
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a new instance of the DynamicWireFactory class.
		/// </summary>
		private DynamicWireFactory()
		{
			_wireFactoryCache = new Dictionary<string, DynamicWireFactoryMethod>();
		}

		#endregion

		#region Wire Factory Cache

		// Cache for created dynamic wire factories
		private Dictionary<string, DynamicWireFactoryMethod> _wireFactoryCache = null;

		// Locking object for thread sync of the wire factory cache
		private object _wireFactoryCacheLockObject = new object();

		/// <summary>
		/// Creates a unique wire cache key for a delegate wire factory.
		/// </summary>
		/// <param name="componentType">Type of server component.</param>
		/// <param name="delegateType">Delegate type of the wire.</param>
		/// <param name="isEvent">Sets if the wire type is for a event (if false, the wire type must be of a delegate property).</param>
		/// <returns>Unique key.</returns>
		private string CreateKeyForWire(Type componentType, Type delegateType, bool isEvent)
		{
			var wireKeyBuilder = new StringBuilder();
			wireKeyBuilder.Append(isEvent ? "E|" : "D|");
			wireKeyBuilder.Append(componentType.FullName);
			wireKeyBuilder.Append("|");
			wireKeyBuilder.Append(delegateType.FullName);

			return wireKeyBuilder.ToString();
		}

		#endregion

		#region Wiring

		/// <summary>
		/// Creates a dynamic wire for a specified event or delegate property of a component.
		/// </summary>
		/// <param name="componentType">Component type</param>
		/// <param name="eventMemberName">Event name or name of the delegate property</param>
		/// <param name="isEvent">Sets if the member is a event (if false, the memeber must be a delegate property)</param>
		/// <returns>Instance of the created dynamic wire type (ready to use)</returns>
		public static DynamicWireBase CreateDynamicWire(Type componentType, string delegateMemberName, bool isEvent)
		{
			return Instance.CreateWire(componentType, delegateMemberName, isEvent);
		}

		/// <summary>
		/// Creates a dynamic wire for a specified event or delegate property of a component.
		/// </summary>
		/// <param name="componentType">Component type</param>
		/// <param name="delegateType">Type of the delegate</param>
		/// <returns>Instance of the created dynamic wire type (ready to use)</returns>
		public static DynamicWireBase CreateDynamicWire(Type componentType, Type delegateType)
		{
			return Instance.CreateWire(componentType, delegateType);
		}

		private DynamicWireBase CreateWire(Type componentType, string delegateMemberName, bool isEvent)
		{
			if (componentType == null)
				throw new ArgumentNullException("componentType");

			if (string.IsNullOrEmpty(delegateMemberName))
				throw new ArgumentException(LanguageResource.ArgumentException_OutPutPinNameMissing, "delegateMemberName");

			var delegateType = GetDelegateType(componentType, delegateMemberName, isEvent);
			var createDynamicWire = GetOrCreateFactoryMethod(componentType, delegateType, isEvent);
			return createDynamicWire(isEvent);
		}

		private DynamicWireBase CreateWire(Type componentType, Type delegateType)
		{
			var createDynamicWire = GetOrCreateFactoryMethod(componentType, delegateType, false);
			return createDynamicWire(false);
		}

		private DynamicWireFactoryMethod GetOrCreateFactoryMethod(Type componentType, Type delegateType, bool isEvent)
		{
			if (componentType == null)
				throw new ArgumentNullException("componentType");

			if (delegateType == null)
				throw new ArgumentNullException("delegateType");

			// look for cached factory method value
			var key = CreateKeyForWire(componentType, delegateType, isEvent);
			if (!_wireFactoryCache.ContainsKey(key))
			{
				lock (_wireFactoryCacheLockObject)
				{
					if (!_wireFactoryCache.ContainsKey(key))
					{
						// create wire factory method and save to cache
						var methodInfo = CreateDynamicWireGenericMethodInfo.MakeGenericMethod(delegateType);
						var dynamicWireFactoryMethod = methodInfo.CreateDelegate<Func<bool, DynamicWireBase>>(this);
						_wireFactoryCache[key] = dynamicWireFactoryMethod;
						return dynamicWireFactoryMethod;
					}
				}
			}

			// return cached value
			return _wireFactoryCache[key];
		}

		/// <summary>
		/// <see cref="MethodInfo"/> for <see cref="CreateDynamicWire{T}"/> method.
		/// </summary>
		private static MethodInfo CreateDynamicWireGenericMethodInfo = typeof(DynamicWireFactory).GetMethod("CreateDynamicWire",
			BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);

		/// <summary>
		/// Creates strongly-typed dynamic wire for event handler or delegate.
		/// </summary>
		/// <typeparam name="T">Delegate type.</typeparam>
		/// <param name="isEvent">True if delegate is an event handler.</param>
		/// <returns>Dynamic wire instance.</returns>
		private DynamicWireBase CreateDynamicWire<T>(bool isEvent)
		{
			return isEvent ? new DynamicEventWire<T>() as DynamicWireBase : new DynamicWire<T>();
		}

		/// <summary>
		/// Gets the delegate type of a component´s event or delegate property.
		/// </summary>
		/// <param name="componentType">Component type</param>
		/// <param name="delegateMemberName">Event name or name of the delegate property</param>
		/// <param name="isEvent">Sets if the member is a event (if false, the memeber must be a delegate property)</param>
		/// <returns>Delegate type</returns>
		private Type GetDelegateType(Type componentType, string delegateMemberName, bool isEvent)
		{
			if (isEvent)
			{
				var eventInfo = componentType.GetEvent(delegateMemberName);
				return eventInfo.EventHandlerType;
			}

			var delegatePropInfo = componentType.GetProperty(delegateMemberName);
			return delegatePropInfo.PropertyType;
		}

		#endregion
	}
}
