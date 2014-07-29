using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using Zyan.Communication.Toolbox;
using Zyan.InterLinq;

namespace Zyan.Communication.Composition
{
	// Shortcut name for registration method delegate
	using ComponentRegistrationDelegate = Action<IComponentCatalog, ComposablePartCatalog, CompositionContainer, string, ActivationType>;
	using IAnyInterface = IDisposable;

	/// <summary>
	/// Server-side MEF integration
	/// </summary>
	public static class IComponentCatalogMefExtensions
	{
		/// <summary>
		/// Registers all components in a MEF catalog attributed by "ComponentInterface" metadata tag.
		/// </summary>
		/// <param name="host"><see cref="ZyanComponentHost"/> or <see cref="ComponentCatalog"/> instance.</param>
		/// <param name="rootCatalog">Component catalog to register components.</param>
		/// <param name="container">Optional root composition container used to instantiate components.</param>
		public static void RegisterComponents(this IComponentCatalog host, ComposablePartCatalog rootCatalog, CompositionContainer container = null)
		{
			var parentContainer = container ??
#if FX4
				new CompositionContainer(rootCatalog);
#else
				new CompositionContainer(rootCatalog, CompositionOptions.DisableSilentRejection);
#endif
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
		/// <param name="host"><see cref="ZyanComponentHost"/> or <see cref="ComponentCatalog"/> instance.</param>
		/// <param name="container">Root composition container used to instantiate components.</param>
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
					// treat shared parts as singletons (influences event handling in ZyanDispatcher)
					var activationType = ActivationType.SingleCall;
					var creationPolicyKey = typeof(CreationPolicy).FullName;
					if (export.Metadata.ContainsKey(creationPolicyKey))
					{
						var creationPolicy = export.Metadata[creationPolicyKey];
						if (creationPolicy is CreationPolicy && (CreationPolicy)creationPolicy == CreationPolicy.Shared)
						{
							activationType = ActivationType.Singleton;
						}
					}

					// register exported component
					host.RegisterComponent(childCatalog, parentContainer, type, export.ContractName, activationType);
				}
			}
		}

		/// <summary>
		/// MethodInfo for RegisterComponent{I} method, used to create generic method delegates.
		/// </summary>
		static MethodInfo RegisterComponentMethodInfo = new ComponentRegistrationDelegate(RegisterComponent<IAnyInterface>).Method.GetGenericMethodDefinition();

		/// <summary>
		/// Registers component implementing interface I. Component instance is provided by MEF container.
		/// </summary>
		static void RegisterComponent<I>(this IComponentCatalog host, ComposablePartCatalog childCatalog, CompositionContainer parentContainer, string contractName, ActivationType activationType)
		{
			var uniqueName = contractName ?? typeof(I).FullName;

			// Singleton component instance is created in the parent container
			if (activationType == ActivationType.Singleton)
			{
				host.RegisterComponent<I>
				(
					uniqueName,
					delegate // lazy-initialized singleton factory
					{
						var component = contractName != null ?
							parentContainer.GetExport<I>(contractName).Value :
							parentContainer.GetExport<I>().Value;

						return component;
					},
					ActivationType.Singleton,
					delegate (object component) // empty cleanup handler
					{
						// do not clean up component instance
						// even if it implements IDisposable
					}
				);
				return;
			}

			// Dictionary: component -> owning container
			var containers = new ConcurrentDictionary<object, CompositionContainer>();
			var lockObject = new object();

			// SingleCall component instance is created inside the child container
			host.RegisterComponent<I>
			(
				uniqueName,
				delegate // factory method
				{
					// create child container for early component disposal
					var childContainer =
#if FX4
						new CompositionContainer(childCatalog, parentContainer);
#else
						new CompositionContainer(childCatalog, CompositionOptions.DisableSilentRejection, parentContainer);
#endif
					var component = contractName != null ? 
						childContainer.GetExport<I>(contractName).Value : 
						childContainer.GetExport<I>().Value;

					containers[component] = childContainer;
					return component;
				},
				ActivationType.SingleCall,
				delegate (object component) // cleanup delegate
				{
					CompositionContainer childContainer;

					// free child container and release non-shared MEF component
					if (containers.TryRemove(component, out childContainer))
					{
						lock (lockObject)
						{
							childContainer.Dispose();
						}
					}
				}
			);
		}

		/// <summary>
		/// Registers component of the given type. Component instance is provided by MEF container.
		/// </summary>
		static void RegisterComponent(this IComponentCatalog host, ComposablePartCatalog childCatalog, CompositionContainer parentContainer, Type type, string contractName, ActivationType activationType)
		{
			// create registration method and register the component
			var method = RegisterComponentMethodInfo.MakeGenericMethod(type);
			var registerComponent = method.CreateDelegate<ComponentRegistrationDelegate>();
			registerComponent(host, childCatalog, parentContainer, contractName, activationType);
		}
	}
}
