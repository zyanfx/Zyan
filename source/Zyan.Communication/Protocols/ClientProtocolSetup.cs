using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using Zyan.Communication.Protocols.Http;
using Zyan.Communication.Protocols.Ipc;
using Zyan.Communication.Protocols.Null;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// General implementation of client protocol setup.
	/// </summary>
	public class ClientProtocolSetup : IClientProtocolSetup
	{
		/// <summary>
		/// Initializes the <see cref="ClientProtocolSetup" /> class.
		/// </summary>
		static ClientProtocolSetup()
		{
			// set up default client protocols
			DefaultClientProtocols = new Dictionary<string, Lazy<IClientProtocolSetup>>
			{
				{ "tcpex://", new Lazy<IClientProtocolSetup>(() => new TcpDuplexClientProtocolSetup(), true) },
				{ "tcp://", new Lazy<IClientProtocolSetup>(() => new TcpBinaryClientProtocolSetup(), true) },
				{ "ipc://", new Lazy<IClientProtocolSetup>(() => new IpcBinaryClientProtocolSetup(), true) },
				{ "http://", new Lazy<IClientProtocolSetup>(() => new HttpCustomClientProtocolSetup(), true) },
				{ "null://", new Lazy<IClientProtocolSetup>(() => new NullClientProtocolSetup(), true) }
			};
		}

		/// <summary>
		/// Unique channel name.
		/// </summary>
		protected string _channelName = "ClientProtocolSetup_" + Guid.NewGuid().ToString();

		/// <summary>
		/// Dictionary for channel settings.
		/// </summary>
		protected Dictionary<string, object> _channelSettings = new Dictionary<string, object>();

		/// <summary>
		/// List for building the client sink chain.
		/// </summary>
		protected List<IClientChannelSinkProvider> _clientSinkChain = new List<IClientChannelSinkProvider>();

		/// <summary>
		/// List for building the server sink chain.
		/// </summary>
		protected List<IServerChannelSinkProvider> _serverSinkChain = new List<IServerChannelSinkProvider>();

		/// <summary>
		/// Delegate to factory method, which creates the .NET Remoting channel instance.
		/// </summary>
		protected Func<IDictionary, IClientChannelSinkProvider, IServerChannelSinkProvider, IChannel> _channelFactory = null;

		/// <summary>
		/// Creates a new instance of the ClientProtocolSetup class.
		/// </summary>
		protected ClientProtocolSetup() { }

		/// <summary>
		/// Creates a new instance of the ClientProtocolSetup class.
		/// </summary>
		/// <param name="channelFactory">Delegate to channel factory method</param>
		public ClientProtocolSetup(Func<IDictionary, IClientChannelSinkProvider, IServerChannelSinkProvider, IChannel> channelFactory)
		{
			if (channelFactory == null)
				throw new ArgumentNullException("channelFactory");

			_channelFactory = channelFactory;
		}

		/// <summary>
		/// Creates a new ClientProtocolSetup with a specified channel factory method.
		/// </summary>
		/// <param name="channelFactory">Delegate to channel factory method</param>
		/// <returns></returns>
		public static IClientProtocolSetup WithChannel(Func<IDictionary, IClientChannelSinkProvider, IServerChannelSinkProvider, IChannel> channelFactory)
		{
			return new ClientProtocolSetup(channelFactory);
		}

		/// <summary>
		/// Registers the default protocol setup for the given URL prefix.
		/// </summary>
		/// <param name="urlPrefix">The URL prefix.</param>
		/// <param name="factory">The protocol setup factory.</param>
		public static void RegisterClientProtocol(string urlPrefix, Func<IClientProtocolSetup> factory)
		{
			DefaultClientProtocols[urlPrefix] = new Lazy<IClientProtocolSetup>(factory, true);
		}

		/// <summary>
		/// Gets the default client protocol setup for the given URL.
		/// </summary>
		/// <param name="url">The URL to connect to.</param>
		/// <returns><see cref="IClientProtocolSetup"/> implementation, or null, if the default protocol is not found.</returns>
		public static IClientProtocolSetup GetClientProtocol(string url)
		{
			foreach (var pair in DefaultClientProtocols)
			{
				if (url.StartsWith(pair.Key, StringComparison.InvariantCultureIgnoreCase))
				{
					return pair.Value.Value;
				}
			}

			return null;
		}

		private static Dictionary<string, Lazy<IClientProtocolSetup>> DefaultClientProtocols { get; set; }

		/// <summary>
		/// Gets a dictionary with channel settings.
		/// </summary>
		public virtual Dictionary<string, object> ChannelSettings { get { return _channelSettings; } }

		/// <summary>
		/// Gets a list of all Remoting sinks from the client sink chain.
		/// </summary>
		public virtual List<IClientChannelSinkProvider> ClientSinkChain { get { return _clientSinkChain; } }

		/// <summary>
		/// Gets a list of all Remoting sinks from the server sink chain.
		/// </summary>
		public virtual List<IServerChannelSinkProvider> ServerSinkChain { get { return _serverSinkChain; } }

		/// <summary>
		/// Gets the name of the remoting channel.
		/// </summary>
		public string ChannelName { get { return _channelName; } }

		/// <summary>
		/// Builds the client sink chain.
		/// </summary>
		/// <returns>First sink provider in sink chain</returns>
		protected virtual IClientChannelSinkProvider BuildClientSinkChain()
		{
			IClientChannelSinkProvider firstProvider = null;
			IClientChannelSinkProvider lastProvider = null;

			foreach (var sinkProvider in _clientSinkChain)
			{
				sinkProvider.Next = null;

				if (firstProvider == null)
					firstProvider = sinkProvider;

				if (lastProvider == null)
					lastProvider = sinkProvider;
				else
				{
					lastProvider.Next = sinkProvider;
					lastProvider = sinkProvider;
				}
			}

			return firstProvider;
		}

		/// <summary>
		/// Builds the server sink chain.
		/// </summary>
		/// <returns>First sink provider in sink chain</returns>
		protected virtual IServerChannelSinkProvider BuildServerSinkChain()
		{
			IServerChannelSinkProvider firstProvider = null;
			IServerChannelSinkProvider lastProvider = null;

			foreach (var sinkProvider in _serverSinkChain)
			{
				sinkProvider.Next = null;

				if (firstProvider == null)
					firstProvider = sinkProvider;

				if (lastProvider == null)
					lastProvider = sinkProvider;
				else
				{
					lastProvider.Next = sinkProvider;
					lastProvider = sinkProvider;
				}
			}

			return firstProvider;
		}

		/// <summary>
		/// Creates and configures a Remoting channel.
		/// </summary>
		/// <returns>Remoting channel</returns>
		public virtual IChannel CreateChannel()
		{
			IChannel channel = ChannelServices.GetChannel(_channelName);

			if (channel == null)
			{
				if (_channelFactory == null)
					throw new ApplicationException(LanguageResource.ApplicationException_NoChannelFactorySpecified);

				_channelSettings["name"] = _channelName;

				channel = _channelFactory(_channelSettings, BuildClientSinkChain(), BuildServerSinkChain());

				if (!MonoCheck.IsRunningOnMono)
				{
					if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
						RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
				}
				return channel;
			}
			return channel;
		}
	}
}
