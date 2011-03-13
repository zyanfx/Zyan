using System;
using InterLinq;
using InterLinq.Communication;
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
		public bool ImplicitTransactionTransfer { get; set; }

		public ZyanConnection Connection { get; private set; }

        private string _unqiueName = string.Empty;

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
            _unqiueName = unqiueName;
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
				queryRemoteHandler = Connection.CreateProxy<IQueryRemoteHandler>(_unqiueName,ImplicitTransactionTransfer);

				if (queryRemoteHandler == null)
				{
					throw new Exception("Connection to server could no be made.");
				}
			}
		}
	}
}
