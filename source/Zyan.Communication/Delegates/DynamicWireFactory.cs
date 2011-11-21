using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Delegates
{
	#region Synonym definitions

	// Factory method for dynamic wires.
	using DynamicWireFactoryMethod = Func<bool, DynamicWireBase>;

#if !FX3
	using DynamicWireFactoryCache = System.Collections.Concurrent.ConcurrentDictionary<string, Func<bool, DynamicWireBase>>;
#else
	using DynamicWireFactoryCache = Zyan.Communication.Toolbox.ConcurrentDictionary<string, Func<bool, DynamicWireBase>>;
#endif

	#endregion

	/// <summary>
	/// Factory class for creation of dynamic wires.
	/// </summary>
	internal sealed class DynamicWireFactory
	{
		#region Singleton implementation

		/// <summary>
		/// This constructor is private, so DynamicWireFactory class cannot be created from the outside.
		/// </summary>
		private DynamicWireFactory()
		{
		}

		/// <summary>
		/// Lazy-initialized singleton instance.
		/// </summary>
		private static readonly Lazy<DynamicWireFactory> Instance =
			new Lazy<DynamicWireFactory>(() => new DynamicWireFactory(), true);

		#endregion

		#region Wiring

		// Cache for created dynamic wire factories
		DynamicWireFactoryCache _wireFactoryCache = new DynamicWireFactoryCache();

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
			return _wireFactoryCache.GetOrAdd(key, x =>
			{
				// create wire factory method
				var methodInfo = CreateDynamicWireGenericMethodInfo.MakeGenericMethod(delegateType);
				var dynamicWireFactoryMethod = methodInfo.CreateDelegate<DynamicWireFactoryMethod>(this);
				return dynamicWireFactoryMethod;
			});
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

		/// <summary>
		/// Creates a dynamic wire for a specified event or delegate property of a component.
		/// </summary>
		/// <param name="componentType">Component type.</param>
		/// <param name="delegateMemberName">Event name or name of the delegate property.</param>
		/// <param name="isEvent">Sets if the member is a event (if false, the memeber must be a delegate property).</param>
		/// <returns>Instance of the created dynamic wire type (ready to use).</returns>
		public static DynamicWireBase CreateDynamicWire(Type componentType, string delegateMemberName, bool isEvent)
		{
			return Instance.Value.CreateWire(componentType, delegateMemberName, isEvent);
		}

		/// <summary>
		/// Creates a dynamic wire for a specified event or delegate property of a component.
		/// </summary>
		/// <param name="componentType">Component type.</param>
		/// <param name="delegateType">Type of the delegate.</param>
		/// <returns>Instance of the created dynamic wire type (ready to use).</returns>
		public static DynamicWireBase CreateDynamicWire(Type componentType, Type delegateType)
		{
			return Instance.Value.CreateWire(componentType, delegateType);
		}
	}
}
