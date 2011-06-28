using System;
using System.Collections;
using System.Linq;
using Zyan.InterLinq;
using Zyan.InterLinq.Communication;
using Zyan.InterLinq.Expressions;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Provides methods to communicate with the InterLINQ service over Zyan. 
	/// </summary>
	public class ZyanServerQueryHandler : IQueryRemoteHandler
	{
		/// <summary>
		/// Gets the <see cref="IQueryRemoteHandler"/>.
		/// </summary>
		public IQueryRemoteHandler InnerHandler { get; private set; }

		/// <summary>
		/// Creates ZyanServerQueryHandler instance.
		/// </summary>
		/// <param name="innerHandler">Inner Handler of this Server</param>
		public ZyanServerQueryHandler(IQueryHandler innerHandler)
		{
			if (innerHandler == null)
			{
				throw new ArgumentNullException("innerHandler");
			}

			InnerHandler = new ServerQueryHandler(innerHandler);
		}

		/// <summary>
		/// Creates ZyanServerQueryHandler instance.
		/// </summary>
		/// <param name="handler">IEnumerable delegate</param>
		public ZyanServerQueryHandler(Func<Type, IEnumerable> handler)
		{
			if (handler == null)
			{
				throw new ArgumentNullException("handler");
			}

			InnerHandler = new ServerQueryHandler(new ZyanObjectQueryHandler(handler));
		}

		/// <summary>
		/// Creates ZyanServerQueryHandler instance.
		/// </summary>
		/// <param name="handler">IQueryable delegate</param>
		public ZyanServerQueryHandler(Func<Type, IQueryable> handler)
		{
			if (handler == null)
			{
				throw new ArgumentNullException("handler");
			}

			InnerHandler = new ServerQueryHandler(new ZyanEntityQueryHandler(handler));
		}

		/// <summary>
		/// Creates ZyanServerQueryHandler instance.
		/// </summary>
		/// <param name="source">IObjectSource instance</param>
		public ZyanServerQueryHandler(IObjectSource source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}

			InnerHandler = new ServerQueryHandler(new ZyanObjectQueryHandler(source));
		}

		/// <summary>
		/// Creates ZyanServerQueryHandler instance.
		/// </summary>
		/// <param name="source">IEntitySource instance</param>
		public ZyanServerQueryHandler(IEntitySource source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}

			InnerHandler = new ServerQueryHandler(new ZyanEntityQueryHandler(source));
		}

		/// <summary>
		/// Retrieves data from the server by an <see cref="SerializableExpression">Expression</see> tree.
		/// </summary>
		/// <remarks>
		/// This method's return type depends on the submitted 
		/// <see cref="SerializableExpression">Expression</see> tree.
		/// Here some examples ('T' is the requested type):
		/// <list type="list">
		///     <listheader>
		///         <term>Method</term>
		///         <description>Return Type</description>
		///     </listheader>
		///     <item>
		///         <term>Select(...)</term>
		///         <description>T[]</description>
		///     </item>
		///     <item>
		///         <term>First(...), Last(...)</term>
		///         <description>T</description>
		///     </item>
		///     <item>
		///         <term>Count(...)</term>
		///         <description><see langword="int"/></description>
		///     </item>
		///     <item>
		///         <term>Contains(...)</term>
		///         <description><see langword="bool"/></description>
		///     </item>
		/// </list>
		/// </remarks>
		/// <param name="expression">
		///     <see cref="SerializableExpression">Expression</see> tree 
		///     containing selection and projection.
		/// </param>
		/// <returns>Returns requested data.</returns>
		/// <seealso cref="IQueryRemoteHandler.Retrieve"/>
		public object Retrieve(SerializableExpression expression)
		{
			return InnerHandler.Retrieve(expression);
		}
	}
}
