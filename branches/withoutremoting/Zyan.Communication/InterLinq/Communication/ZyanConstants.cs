using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;

namespace Zyan.InterLinq.Communication
{
	/// <summary>
	/// Constants for the default Zyan configuration.
	/// </summary>
	public abstract class ZyanConstants
	{
		/// <summary>
		/// The default service protocol for the server name
		/// </summary>
		public const string DefaultServiceProtocol = "tcpex";

		/// <summary>
		/// The default server name.
		/// </summary>
		public const string DefaultServerName = "localhost";

		/// <summary>
		/// The default remote object name for the factory.
		/// </summary>
		public const string DefaultServerObjectName = "InterLINQ_Zyan_Server";

		/// <summary>
		/// The default port a for remoting connection.
		/// </summary>
		public const int DefaultServicePort = 7899;

		/// <summary>
		/// A server protocol setup with the default name and the default port.
		/// </summary>
		/// <param name="port">Tcp port.</param>
		/// <returns>Returns a default <see cref="IServerProtocolSetup"/> for the <see cref="ZyanComponentHost"/>.</returns>
		public static IServerProtocolSetup GetDefaultServerProtocol(int port)
		{
			return new TcpDuplexServerProtocolSetup(port, new NullAuthenticationProvider(), false);
		}

		/// <summary>
		/// A client protocol setup with the default name and the default port.
		/// </summary>
		/// <returns>Returns a default <see cref="IClientProtocolSetup"/> for the <see cref="ZyanComponentHost"/>.</returns>
		public static IClientProtocolSetup GetDefaultClientProtocol()
		{
			return new TcpDuplexClientProtocolSetup(false);
		}
	}
}
