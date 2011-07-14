using System;
using System.Collections.Generic;
using Zyan.Communication.Toolbox;
using System.Linq;
using Zyan.InterLinq;
using System.Collections;

namespace Zyan.Communication
{
	/// <summary>
	/// Implements a component catalog.
	/// </summary>
	public class ComponentCatalog : IComponentCatalog, IDisposable
	{
		#region Constructors

		/// <summary>
		/// Creates a new instance of the ComponentCatalog class.
		/// </summary>
		public ComponentCatalog()
		{
		}

		/// <summary>
		/// Creates a new instance of the ComponentCatalog class.
		/// </summary>
		/// <param name="disposeWithHost">Must be "true", when this instance should be disposed together with its owning host</param>
		public ComponentCatalog(bool disposeWithHost) : this()
		{
			_disposeWithHost = disposeWithHost;
		}

		#endregion

		#region Register components

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public void RegisterComponent<I, T>(string uniqueName, ActivationType activationType, Action<object> cleanUpHandler)
		{
			Type interfaceType = typeof(I);
			Type implementationType = typeof(T);

			if (!interfaceType.IsInterface)
				throw new ArgumentException(LanguageResource.ArgumentException_TypeIsNotAInterface, "interfaceType");

			if (!implementationType.IsClass)
				throw new ArgumentException(LanguageResource.ArgumentException_TypeIsNotAClass, "interfaceType");

			new TypeComparer<I, T>().Validate();

			if (string.IsNullOrEmpty(uniqueName))
				uniqueName = interfaceType.FullName;

			if (!ComponentRegistry.ContainsKey(uniqueName))
			{
				ComponentRegistration registration = new ComponentRegistration(interfaceType, implementationType, uniqueName, activationType, cleanUpHandler);
				registration.DisposeWithCatalog = true;
				ComponentRegistry.Add(uniqueName, registration);
			}

			RegisterQueryableMethods<I>(uniqueName);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public void RegisterComponent<I>(string uniqueName, Func<object> factoryMethod, ActivationType activationType, Action<object> cleanUpHandler)
		{
			Type interfaceType = typeof(I);

			if (!interfaceType.IsInterface)
				throw new ApplicationException(LanguageResource.ArgumentException_TypeIsNotAInterface);

			if (factoryMethod == null)
				throw new ArgumentException(LanguageResource.ArgumentException_FactoryMethodDelegateMissing, "factoryMethod");

			if (string.IsNullOrEmpty(uniqueName))
				uniqueName = interfaceType.FullName;

			if (!ComponentRegistry.ContainsKey(uniqueName))
			{
				ComponentRegistration registration = new ComponentRegistration(interfaceType, factoryMethod, uniqueName, activationType, cleanUpHandler);
				registration.DisposeWithCatalog = true;
				ComponentRegistry.Add(uniqueName, registration);
			}

			RegisterQueryableMethods<I>(uniqueName);
		}

		/// <summary>
		/// Registeres a component instance in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="instance">Component instance</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public void RegisterComponent<I, T>(string uniqueName, T instance, Action<object> cleanUpHandler)
		{
			Type interfaceType = typeof(I);
			Type implementationType = typeof(T);

			if (!interfaceType.IsInterface)
				throw new ArgumentException(LanguageResource.ArgumentException_TypeIsNotAInterface, "interfaceType");

			if (!implementationType.IsClass)
				throw new ArgumentException(LanguageResource.ArgumentException_TypeIsNotAClass, "interfaceType");

			new TypeComparer<I, T>().Validate();

			if (string.IsNullOrEmpty(uniqueName))
				uniqueName = interfaceType.FullName;

			// do not dispose externally-owned component instance
			var externallyOwned = cleanUpHandler == null;

			if (!ComponentRegistry.ContainsKey(uniqueName))
			{
				ComponentRegistration registration = new ComponentRegistration(interfaceType, instance, uniqueName, cleanUpHandler);
				registration.DisposeWithCatalog = !externallyOwned;
				ComponentRegistry.Add(uniqueName, registration);
			}

			RegisterQueryableMethods<I>(uniqueName);
		}

		/// <summary>
		/// Wraps all component methods returning IEnumerable{T} or IQueryable{T}.
		/// </summary>
		/// <typeparam name="I">Interface to wrap.</typeparam>
		/// <param name="uniqueName">Component unique name.</param>
		private void RegisterQueryableMethods<I>(string uniqueName)
		{
			foreach (var mi in typeof(I).GetMethods())
			{
				if (mi.IsGenericMethod && mi.GetParameters().Length == 0)
				{
					if (typeof(IEnumerable).IsAssignableFrom(mi.ReturnType) || typeof(IQueryable).IsAssignableFrom(mi.ReturnType))
					{
						var queryHandler = new ZyanMethodQueryHandler(this, uniqueName, mi);
						var remoteHandler = new ZyanServerQueryHandler(queryHandler);
						RegisterComponent<IQueryRemoteHandler, ZyanServerQueryHandler>(queryHandler.MethodQueryHandlerName, remoteHandler, null);
					}
				}
			}
		}

		#endregion

		#region Unregister components

		/// <summary>
		/// Deletes a component registration.
		/// </summary>
		/// <param name="uniqueName">Unique component name</param>
		public void UnregisterComponent(string uniqueName)
		{
			if (ComponentRegistry.ContainsKey(uniqueName))
				ComponentRegistry.Remove(uniqueName);
		}

		#endregion

		#region Managing component registrations

		private Dictionary<string, ComponentRegistration> _componentRegistry = null;

		/// <summary>
		/// Gets registration data for a specified component by its interface name.
		/// </summary>
		/// <param name="interfaceName">Name of the component´s interface</param>
		/// <returns>Component registration</returns>
		public ComponentRegistration GetRegistration(string interfaceName)
		{
			if (!ComponentRegistry.ContainsKey(interfaceName))
				throw new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_CannotFindComponentForInterface, interfaceName));
			
