using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using Zyan.Communication.Security;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Describes server side communication protocol settings.
	/// </summary>
	public interface IServerProtocolSetup
	{
		/// <summary>
		/// Gets a list of all Remoting sinks from the client sink chain.
		/// </summary>
		List<IClientChannelSinkProvider> ClientSinkChain { get; }

		/// <summary>
		/// Gets a list of all Remoting sinks from the server sink chain.
		/// </summary>
		List<IServerChannelSinkProvider> ServerSinkChain { get; }

		/// <summary>
		/// Creates and configures a Remoting channel.
		/// </summary>
		/// <returns>Remoting channel</returns>
		IChannel CreateChannel();

		/// <summary>
		/// Gets the authentication provider.
		/// </summary>
		IAuthenticationProvider AuthenticationProvider
		{
			get;
		}

		/// <summary>
		/// Gets a dictionary with channel settings.
		/// </summary>
		Dictionary<string, object> ChannelSettings { get; }

		/// <summary>
		/// Gets the name of the remoting channel.
		/// </summary>
		string ChannelName { get; }

		/// <summary>
		/// Gets the URL for automatic discovery.
		/// </summary>
		/// <param name="zyanHostName">Name of the <see cref="ZyanComponentHost"/> instance.</param>
		string GetDiscoverableUrl(string zyanHostName);
	}
}
