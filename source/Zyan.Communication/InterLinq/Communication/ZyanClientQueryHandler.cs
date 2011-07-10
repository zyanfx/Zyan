using System;
using Zyan.InterLinq;
using Zyan.InterLinq.Communication;
using Zyan.Communication;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Client handler class managing Zyan connection to the InterLINQ Server.
	/// </summary>
	/// <seealso cref="ClientQueryHandler"/>
	/// <seealso cref="InterLinqQueryHandler"/>
	/// <seealso cref="IQueryHandler"/>
	public class ZyanClientQueryHandler : ClientQueryHandler
	{
		/// <summary>
		/// Gets or sets a value indicating whether ambient transaction support is enabled.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if ambient transaction support; otherwise, <c>false</c>.
		/// </value>
		public bool ImplicitTransactionTransfer { get; set; }

		/// <summary>
		/// Gets the <see cref="ZyanConnection"/> associated with the client query handler.
		/// </summary>
		public ZyanConnection Connection { get; private set; }

		private string _uniqueName = string.Empty;

		/// <summary>
		/// Creates ZyanClientQueryHandler instance.
		/// </summary>
		/// <param name="connection">Zyan connection.</param>
		public ZyanClientQueryHandler(ZyanConnection connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException("connection");
			}

			Connection = connection;
		}

		/// <summary>
		/// Creates ZyanClientQueryHandler instance.
		/// </summary>
		/// <param name="connection">Zyan connection.</param>
		/// <param name="unqiueName">Unique component name</param>
		public ZyanClientQueryHandler(ZyanConnection connection, string unqiueName) : this(connection)
		{
			_uniqueName = unqiueName;
		}

		/// <summary>
		/// Creates ZyanClientQueryHandler instance.
		/// </summary>
		/// <param name="serverUrl">URL where the Remote Objects will be published.</param>
		public ZyanClientQueryHandler(string serverUrl)
		{
			if (string.IsNullOrEmpty(serverUrl))
			{
				throw new ArgumentNullException("serverUrl");
			}

			Connection = new ZyanConnection(serverUrl);
		}

		/// <summary>
		/// Connects to the server.
		/// <see cref="InterLinqQueryHandler"/>
		/// </summary>
		/// <seealso cref="ClientQueryHandler.Connect"/>
		public override void Connect()
		{
			if (queryRemoteHandler == null)
			{
				queryRemoteHandler = Connection.CreateProxy<IQueryRemoteHandler>(_uniqueName,ImplicitTransactionTransfer);

				if (queryRemoteHandler == null)
				{
					throw new Exception("Connection to server could no be made.");
				}
			}
		}
	}
}
