using CoreRemoting;
using CoreRemoting.Channels.Websocket;

namespace Zyan.Communication.Protocols.Websocket
{
	/// <summary>
	/// Server protocol setup for websocket communication.
	/// </summary>
	public class WebsocketServerProtocolSetup : ServerProtocolSetup
	{
		/// <summary>
		/// Gets or sets the network hostname or the IP address of the server. 
		/// </summary>
		public string NetworkHostName { get; set; }
		
		/// <summary>
		/// Gets or sets the TCP port of the server.
		/// </summary>
		public int TcpPort { get; set; }

		/// <summary>
		/// Gets or sets the key size (only relevant, if message encryption is enabled).
		/// </summary>
		public int KeySize { get; set; }
		
		/// <summary>
		/// Gets or sets if message encryption should be enabled.
		/// </summary>
		public bool MessageEncryption { get; set; }
		
		/// <summary>
		/// Build the configuration for the internal CoreRemoting server. 
		/// </summary>
		/// <returns>CoreRemoting server configuration</returns>
		public override ServerConfig BuildServerConfig()
		{
			return new ServerConfig()
			{
				Channel = new WebsocketServerChannel(),
				AuthenticationRequired = false, //TODO: Integrate Zyan auth provider
				HostName = NetworkHostName,
				NetworkPort = TcpPort,
				KeySize = KeySize,
				MessageEncryption = MessageEncryption
			};
		}

		/// <summary>
		/// Gets the URL for automatic discovery.
		/// </summary>
		public override string GetDiscoverableUrl()
		{
			return $"http://{NetworkHostName}:{TcpPort}";
		}
	}
}