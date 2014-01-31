using System;
using System.Collections.Generic;

namespace Zyan.Communication
{
	/// <summary>
	/// Component catalog shorter method overloads.
	/// </summary>
	public static class IComponentCatalogExtensions
	{
		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog)
		{
			catalog.RegisterComponent<I, T>(string.Empty, ActivationType.SingleCall, null);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog, Action<object> cleanUpHandler)
		{
			catalog.RegisterComponent<I, T>(string.Empty, ActivationType.SingleCall, cleanUpHandler);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="uniqueName">Unique component name</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog, string uniqueName)
		{
			catalog.RegisterComponent<I, T>(uniqueName, ActivationType.SingleCall, null);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog, string uniqueName, Action<object> cleanUpHandler)
		{
			catalog.RegisterComponent<I, T>(uniqueName, ActivationType.SingleCall, cleanUpHandler);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog, ActivationType activationType)
		{
			catalog.RegisterComponent<I, T>(string.Empty, activationType, null);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog, ActivationType activationType, Action<object> cleanUpHandler)
		{
			catalog.RegisterComponent<I, T>(string.Empty, activationType, cleanUpHandler);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog, string uniqueName, ActivationType activationType)
		{
			catalog.RegisterComponent<I, T>(uniqueName, activationType, null);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		public static void RegisterComponent<I>(this IComponentCatalog catalog, Func<object> factoryMethod)
		{
			catalog.RegisterComponent<I>(string.Empty, factoryMethod, ActivationType.SingleCall, null);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public static void RegisterComponent<I>(this IComponentCatalog catalog, Func<object> factoryMethod, Action<object> cleanUpHandler)
		{
			catalog.RegisterComponent<I>(string.Empty, factoryMethod, ActivationType.SingleCall, cleanUpHandler);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		public static void RegisterComponent<I>(this IComponentCatalog catalog, string uniqueName, Func<object> factoryMethod)
		{
			catalog.RegisterComponent<I>(uniqueName, factoryMethod, ActivationType.SingleCall, null);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public static void RegisterComponent<I>(this IComponentCatalog catalog, string uniqueName, Func<object> factoryMethod, Action<object> cleanUpHandler)
		{
			catalog.RegisterComponent<I>(uniqueName, factoryMethod, ActivationType.SingleCall, cleanUpHandler);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		public static void RegisterComponent<I>(this IComponentCatalog catalog, Func<object> factoryMethod, ActivationType activationType)
		{
			catalog.RegisterComponent<I>(string.Empty, factoryMethod, activationType, null);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public static void RegisterComponent<I>(this IComponentCatalog catalog, Func<object> factoryMethod, ActivationType activationType, Action<object> cleanUpHandler)
		{
			catalog.RegisterComponent<I>(string.Empty, factoryMethod, activationType, cleanUpHandler);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		public static void RegisterComponent<I>(this IComponentCatalog catalog, string uniqueName, Func<object> factoryMethod, ActivationType activationType)
		{
			catalog.RegisterComponent<I>(uniqueName, factoryMethod, activationType, null);
		}

		/// <summary>
		/// Registeres a component instance in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="instance">Component instance</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog, T instance)
		{
			catalog.RegisterComponent<I, T>(string.Empty, instance, null);
		}

		/// <summary>
		/// Registeres a component instance in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="instance">Component instance</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog, T instance, Action<object> cleanUpHandler)
		{
			catalog.RegisterComponent<I, T>(string.Empty, instance, cleanUpHandler);
		}

		/// <summary>
		/// Registeres a component instance in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="instance">Component instance</param>
		public static void RegisterComponent<I, T>(this IComponentCatalog catalog, string uniqueName, T instance)
		{
			catalog.RegisterComponent<I, T>(uniqueName, instance, null);
		}

		/// <summary>
		/// Gets registration data for a specified component by its interface type.
		/// </summary>
		/// <param name="catalog">IComponentCatalog instance</param>
		/// <param name="interfaceType">Interface type of the component</param>
		/// <returns>Component registration</returns>
		public static ComponentRegistration GetRegistration(this IComponentCatalog catalog, Type interfaceType)
		{
			if (interfaceType == null)
				throw new ArgumentNullException("interfaceType");

			return catalog.GetRegistration(interfaceType.FullName);
		}

		/// <summary>
		/// Deletes a component registration.
		/// </summary>
		/// <typeparam name="I">Interface type of the component to unregister</typeparam>
		/// <param name="catalog">IComponentCatalog instance</param>
		public static void UnregisterComponent<I>(this IComponentCatalog catalog)
		{
			string uniqueName = typeof(I).FullName;

			catalog.UnregisterComponent(uniqueName);
		}
	}
}
