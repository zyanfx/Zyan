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
