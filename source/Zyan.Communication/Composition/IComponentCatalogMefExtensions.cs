using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Composition
{
	// Shortcut name for registration method delegate
	using ComponentRegistrationDelegate = Action<IComponentCatalog, ComposablePartCatalog, CompositionContainer, string>;

	/// <summary>
	/// Server-side MEF integration
	/// </summary>
	public static class IComponentCatalogMefExtensions
	{
		/// <summary>
		/// Registers all components in a MEF catalog attributed by "ComponentInterface" metadata tag.
		/// </summary>
		public static void RegisterComponents(this IComponentCatalog host, ComposablePartCatalog rootCatalog)
		{
			var parentContainer = new CompositionContainer(rootCatalog);
			var nonSharedCatalog = new NonSharedPartsCatalog(rootCatalog);

			// register exported components
			foreach (var part in rootCatalog.Parts)
			{
				host.RegisterComponents(nonSharedCatalog, parentContainer, part);
			}
		}

		/// <summary>
		/// Registers all components in a MEF container attributed by "ComponentInterface" metadata tag.
		/// </summary>
		public static void RegisterComponents(this IComponentCatalog host, CompositionContainer container)
		{
			var rootCatalog = container.Catalog;
			var nonSharedCatalog = new NonSharedPartsCatalog(rootCatalog);

			// register exported components
			foreach (var part in container.Catalog.Parts)
			{
				host.RegisterComponents(nonSharedCatalog, container, part);
			}
		}

		/// <summary>
		/// Registers exports provided by ComposablePartDefinition as components.
		/// </summary>
		static void RegisterComponents(this IComponentCatalog host, ComposablePartCatalog childCatalog, CompositionContainer parentContainer, ComposablePartDefinition partDefinition)
		{
			foreach (var export in partDefinition.ExportDefinitions.Where(def => def.IsZyanComponent()))
			{
				var type = export.Metadata[ZyanComponentAttribute.ComponentInterfaceKeyName] as Type;
				if (type != null)
				{
					host.RegisterComponent(childCatalog, parentContainer, type, export.ContractName);
				}
			}
		}

		/// <summary>
		/// MethodInfo for the following method (used to create generic method delegates)
		/// </summary>
		static MethodInfo RegisterComponentMethodInfo = typeof(IComponentCatalogMefExtensions).GetMethod("RegisterComponent", BindingFlags.Static | BindingFlags.NonPublic,
			null, new[] { typeof(IComponentCatalog), typeof(ComposablePartCatalog), typeof(CompositionContainer), typeof(string) }, null);

		/// <summary>
		/// Registers component of type T. Component instance is provided by MEF container.
		/// </summary>
		static void RegisterComponent<T>(this IComponentCatalog host, ComposablePartCatalog childCatalog, CompositionContainer parentContainer, string contractName = null)
		{
			// component -> owning container
			var containers = new ConcurrentDictionary<object, CompositionContainer>();
			var uniqueName = contractName ?? typeof(T).FullName;

			// component instance is created inside child container
			host.RegisterComponent<T>
			(
				uniqueName,
				delegate // factory method
				{
					// create child container for early component disposal
					var childContainer = new CompositionContainer(childCatalog, parentContainer);
					var component = contractName != null ? childContainer.GetExport<T>(contractName).Value : childContainer.GetExport<T>().Value;
					containers[component] = childContainer;
					return component;
				},
				delegate (object component) // cleanup delegate
				{
					CompositionContainer childContainer;

					// free child container and release non-shared MEF component
					if (containers.TryRemove(component, out childContainer))
					{
						childContainer.Dispose();
					}
				}
			);
		}

		/// <summary>
		/// Registers component of the given type. Component instance is provided by MEF container.
		/// </summary>
		static void RegisterComponent(this IComponentCatalog host, ComposablePartCatalog childCatalog, CompositionContainer parentContainer, Type type, string contractName = null)
		{
			var method = RegisterComponentMethodInfo.MakeGenericMethod(type);
			var registerComponent = method.CreateDelegate<ComponentRegistrationDelegate>();
			registerComponent(host, childCatalog, parentContainer, contractName);
		}
	}
}
