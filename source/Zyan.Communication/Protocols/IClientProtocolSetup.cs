using System.Collections.Generic;
using CoreRemoting;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Describes client side communication protocol settings.
	/// </summary>
	public interface IClientProtocolSetup
	{
		/// <summary>
		/// Build the configuration for the internal CoreRemoting client. 
		/// </summary>
		/// <returns>CoreRemoting client configuration</returns>
		ClientConfig BuildClientConfig();

		/// <summary>
		/// Gets the name of the remoting channel.
		/// </summary>
		string ChannelName { get; }

		/// <summary>
		/// Checks if the provided URL is valid for this protocol setup.
		/// </summary>
		/// <param name="serverUrl">URL to address the remote server</param>
		/// <returns>True if the URL is valid, otherwise false</returns>
		bool IsUrlValid(string serverUrl);
	}
}
