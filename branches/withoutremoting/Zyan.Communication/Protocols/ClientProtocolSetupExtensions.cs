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
		/// Adds a specified client sink provider into the client sink chain.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <param name="clientSinkProvider">Client sink provider to be added</param>
		/// <returns>Protocol setup</returns>
		public static IClientProtocolSetup AddClientSink(this IClientProtocolSetup protocolSetup, IClientChannelSinkProvider clientSinkProvider)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			if (clientSinkProvider == null)
				throw new ArgumentNullException("clientSinkProvider");

			protocolSetup.ClientSinkChain.Add(clientSinkProvider);

			return protocolSetup;
		}

		/// <summary>
		/// Adds a specified server sink provider into the server sink chain.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <param name="serverSinkProvider">Server sink provider to be added</param>
		/// <returns>Protocol setup</returns>
		public static IClientProtocolSetup AddServerSink(this IClientProtocolSetup protocolSetup, IServerChannelSinkProvider serverSinkProvider)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			if (serverSinkProvider == null)
				throw new ArgumentNullException("serverSinkProvider");

			protocolSetup.ServerSinkChain.Add(serverSinkProvider);

			return protocolSetup;
		}

		/// <summary>
		/// Adds a specified client sink provider into the client sink chain before the formatter.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <param name="clientSinkProvider">Client sink provider to be added</param>
		/// <returns>Protocol setup</returns>
		public static IClientProtocolSetup AddClientSinkBeforeFormatter(this IClientProtocolSetup protocolSetup, IClientChannelSinkProvider clientSinkProvider)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			if (clientSinkProvider == null)
				throw new ArgumentNullException("clientSinkProvider");

			IClientFormatterSinkProvider formatter = GetClientFormatter(protocolSetup);

			if (formatter == null)
				throw new ApplicationException(LanguageResource.ApplicationException_NoFormatterSpecified);

			int index = protocolSetup.ClientSinkChain.IndexOf((IClientChannelSinkProvider)formatter);
			protocolSetup.ClientSinkChain.Insert(index, clientSinkProvider);

			return protocolSetup;
		}

		/// <summary>
		/// Adds a specified client sink provider into the client sink chain after the formatter.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <param name="clientSinkProvider">Client sink provider to be added</param>
		/// <returns>Protocol setup</returns>
		public static IClientProtocolSetup AddClientSinkAfterFormatter(this IClientProtocolSetup protocolSetup, IClientChannelSinkProvider clientSinkProvider)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			if (clientSinkProvider == null)
				throw new ArgumentNullException("clientSinkProvider");

			IClientFormatterSinkProvider formatter = GetClientFormatter(protocolSetup);

			if (formatter == null)
				throw new ApplicationException(LanguageResource.ApplicationException_NoFormatterSpecified);

			int index = protocolSetup.ClientSinkChain.IndexOf((IClientChannelSinkProvider)formatter);
			protocolSetup.ClientSinkChain.Insert(index + 1, clientSinkProvider);

			return protocolSetup;
		}

		/// <summary>
		/// Adds a specified server sink provider into the server sink chain before the formatter.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <param name="serverSinkProvider">Server sink provider to be added</param>
		/// <returns>Protocol setup</returns>
		public static IClientProtocolSetup AddServerSinkBeforeFormatter(this IClientProtocolSetup protocolSetup, IServerChannelSinkProvider serverSinkProvider)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			if (serverSinkProvider == null)
				throw new ArgumentNullException("serverSinkProvider");

			IServerFormatterSinkProvider formatter = GetServerFormatter(protocolSetup);

			if (formatter == null)
				throw new ApplicationException(LanguageResource.ApplicationException_NoFormatterSpecified);

			int index = protocolSetup.ServerSinkChain.IndexOf((IServerChannelSinkProvider)formatter);
			protocolSetup.ServerSinkChain.Insert(index, serverSinkProvider);

			return protocolSetup;
		}

		/// <summary>
		/// Adds a specified server sink provider into the server sink chain after the formatter.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <param name="serverSinkProvider">Server sink provider to be added</param>
		/// <returns>Protocol setup</returns>
		public static IClientProtocolSetup AddServerSinkAfterFormatter(this IClientProtocolSetup protocolSetup, IServerChannelSinkProvider serverSinkProvider)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			if (serverSinkProvider == null)
				throw new ArgumentNullException("serverSinkProvider");

			IServerFormatterSinkProvider formatter = GetServerFormatter(protocolSetup);

			if (formatter == null)
				throw new ApplicationException(LanguageResource.ApplicationException_NoFormatterSpecified);

			int index = protocolSetup.ServerSinkChain.IndexOf((IServerChannelSinkProvider)formatter);
			protocolSetup.ServerSinkChain.Insert(index + 1, serverSinkProvider);

			return protocolSetup;
		}

		/// <summary>
		/// Returns the configured formatter from the client sink chain of a specified client protocol setup.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <returns>client formatter sink</returns>
		public static IClientFormatterSinkProvider GetClientFormatter(this IClientProtocolSetup protocolSetup)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			return (from sink in protocolSetup.ClientSinkChain
					where sink is IClientFormatterSinkProvider
					select sink as IClientFormatterSinkProvider).FirstOrDefault();
		}

		/// <summary>
		/// Returns the configured formatter from the server sink chain of a specified client protocol setup.
		/// </summary>
		/// <param name="protocolSetup">Protocol setup</param>
		/// <returns>server formatter sink</returns>
		public static IServerFormatterSinkProvider GetServerFormatter(this IClientProtocolSetup protocolSetup)
		{
			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			return (from sink in protocolSetup.ServerSinkChain
					where sink is IServerFormatterSinkProvider
					select sink as IServerFormatterSinkProvider).FirstOrDefault();
		}

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
