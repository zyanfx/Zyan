using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CoreRemoting;
using CoreRemoting.Channels;
using Zyan.Communication.Security;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// General implementation of server protocol setup.
	/// </summary>
	public abstract class ServerProtocolSetup : IServerProtocolSetup
	{
		/// <summary>
		/// Unique channel name.
		/// </summary>
		protected string _channelName = "ServerProtocolSetup_" + Guid.NewGuid().ToString();

		/// <summary>
		/// Dictionary for channel settings.
		/// </summary>
		protected Dictionary<string, object> _channelSettings = new Dictionary<string, object>();

		/// <summary>
		/// Authentication provider.
		/// </summary>
		protected IAuthenticationProvider _authProvider = new NullAuthenticationProvider();

		/// <summary>
		/// Creates a new instance of the ServerProtocolSetupBase class.
		/// </summary>
		protected ServerProtocolSetup() { }

		/// <summary>
		/// Gets the name of the remoting channel.
		/// </summary>
		public string ChannelName { get { return _channelName; } }

		/// <summary>
		/// Build the configuration for the internal CoreRemoting server. 
		/// </summary>
		/// <returns>CoreRemoting server configuration</returns>
		public abstract ServerConfig BuildServerConfig();

		/// <summary>
		/// Gets or sets the authentication provider.
		/// </summary>
		public virtual IAuthenticationProvider AuthenticationProvider
		{
			get { return _authProvider; }
			set
			{
				if (value == null)
					_authProvider = new NullAuthenticationProvider();
				else
					_authProvider = value;
			}
		}

		/// <summary>
		/// Gets the URL for automatic discovery.
		/// </summary>
		public abstract string GetDiscoverableUrl();

		/// <summary>
		/// Determines whether the given URL is discoverable across the network.
		/// </summary>
		/// <param name="url">The URL to check.</param>
		protected virtual bool IsDiscoverableUrl(string url)
		{
			try
			{
				// everything but loopback is discoverable
				var uri = new Uri(url);
				var host = uri.Host.ToLower();
				return
					host != "localhost" &&
					host != "0.0.0.0" &&
					host != IPAddress.Loopback.ToString() &&
					host != IPAddress.IPv6Loopback.ToString();
			}
			catch (UriFormatException)
			{
				// invalid uri is not discoverable
				return false;
			}
		}

		//TODO: Find a way to get bound IP addresses of CoreRemoting server. 
		// /// <summary>
		// /// Gets the current machine IP addresses.
		// /// TODO: move this method to a helper class.
		// /// </summary>
		// internal static IPAddress[] GetIpAddresses()
		// {
		// 	var invalid = new[]
		// 	{
		// 		IPAddress.Loopback, IPAddress.Any,
		// 		IPAddress.IPv6Loopback, IPAddress.IPv6Any
		// 	};
		//
		// 	return Tcp.DuplexChannel.Manager.GetAddresses()
		// 		.Where(ip => !invalid.Contains(ip))
		// 		.ToArray();
		// }

		/// <summary>
		/// Replaces the host name within the given URL.
		/// TODO: move this method to a helper class.
		/// </summary>
		/// <param name="url">URL to replace the host name.</param>
		/// <param name="hostName">New host name.</param>
		internal static string TryReplaceHostName(string url, string hostName)
		{
			try
			{
				// use Uri class to validate the input
				var uri = new Uri(url ?? string.Empty);
				var builder = new UriBuilder(uri);
				builder.Host = hostName;
				return builder.Uri.ToString();
			}
			catch (UriFormatException)
			{
				// fail to format the given url
				return url;
			}
		}
	}
}
