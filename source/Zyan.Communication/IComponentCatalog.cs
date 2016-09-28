using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication
{
	/// <summary>
	/// Component catalog interface
	/// </summary>
	public interface IComponentCatalog
	{
		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		void RegisterComponent<I, T>(string uniqueName = "", ActivationType activationType = ActivationType.SingleCall, Action<object> cleanUpHandler = null);

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		void RegisterComponent<I>(string uniqueName, Func<object> factoryMethod, ActivationType activationType = ActivationType.SingleCall, Action<object> cleanUpHandler = null);

		/// <summary>
		/// Registeres a component instance in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="instance">Component instance</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		void RegisterComponent<I, T>(string uniqueName, T instance, Action<object> cleanUpHandler = null);

		/// <summary>
		/// Deletes a component registration.
		/// </summary>
		/// <param name="uniqueName">Unique component name</param>
		void UnregisterComponent(string uniqueName);

		/// <summary>
		/// Determines whether the specified interface name is registered.
		/// </summary>
		/// <param name="interfaceName">Name of the interface.</param>
		bool IsRegistered(string interfaceName);

		/// <summary>
		/// Gets registration data for a specified component by its interface name.
		/// </summary>
		/// <param name="interfaceName">Name of the component´s interface</param>
		/// <returns>Component registration</returns>
		ComponentRegistration GetRegistration(string interfaceName);

		/// <summary>
		/// Returns a list with information about all registered components.
		/// </summary>
		/// <returns>List with component information</returns>
		List<ComponentInfo> GetRegisteredComponents();

		/// <summary>
		/// Returns an instance of a specified registered component.
		/// </summary>
		/// <param name="registration">Component registration</param>
		/// <returns>Component instance</returns>
		object GetComponentInstance(ComponentRegistration registration);

		/// <summary>
		/// Processes resource clean up logic for a specified registered Singleton activated component.
		/// </summary>
		/// <param name="regEntry">Component registration</param>
		void CleanUpComponentInstance(ComponentRegistration regEntry);

		/// <summary>
		/// Processes resource clean up logic for a specified registered component.
		/// </summary>
		/// <param name="regEntry">Component registration</param>
		/// <param name="instance">Component instance to clean up</param>
		void CleanUpComponentInstance(ComponentRegistration regEntry, object instance);
	}
}
