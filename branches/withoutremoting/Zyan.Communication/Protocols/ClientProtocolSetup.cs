using System;
using System.Collections;
using System.Collections.Generic;
//using Zyan.Communication.Protocols.Http;
//using Zyan.Communication.Protocols.Ipc;
//using Zyan.Communication.Protocols.Null;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Transport;

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
                //TODO: Implement transport protocols without .NET Remoting.

				//{ "tcpex://", new Lazy<IClientProtocolSetup>(() => new TcpDuplexClientProtocolSetup(), true) },
                //{ "tcp://", new Lazy<IClientProtocolSetup>(() => new TcpBinaryClientProtocolSetup(), true) },
                //{ "ipc://", new Lazy<IClientProtocolSetup>(() => new IpcBinaryClientProtocolSetup(), true) },
                //{ "http://", new Lazy<IClientProtocolSetup>(() => new HttpCustomClientProtocolSetup(), true) },
                //{ "null://", new Lazy<IClientProtocolSetup>(() => new NullClientProtocolSetup(), true) }
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
		/// Creates a new instance of the ClientProtocolSetup class.
		/// </summary>
		protected ClientProtocolSetup() { }

		/// <summary>
		/// Creates a new instance of the ClientProtocolSetup class.
		/// </summary>
		/// <param name="channelFactory">Delegate to channel factory method</param>
		public ClientProtocolSetup(Func<IDictionary, IZyanTransportChannel> channelFactory)
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
		public static IClientProtocolSetup WithChannel(Func<IDictionary, IZyanTransportChannel> channelFactory)
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
        /// Gets a list of all stages of the send pipeline.
        /// </summary>
        public virtual List<ISendPipelineStage> SendPipeline { get { return _sendPipeline; } }

        /// <summary>
        /// Gets a list of all stages of the receive pipeline.
        /// </summary>
        public virtual List<IReceivePipelineStage> ReceivePipeline { get { return _receivePipeline; } }

		/// <summary>
		/// Creates and configures a Remoting channel.
		/// </summary>
		/// <returns>Remoting channel</returns>
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
	}
}
