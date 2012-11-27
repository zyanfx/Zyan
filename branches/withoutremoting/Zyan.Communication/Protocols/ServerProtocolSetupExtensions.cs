using System;
using System.Linq;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Contains extension methods for server protocol setups.
	/// </summary>
	public static class ServerProtocolSetupExtensions
	{
		/// <summary>
		/// Adds a single channel setting.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <param name="name">Name of channel setting (example: "port")</param>
		/// <param name="value">Value of channel setting (example: 8080)</param>
		/// <returns></returns>
		public static IServerProtocolSetup AddChannelSetting(this IServerProtocolSetup protocolSetup, string name, object value)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			protocolSetup.ChannelSettings.Add(name, value);

			return protocolSetup;
		}
	}
}
