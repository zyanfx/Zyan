using System;
using System.Collections;
using System.Linq;
using Zyan.InterLinq;
using Zyan.Communication;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Extension methods for easier InterLINQ handlers access.
	/// </summary>
	public static class ZyanExtensions
	{
		/// <summary>
		/// Creates IQueryable proxy for Zyan connection
		/// </summary>
		/// <param name="connection">ZyanConnection</param>
		/// <param name="implicitTransactionTransfer">Transfer ambient transactions</param>
		public static ZyanClientQueryHandler CreateQueryableProxy(this ZyanConnection connection, bool implicitTransactionTransfer)
		{
			return new ZyanClientQueryHandler(connection) { ImplicitTransactionTransfer = implicitTransactionTransfer };
		}

		/// <summary>
		/// Creates IQueryable proxy for Zyan connection
		/// </summary>
		/// <param name="connection">ZyanConnection</param>
		public static ZyanClientQueryHandler CreateQueryableProxy(this ZyanConnection connection)
		{
			return new ZyanClientQueryHandler(connection);
		}

		/// <summary>
		/// Creates IQueryable proxy for Zyan connection
		/// </summary>
		/// <param name="connection">ZyanConnection</param>
		/// <param name="unqiueName">Unique component name</param>
		/// <param name="implicitTransactionTransfer">Transfer ambient transactions</param>
		public static ZyanClientQueryHandler CreateQueryableProxy(this ZyanConnection connection, string unqiueName, bool implicitTransactionTransfer)
		{
			return new ZyanClientQueryHandler(connection, unqiueName) { ImplicitTransactionTransfer = implicitTransactionTransfer };
		}

		/// <summary>
		/// Creates IQueryable proxy for Zyan connection
		/// </summary>
		/// <param name="connection">ZyanConnection</param>
		/// <param name="unqiueName">Unique component name</param>
		public static ZyanClientQueryHandler CreateQueryableProxy(this ZyanConnection connection, string unqiueName)
		{
			return new ZyanClientQueryHandler(connection, unqiueName);
		}

		/// <summary>
		/// Creates function returning ZyanServerQueryHandler for the given instance
		/// </summary>
		/// <typeparam name="T">Type (either IObjectSource or IEntitySource)</typeparam>
		private static Func<object> CreateServerHandler<T>(T instance) where T : IBaseSource
		{
			Func<object> handler = null;

			if (instance is IObjectSource)
			{
				handler = () => new ZyanServerQueryHandler(instance as IObjectSource);
			}

			if (instance is IEntitySource)
			{
				handler = () => new ZyanServerQueryHandler(instance as IEntitySource);
			}

			if (handler == null)
			{
				throw new NotSupportedException("Type not supported: " + typeof(T).Name);
			}

			return handler;
		}

		/// <summary>
		/// Creates function returning ZyanServerQueryHandler for the given type
		/// </summary>
		/// <typeparam name="T">Type (either IObjectSource or IEntitySource)</typeparam>
		private static Func<object> CreateServerHandler<T>() where T : IBaseSource, new()
		{
			Func<object> handler = null;

			if (typeof(IObjectSource).IsAssignableFrom(typeof(T)))
			{
				handler = () => new ZyanServerQueryHandler(new T() as IObjectSource);
			}

			if (typeof(IEntitySource).IsAssignableFrom(typeof(T)))
			{
				handler = () => new ZyanServerQueryHandler(new T() as IEntitySource);
			}

			if (handler == null)
			{
				throw new NotSupportedException("Type not supported: " + typeof(T).Name);
			}

			return handler;
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host) where T : IBaseSource, new()
		{
			host.RegisterComponent<IQueryRemoteHandler>(CreateServerHandler<T>());
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, ActivationType activationType) where T : IBaseSource, new()
		{
			host.RegisterComponent<IQueryRemoteHandler>(CreateServerHandler<T>(), activationType);
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, string uniqueName) where T : IBaseSource, new()
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, CreateServerHandler<T>());
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="instance">Component instance</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, T instance) where T : IBaseSource
		{
			host.RegisterComponent<IQueryRemoteHandler>(CreateServerHandler<T>(instance));
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="instance">Component instance</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, string uniqueName, T instance) where T : IBaseSource
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, CreateServerHandler<T>(instance));
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, string uniqueName, ActivationType activationType) where T : IBaseSource, new()
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, CreateServerHandler<T>(), activationType);
		}

		/// <summary>
		/// Registers IQueryHandler as IQueryable component
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="queryHandler">Query handler</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, IQueryHandler queryHandler)
		{
			host.RegisterComponent<IQueryRemoteHandler, ZyanServerQueryHandler>(new ZyanServerQueryHandler(queryHandler));
		}

		/// <summary>
		/// Registers IQueryHandler as IQueryable component
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="queryHandler">Query handler</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, IQueryHandler queryHandler)
		{
			host.RegisterComponent<IQueryRemoteHandler, ZyanServerQueryHandler>(uniqueName, new ZyanServerQueryHandler(queryHandler));
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="getMethod">Method returning IEnumerable instances of the given type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, Func<Type, IEnumerable> getMethod)
		{
			host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(getMethod));
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="getMethod">Method returning IQueryable instances of the given type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, Func<Type, IQueryable> getMethod)
		{
			host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(getMethod));
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="getMethod">Method returning IEnumerable instances of the given type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, Func<Type, IEnumerable> getMethod)
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(getMethod));
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="getMethod">Method returning IQueryable instances of the given type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, Func<Type, IQueryable> getMethod)
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(getMethod));
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="getMethod">Method returning IEnumerable instances of the given type</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, Func<Type, IEnumerable> getMethod, ActivationType activationType)
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(getMethod), activationType);
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="getMethod">Method returning IQueryable instances of the given type</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, Func<Type, IQueryable> getMethod, ActivationType activationType)
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(getMethod), activationType);
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="factoryMethod">Factory method to create component instance</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, Func<IObjectSource> factoryMethod)
		{
			host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(factoryMethod()));
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="factoryMethod">Factory method to create component instance</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, Func<IEntitySource> factoryMethod)
		{
			host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(factoryMethod()));
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Factory method to create component instance</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, Func<IObjectSource> factoryMethod)
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(factoryMethod()));
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Factory method to create component instance</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, Func<IEntitySource> factoryMethod)
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(factoryMethod()));
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="factoryMethod">Factory method to create component instance</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, Func<IObjectSource> factoryMethod, ActivationType activationType)
		{
			host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(factoryMethod()), activationType);
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="factoryMethod">Factory method to create component instance</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, Func<IEntitySource> factoryMethod, ActivationType activationType)
		{
			host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(factoryMethod()), activationType);
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Factory method to create component instance</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, Func<IObjectSource> factoryMethod, ActivationType activationType)
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(factoryMethod()), activationType);
		}

		/// <summary>
		/// Registers IQueryable component factory
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Factory method to create component instance</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, Func<IEntitySource> factoryMethod, ActivationType activationType)
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(factoryMethod()), activationType);
		}
	}
}
