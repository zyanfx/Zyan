using System.Collections.Generic;
using Zyan.Communication.Transport;
using Zyan.Communication.Security;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Describes server side communication protocol settings.
	/// </summary>
	public interface IServerProtocolSetup
	{
        /// <summary>
        /// Creates and configures a transport channel.
        /// </summary>
        /// <returns>Transport channel</returns>
        IServerTransportAdapter CreateTransportAdapter();

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
	}
}
