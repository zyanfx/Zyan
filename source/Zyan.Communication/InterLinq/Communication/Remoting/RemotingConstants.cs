using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Zyan.InterLinq.Communication.Remoting
{
	/// <summary>
	/// Constants for the default remoting configuration.
	/// </summary>
	public abstract class RemotingConstants
	{
		/// <summary>
		/// The default service protocol for the server name
		/// </summary>
		public const string DefaultServiceProtcol = "tcp";

		/// <summary>
		/// The default server name.
		/// </summary>
		public const string DefaultServerName = "localhost";

		/// <summary>
		/// The default remote object name for the factory.
		/// </summary>
		public const string DefaultServerObjectName = "InterLINQ_Remoting_Server";

		/// <summary>
		/// The default service channel name for the remoting connection.
		/// </summary>
		public const string DefaultServiceChannelName = "InterLINQ_tcp_binary";

		/// <summary>
		/// The default port a for remoting connection.
		/// </summary>
		public const int DefaultServicePort = 7890;

		/// <summary>
		/// A channel with the default name and the default port.
		/// </summary>
		/// <param name="properties">The properties of the channel.</param>
		/// <returns>Retruns a default <see cref="IChannel"/>.</returns>
		public static IChannel GetDefaultChannel(IDictionary properties)
		{
			return new TcpChannel(properties, new BinaryClientFormatterSinkProvider(), new BinaryServerFormatterSinkProvider());
		}
	}
}
