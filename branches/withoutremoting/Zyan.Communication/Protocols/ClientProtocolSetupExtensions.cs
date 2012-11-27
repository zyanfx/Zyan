using System;
using System.Linq;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Contains extension methods for client protocol setups.
	/// </summary>
	public static class ClientProtocolSetupExtensions
	{
		/// <summary>
		/// Adds a single channel setting.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <param name="name">Name of channel setting (example: "port")</param>
		/// <param name="value">Value of channel setting (example: 8080)</param>
		/// <returns></returns>
		public static IClientProtocolSetup AddChannelSetting(this IClientProtocolSetup protocolSetup, string name, object value)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			protocolSetup.ChannelSettings.Add(name, value);

			return protocolSetup;
		}
	}
}
