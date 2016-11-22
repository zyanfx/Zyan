using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication
{
	/// <summary>
	/// The default implementation of the <see cref="IComponentRegistry"/> based on <see cref="ConcurrentDictionary{TKey, TValue}"/>.
	/// </summary>
	/// <seealso cref="Zyan.Communication.IComponentRegistry" />
	public class ComponentRegistry : IComponentRegistry
	{
		private ConcurrentDictionary<string, ComponentRegistration> components = new ConcurrentDictionary<string, ComponentRegistration>();

		/// <summary>
		/// Gets the <see cref="ComponentRegistration"/> with the specified component name.
		/// </summary>
		/// <param name="componentName">Name of the component.</param>
		public ComponentRegistration this[string componentName]
		{
			get
			{
				return components[componentName];
			}
		}

		/// <summary>
		/// Clears the list of registrations.
		/// </summary>
		public void Clear()
		{
			components.Clear();
		}

		/// <summary>
		/// Gets all component registrations.
		/// </summary>
		public IEnumerable<ComponentRegistration> GetRegistrations()
		{
			return components.Values;
		}

		/// <summary>
		/// Determines whether the specified component is registered.
		/// </summary>
		/// <param name="componentName">The name of the component.</param>
		public bool IsRegistered(string componentName)
		{
			return components.ContainsKey(componentName);
		}

		/// <summary>
		/// Registers the specified component name.
		/// </summary>
		/// <param name="componentName">Name of the component.</param>
		/// <param name="registration">The registration.</param>
		/// <param name="ignoreUpdates">If set to true, then duplicate registrations are ignored rather than updated.</param>
		public void Register(string componentName, ComponentRegistration registration, bool ignoreUpdates)
		{
			if (ignoreUpdates)
			{
				components.TryAdd(componentName, registration);
				return;
			}

			components[componentName] = registration;
		}

		/// <summary>
		/// Removes the specified component.
		/// </summary>
		/// <param name="componentName">Name of the component.</param>
		public void Remove(string componentName)
		{
			ComponentRegistration _;
			components.TryRemove(componentName, out _);
		}

		/// <summary>
		/// Tries to get the component registration.
		/// </summary>
		/// <param name="componentName">Name of the component.</param>
		/// <param name="registration">The registration.</param>
		public bool TryGetRegistration(string componentName, out ComponentRegistration registration)
		{
			return components.TryGetValue(componentName, out registration);
		}
	}
}
