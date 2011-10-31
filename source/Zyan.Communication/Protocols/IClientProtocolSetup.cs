using System;
using System.Runtime.Remoting.Channels;
using System.Collections.Generic;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Describes client side communication protocol settings.
	/// </summary>
	public interface IClientProtocolSetup
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
		/// Gets a dictionary with channel settings.
		/// </summary>
		Dictionary<string, object> ChannelSettings { get; }
	}
}
