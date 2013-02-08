using System.Collections.Generic;
using Zyan.Communication.Transport;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Describes client side communication protocol settings.
	/// </summary>
	public interface IClientProtocolSetup
	{
		/// <summary>
		/// Creates and configures a transport channel.
		/// </summary>
		/// <returns>Transport channel</returns>
		IClientTransportAdapter CreateTransportAdapter();

		/// <summary>
		/// Gets a dictionary with channel settings.
		/// </summary>
		Dictionary<string, object> ChannelSettings { get; }
	}
}
