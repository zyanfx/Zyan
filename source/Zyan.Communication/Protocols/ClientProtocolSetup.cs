using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CoreRemoting;
using Zyan.Communication.Protocols.Websocket;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// General implementation of client protocol setup.
	/// </summary>
	public abstract class ClientProtocolSetup : IClientProtocolSetup
	{
		/// <summary>
		/// Initializes the <see cref="ClientProtocolSetup" /> class.
		/// </summary>
		static ClientProtocolSetup()
		{
			// set up default client protocols
			DefaultClientProtocols = new Dictionary<string, Lazy<IClientProtocolSetup>>
			{
				{ "http://", new Lazy<IClientProtocolSetup>(() => new WebsocketClientProtocolSetup(), true) }
			};
		}

		/// <summary>
		/// Unique channel name.
		/// </summary>
		protected string _channelName = "ClientProtocolSetup_" + Guid.NewGuid().ToString();
		
		/// <summary>
		/// Creates a new instance of the ClientProtocolSetup class.
		/// </summary>
		protected ClientProtocolSetup() { }

		/// <summary>
		/// Registers the default protocol setup for the given URL prefix.
		/// </summary>
		/// <param name="urlPrefix">The URL prefix.</param>
		/// <param name="factory">The protocol setup factory.</param>
		public static void RegisterClientProtocol(string urlPrefix, Func<IClientProtocolSetup> factory)
		{
			DefaultClientProtocols[urlPrefix] = new Lazy<IClientProtocolSetup>(factory, true);
		}

		/// <summary>
		/// Gets the default client protocol setup for the given URL.
		/// </summary>
		/// <param name="url">The URL to connect to.</param>
		/// <returns><see cref="IClientProtocolSetup"/> implementation, or null, if the default protocol is not found.</returns>
		public static IClientProtocolSetup GetClientProtocol(string url)
		{
			foreach (var pair in DefaultClientProtocols)
			{
				if (url.StartsWith(pair.Key, StringComparison.InvariantCultureIgnoreCase))
				{
					return pair.Value.Value;
				}
			}

			return null;
		}

		private static Dictionary<string, Lazy<IClientProtocolSetup>> DefaultClientProtocols { get; set; }

		/// <summary>
		/// Gets the name of the remoting channel.
		/// </summary>
		public string ChannelName { get { return _channelName; } }

		/// <summary>
		/// Build the configuration for the internal CoreRemoting client. 
		/// </summary>
		/// <returns>CoreRemoting client configuration</returns>
		public abstract ClientConfig BuildClientConfig();
		
		/// <summary>
		/// Checks if the provided URL is valid for this protocol setup.
		/// </summary>
		/// <param name="serverUrl">URL to address the remote server</param>
		/// <returns>True if the URL is valid, otherwise false</returns>
		public abstract bool IsUrlValid(string serverUrl);
	}
}
