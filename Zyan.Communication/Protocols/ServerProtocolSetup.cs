using System;
using System.Collections;
using System.Collections.Generic;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Transport;

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
        protected List<ISendPipelineStage> _sendPipeline = new List<ISendPipelineStage>();

		/// <summary>
		/// List for building the server sink chain.
		/// </summary>
        protected List<IReceivePipelineStage> _receivePipeline = new List<IReceivePipelineStage>();

		/// <summary>
		/// Delegate to factory method, which creates the .NET Remoting channel instance.
		/// </summary>
		protected Func<IDictionary, IZyanTransportChannel> _channelFactory = null;

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
        public ServerProtocolSetup(Func<IDictionary, IZyanTransportChannel> channelFactory)
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
		public static IServerProtocolSetup WithChannel(Func<IDictionary, IZyanTransportChannel> channelFactory)
		{
			return new ServerProtocolSetup(channelFactory);
		}

		/// <summary>
		/// Gets a dictionary with channel settings.
		/// </summary>
		public virtual Dictionary<string, object> ChannelSettings { get { return _channelSettings; } }

        /// <summary>
        /// Gets a list of all stages of the send pipeline.
        /// </summary>
		public virtual List<ISendPipelineStage> SendPipeline { get { return _sendPipeline; } }

        /// <summary>
        /// Gets a list of all stages of the receive pipeline.
        /// </summary>
		public virtual List<IReceivePipelineStage> ReceivePipeline { get { return _receivePipeline; } }

        /// <summary>
        /// Creates and configures a transport channel.
        /// </summary>
        /// <returns>Transport channel</returns>
		public virtual IZyanTransportChannel CreateChannel()
		{
            IZyanTransportChannel channel = TransportChannelManager.Instance.GetChannel(_channelName);

			if (channel == null)
			{
				if (_channelFactory == null)
					throw new ApplicationException(LanguageResource.ApplicationException_NoChannelFactorySpecified);

				_channelSettings["name"] = _channelName;

				channel = _channelFactory(_channelSettings);
                				
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
