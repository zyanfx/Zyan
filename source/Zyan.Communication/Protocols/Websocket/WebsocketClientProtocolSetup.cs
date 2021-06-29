using System;
using CoreRemoting;
using CoreRemoting.Channels.Websocket;

namespace Zyan.Communication.Protocols.Websocket
{
	/// <summary>
	/// Client protocol setup for websocket communication.
	/// </summary>
	public class WebsocketClientProtocolSetup : ClientProtocolSetup
	{
		/// <summary>
		/// Gets or sets the network hostname or the IP address of the remote server. 
		/// </summary>
		public string ServerHostName { get; set; }
		
		/// <summary>
		/// Gets or sets the TCP port of the remote server.
		/// </summary>
		public int ServerTcpPort { get; set; }

		/// <summary>
		/// Gets or sets the key size (only relevant, if message encryption is enabled).
		/// </summary>
		public int KeySize { get; set; }
		
		/// <summary>
		/// Gets or sets if message encryption should be enabled.
		/// </summary>
		public bool MessageEncryption { get; set; }
		
		/// <summary>
		/// Build the configuration for the internal CoreRemoting client. 
		/// </summary>
		/// <returns>CoreRemoting client configuration</returns>
		public override ClientConfig BuildClientConfig()
		{
			return new ClientConfig()
			{
				Channel = new WebsocketClientChannel(),
				ServerHostName = ServerHostName,
				ServerPort = ServerTcpPort,
				KeySize = KeySize,
				MessageEncryption = MessageEncryption
			};
		}

		/// <summary>
		/// Checks if the provided URL is valid for this protocol setup.
		/// </summary>
		/// <param name="serverUrl">URL to address the remote server</param>
		/// <returns>True if the URL is valid, otherwise false</returns>
		public override bool IsUrlValid(string serverUrl)
		{
			return serverUrl.Equals($"http://{ServerHostName}:{ServerTcpPort}",
				StringComparison.InvariantCultureIgnoreCase);
		}
	}
}