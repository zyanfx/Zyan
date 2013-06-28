using System.Collections.Generic;
using System.Runtime.Remoting.Channels;

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

		/// <summary>
		/// Gets the name of the remoting channel.
		/// </summary>
		string ChannelName { get; }

		/// <summary>
		/// Formats the connection URL for this protocol.
		/// </summary>
		/// <param name="parts">The parts of the url, such as server name, port, etc.</param>
		/// <returns>
		/// Formatted URL supported by the protocol.
		/// </returns>
		string FormatUrl(params object[] parts);

		/// <summary>
		/// Checks whether the given URL is valid for this protocol.
		/// </summary>
		/// <param name="url">The URL to check.</param>
		/// <returns>
		/// True, if the URL is supported by the protocol, otherwise, False.
		/// </returns>
		bool IsUrlValid(string url);
	}
}
