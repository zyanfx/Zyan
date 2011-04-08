using Zyan.Communication;
using InterLinq;
using System;

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
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host) where T : IObjectSource, new()
		{
			host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(new T()));
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, ActivationType activationType) where T : IObjectSource, new()
		{
			host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(new T()), activationType);
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, string uniqueName) where T : IObjectSource, new()
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(new T()));
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="instance">Component instance</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, T instance) where T : IObjectSource
		{
			host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(instance));
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="instance">Component instance</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, string uniqueName, T instance) where T : IObjectSource
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(instance));
		}

		/// <summary>
		/// Registers IQueryable component
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent<T>(this ZyanComponentHost host, string uniqueName, ActivationType activationType) where T : IObjectSource, new()
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(new T()), activationType);
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
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Factory method to create component instance</param>
		/// <param name="activationType">Activation type</param>
		public static void RegisterQueryableComponent(this ZyanComponentHost host, string uniqueName, Func<IObjectSource> factoryMethod, ActivationType activationType)
		{
			host.RegisterComponent<IQueryRemoteHandler>(uniqueName, () => new ZyanServerQueryHandler(factoryMethod()), activationType);
		}
	}
}
