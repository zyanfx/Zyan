using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// General implementation of server protocol setup.
	/// </summary>
	public class ServerProtocolSetup : IServerProtocolSetup
	{
		/// <summary>
		/// Unique channel name.
		/// </summary>
		protected string _channelName = "ServerProtocolSetup_" + Guid.NewGuid().ToString();

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
		/// Authentication provider.
		/// </summary>
		protected IAuthenticationProvider _authProvider = new NullAuthenticationProvider();

		/// <summary>
		/// Creates a new instance of the ServerProtocolSetupBase class.
		/// </summary>
		protected ServerProtocolSetup() { }

		/// <summary>
		/// Creates a new instance of the ServerProtocolSetup class.
		/// </summary>
		/// <param name="channelFactory">Delegate to channel factory method</param>
		public ServerProtocolSetup(Func<IDictionary, IClientChannelSinkProvider, IServerChannelSinkProvider, IChannel> channelFactory)
			: this()
		{
			if (channelFactory == null)
				throw new ArgumentNullException("channelFactory");

			_channelFactory = channelFactory;
		}

		/// <summary>
		/// Creates a new ServerProtocolSetup with a specified channel factory method.
		/// </summary>
		/// <param name="channelFactory">Delegate to channel factory method</param>
		/// <returns></returns>
		public static IServerProtocolSetup WithChannel(Func<IDictionary, IClientChannelSinkProvider, IServerChannelSinkProvider, IChannel> channelFactory)
		{
			return new ServerProtocolSetup(channelFactory);
		}

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

		/// <summary>
		/// Gets or sets the authentication provider.
		/// </summary>
		public virtual IAuthenticationProvider AuthenticationProvider
		{
			get { return _authProvider; }
			set
			{
				if (value == null)
					_authProvider = new NullAuthenticationProvider();
				else
					_authProvider = value;
			}
		}
	}
}
