using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using Zyan.Communication.Toolbox;
using Zyan.InterLinq;

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
		/// MethodInfo for RegisterComponent{I} method, used to create generic method delegates
		/// </summary>
		static MethodInfo RegisterComponentMethodInfo = typeof(IComponentCatalogMefExtensions).GetMethod("RegisterComponent", BindingFlags.Static | BindingFlags.NonPublic,
			null, new[] { typeof(IComponentCatalog), typeof(ComposablePartCatalog), typeof(CompositionContainer), typeof(string) }, null);

		/// <summary>
		/// Registers component implementing interface I. Component instance is provided by MEF container.
		/// </summary>
		static void RegisterComponent<I>(this IComponentCatalog host, ComposablePartCatalog childCatalog, CompositionContainer parentContainer, string contractName = null)
		{
			// component -> owning container
			var containers = new ConcurrentDictionary<object, CompositionContainer>();
			var uniqueName = contractName ?? typeof(I).FullName;

			// component instance is created inside the child container
			host.RegisterComponent<I>
			(
				uniqueName,
				delegate // factory method
				{
					// create child container for early component disposal
					var childContainer = new CompositionContainer(childCatalog, parentContainer);
					var component = contractName != null ? childContainer.GetExport<I>(contractName).Value : childContainer.GetExport<I>().Value;
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
		/// MethodInfo for RegisterQueryableComponent{I} method, used to create generic method delegates
		/// </summary>
		static MethodInfo RegisterQueryableComponentMethodInfo = typeof(IComponentCatalogMefExtensions).GetMethod("RegisterQueryableComponent", BindingFlags.Static | BindingFlags.NonPublic,
			null, new[] { typeof(IComponentCatalog), typeof(ComposablePartCatalog), typeof(CompositionContainer), typeof(string) }, null);

		/// <summary>
		/// Registers queryable component implementing either IObjectSource or IEntitySource. Component instance is provided by MEF container.
		/// </summary>
		static void RegisterQueryableComponent<I>(this IComponentCatalog host, ComposablePartCatalog childCatalog, CompositionContainer parentContainer, string contractName = null) where I : IBaseSource
		{
			// zyanServerQueryHandler -> owning container
			var containers = new ConcurrentDictionary<object, CompositionContainer>();
			var uniqueName = contractName ?? typeof(I).FullName;

			// ZyanServerQueryHandler factory for the given object or entity source
			var createZyanServerQueryHandler = GetZyanServerQueryHandlerFactory<I>();

			// queryable component instance is created inside the child container
			host.RegisterComponent<IQueryRemoteHandler>
			(
				uniqueName,
				delegate // factory method
				{
					// create child container for early component disposal and fetch exported component
					var childContainer = new CompositionContainer(childCatalog, parentContainer);
					var component = contractName != null ? childContainer.GetExport<I>(contractName).Value : childContainer.GetExport<I>().Value;

					// create server query handler for exported component
					var zyanServerQueryHandler = createZyanServerQueryHandler(component);
					containers[zyanServerQueryHandler] = childContainer;
					return zyanServerQueryHandler;
				},
				delegate(object zyanServerQueryHandler) // cleanup delegate
				{
					CompositionContainer childContainer;

					// free child container and release non-shared MEF component
					if (containers.TryRemove(zyanServerQueryHandler, out childContainer))
					{
						childContainer.Dispose();
					}
				}
			);
		}

		/// <summary>
		/// Returns factory method for ZyanServerQueryHandler: component -> server query handler
		/// </summary>
		/// <typeparam name="I">Component interface</typeparam>
		/// <returns>Factory method</returns>
		static Func<object, object> GetZyanServerQueryHandlerFactory<I>() where I : IBaseSource
		{
			if (typeof(IObjectSource).IsAssignableFrom(typeof(I)))
				return component =>
					new ZyanServerQueryHandler((IObjectSource)component);

			if (typeof(IEntitySource).IsAssignableFrom(typeof(I)))
				return component =>
					new ZyanServerQueryHandler((IEntitySource)component);

			// interface should be either IObjectSource or IEntitySource
			throw new NotSupportedException("Type not supported: " + typeof(I).Name); 
		}

		/// <summary>
		/// Registers component of the given type. Component instance is provided by MEF container.
		/// </summary>
		static void RegisterComponent(this IComponentCatalog host, ComposablePartCatalog childCatalog, CompositionContainer parentContainer, Type type, string contractName = null)
		{
			// select registration method
			var methodInfo = RegisterComponentMethodInfo;
			if (typeof(IBaseSource).IsAssignableFrom(type))
			{
				methodInfo = RegisterQueryableComponentMethodInfo;
			}

			// create registration method and register the component
			var method = methodInfo.MakeGenericMethod(type);
			var registerComponent = method.CreateDelegate<ComponentRegistrationDelegate>();
			registerComponent(host, childCatalog, parentContainer, contractName);
		}
	}
}