			return ComponentRegistry[interfaceName];
		}

		/// <summary>
		/// Returns a name-value-list of all component registrations.
		/// <remarks>
		/// If the list doesn´t exist yet, it will be created automaticly.
		/// </remarks>
		/// </summary>
		internal Dictionary<string, ComponentRegistration> ComponentRegistry
		{
			get
			{
				if (_disposed)
					throw new ObjectDisposedException("_componentRegistry");
				
				if (_componentRegistry == null)
					_componentRegistry = new Dictionary<string, ComponentRegistration>();

				return _componentRegistry;
			}
		}

		/// <summary>
		/// Returns a list with information about all registered components.
		/// </summary>
		/// <returns>List with component information</returns>
		public List<ComponentInfo> GetRegisteredComponents()
		{
			List<ComponentInfo> result = new List<ComponentInfo>();

			foreach (ComponentRegistration registration in ComponentRegistry.Values)
			{
				result.Add(
					new ComponentInfo()
					{
						InterfaceName = registration.InterfaceType.FullName,
						UniqueName = registration.UniqueName,
						ActivationType = registration.ActivationType
					});
			}
			return result;
		}
		
		/// <summary>
		/// Returns an instance of a specified registered component.
		/// </summary>
		/// <param name="registration">Component registration</param>
		/// <returns>Component instance</returns>
		public object GetComponentInstance(ComponentRegistration registration)
		{
			if (registration == null)
				throw new ArgumentNullException("registration");

			switch (registration.ActivationType)
			{ 
				case ActivationType.SingleCall:
					
					if (registration.InitializationHandler != null)
						return registration.InitializationHandler();
					else
						return Activator.CreateInstance(registration.ImplementationType);

				case ActivationType.Singleton:

					if (registration.SingletonInstance == null)
					{
						lock (registration.SyncLock)
						{ 
							if (registration.SingletonInstance == null)
							{
								if (registration.InitializationHandler!=null)
									registration.SingletonInstance = registration.InitializationHandler();
								else
									registration.SingletonInstance = Activator.CreateInstance(registration.ImplementationType);
							}
						}
					}
					return registration.SingletonInstance;
			}
			throw new NullReferenceException();
		}

		/// <summary>
		/// Returns an instance of a specified registered component.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <returns>Component instance</returns>
		public I GetComponent<I>()
		{
			string interfaceName=typeof(I).FullName;

			return (I)GetComponentInstance(ComponentRegistry[interfaceName]);
		}

		/// <summary>
		/// Returns an instance of a specified registered component.
		/// </summary>
		/// <param name="uniqueName">Unique component name</param>
		/// <returns>Component instance</returns>
		public object GetComponent(string uniqueName)
		{
			return GetComponentInstance(ComponentRegistry[uniqueName]);
		}
		
		#endregion

		#region Cleanup

		private bool _disposeWithHost = false;
		private bool _disposed = false;

		/// <summary>
		/// Gets, if this component catalog should be automaticly disposed, when the owning host is disposed.
		/// </summary>
		public bool DisposeWithHost
		{
			get { return _disposeWithHost; }
		}
				
		/// <summary>
		/// Releases all managed resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(false);
		}

		/// <summary>
		/// Releases all managed resources.
		/// </summary>
		/// <param name="calledFromFinalizer">Specifies if this method is called from finalizer or not</param>
		private void Dispose(bool calledFromFinalizer)
		{
			if (!_disposed)
			{
				_disposed = true;
				
				if (_componentRegistry != null)
				{
					foreach (ComponentRegistration regEntry in _componentRegistry.Values)
					{
						CleanUpComponentInstance(regEntry);
					}
					_componentRegistry.Clear();
					_componentRegistry = null;
				}
				if (!calledFromFinalizer)
					GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Is called from runtime when this object is finalized.
		/// </summary>
		~ComponentCatalog()
		{
			Dispose(true);
		}

		/// <summary>
		/// Processes resource clean up logic for a specified registered Singleton activated component.
		/// </summary>
		/// <param name="regEntry">Component registration</param>
		public void CleanUpComponentInstance(ComponentRegistration regEntry)
		{
			if (regEntry == null)
				throw new ArgumentNullException("regEntry");

			CleanUpComponentInstance(regEntry, regEntry.SingletonInstance);
		}

		/// <summary>
		/// Processes resource clean up logic for a specified registered component.
		/// </summary>
		/// <param name="regEntry">Component registration</param>
		/// <param name="instance">Component instance to clean up</param>
		public void CleanUpComponentInstance(ComponentRegistration regEntry, object instance)
		{
			if (regEntry == null)
				throw new ArgumentNullException("regEntry");

			lock (regEntry.SyncLock)
			{
				if (instance == null)
					return;

				if (regEntry.DisposeWithCatalog)
				{
					if (regEntry.CleanUpHandler != null)
					{
						regEntry.CleanUpHandler(instance);
						return;
					}

					var disposableComponent = instance as IDisposable;

					if (disposableComponent != null)
						disposableComponent.Dispose();
				}
			}
		}

		#endregion
	}
}
