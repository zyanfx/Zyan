using System.Collections.Generic;

namespace Zyan.Communication
{
	/// <summary>
	/// Simple dictionary to handle registrations for the <see cref="ComponentCatalog"/>.
	/// </summary>
	public interface IComponentRegistry
	{
		/// <summary>
		/// Determines whether the specified component is registered.
		/// </summary>
		/// <param name="componentName">The name of the component.</param>
		bool IsRegistered(string componentName);

		/// <summary>
		/// Registers the specified component name.
		/// </summary>
		/// <param name="componentName">Name of the component.</param>
		/// <param name="registration">The registration.</param>
		/// <param name="ignoreUpdates">If set to true, then duplicate registrations are ignored rather than updated.</param>
		void Register(string componentName, ComponentRegistration registration, bool ignoreUpdates);

		/// <summary>
		/// Removes the specified component name.
		/// </summary>
		/// <param name="componentName">Name of the component.</param>
		void Remove(string componentName);

		/// <summary>
		/// Gets the <see cref="ComponentRegistration"/> with the specified component name.
		/// </summary>
		/// <param name="componentName">Name of the component.</param>
		ComponentRegistration this[string componentName] { get; }

		/// <summary>
		/// Tries to get the component registration.
		/// </summary>
		/// <param name="componentName">Name of the component.</param>
		/// <param name="registration">The registration.</param>
		bool TryGetRegistration(string componentName, out ComponentRegistration registration);

		/// <summary>
		/// Gets all component registrations.
		/// </summary>
		IEnumerable<ComponentRegistration> GetRegistrations();

		/// <summary>
		/// Clears the list of registrations.
		/// </summary>
		void Clear();
	}
}
