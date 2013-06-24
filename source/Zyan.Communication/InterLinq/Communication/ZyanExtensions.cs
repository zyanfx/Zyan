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
		#region Messages

		private const string ObsoleteMessage = "are not used anymore. Linq queries can be executed against any Zyan components. " +
			"Implement an interface with methods like IQueryable<T> Query<T>() or IEnumerable<T> Get<T>() in a component to be able to use Linq against it.";
		private const string CreateQueryableProxyObsoleteMessage = "Queryable proxies " + ObsoleteMessage;
		private const string RegisterQueryableComponentObsoleteMessage = "Queryable components " + ObsoleteMessage;

		#endregion

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
		/// Registers IQueryHandler as IQueryable component
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="queryHandler">Query handler</param>
		public static void RegisterQueryHandler(this ZyanComponentHost host, IQueryHandler queryHandler)
		{
			host.RegisterComponent<IQueryRemoteHandler, ZyanServerQueryHandler>(new ZyanServerQueryHandler(queryHandler));
		}

		/// <summary>
		/// Registers IQueryHandler as IQueryable component
		/// </summary>
		/// <param name="host">Component host</param>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="queryHandler">Query handler</param>
		public static void RegisterQueryHandler(this ZyanComponentHost host, string uniqueName, IQueryHandler queryHandler)
		{
			host.RegisterComponent<IQueryRemoteHandler, ZyanServerQueryHandler>(uniqueName, new ZyanServerQueryHandler(queryHandler));
		}
	}
}
