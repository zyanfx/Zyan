using System;
using System.Collections;
using System.Runtime.Remoting.Channels;

namespace Zyan.InterLinq.Communication.Remoting
{
	/// <summary>
	/// Client handler class managing the connection 
	/// via Remoting to the InterLINQ Server.
	/// </summary>
	/// <seealso cref="ClientQueryHandler"/>
	/// <seealso cref="InterLinqQueryHandler"/>
	/// <seealso cref="IQueryHandler"/>
	public class ClientQueryRemotingHandler : ClientQueryHandler
	{
		#region Fields

		private readonly bool makeDefaultConnection;
		private string url;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		public ClientQueryRemotingHandler() : this(true) { }

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="makeDefaultConnection">If set to true, the client will connect to tcp://localhost:7890/InterLINQ_Remoting_Server.</param>
		public ClientQueryRemotingHandler(bool makeDefaultConnection)
		{
			this.makeDefaultConnection = makeDefaultConnection;
		}

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="url">URL where the Remote Objects will be published.</param>
		public ClientQueryRemotingHandler(string url) : this(false)
		{
			if (string.IsNullOrEmpty(url))
			{
				throw new ArgumentNullException("url");
			}
			this.url = url;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Connects to the server.
		/// <see cref="InterLinqQueryHandler"/>
		/// </summary>
		/// <seealso cref="ClientQueryHandler.Connect"/>
		public override void Connect()
		{
			if (queryRemoteHandler == null)
			{
				if (makeDefaultConnection)
				{
					Hashtable properties = new Hashtable();
					properties["name"] = RemotingConstants.DefaultServiceChannelName;
					ChannelServices.RegisterChannel(RemotingConstants.GetDefaultChannel(properties), false);

					url = string.Format("{0}://{1}:{2}/{3}", RemotingConstants.DefaultServiceProtcol, RemotingConstants.DefaultServerName, RemotingConstants.DefaultServicePort, RemotingConstants.DefaultServerObjectName);
				}

				queryRemoteHandler = (IQueryRemoteHandler)Activator.GetObject(typeof(IQueryRemoteHandler), url);

				if (queryRemoteHandler == null)
				{
					throw new Exception("Connection to server could no be made.");
				}
			}
		}

		#endregion
	}
}
