using System.Collections.Generic;
using CoreRemoting;
using CoreRemoting.Channels;
using Zyan.Communication.Security;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Describes server side communication protocol settings.
	/// </summary>
	public interface IServerProtocolSetup
	{
		/// <summary>
		/// Build the configuration for the internal CoreRemoting server. 
		/// </summary>
		/// <returns>CoreRemoting server configuration</returns>
		ServerConfig BuildServerConfig();
		
		/// <summary>
		/// Gets the authentication provider.
		/// </summary>
		IAuthenticationProvider AuthenticationProvider
		{
			get;
		}
		
		/// <summary>
		/// Gets the name of the remoting channel.
		/// </summary>
		string ChannelName { get; }

		/// <summary>
		/// Gets the URL for automatic discovery.
		/// </summary>
		string GetDiscoverableUrl();
	}
}
