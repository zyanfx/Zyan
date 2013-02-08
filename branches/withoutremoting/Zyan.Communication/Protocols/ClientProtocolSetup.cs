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
        /// Delegate to factory method, which creates the transport adapter instance.
        /// </summary>
        protected Func<IDictionary, IClientTransportAdapter> _transportAdapterFactory = null;

		/// <summary>
		/// Creates a new instance of the ClientProtocolSetup class.
		/// </summary>
		protected ClientProtocolSetup() { }

		/// <summary>
		/// Creates a new instance of the ClientProtocolSetup class.
		/// </summary>
		/// <param name="transportAdapterFactory">Delegate to transport adapter factory method</param>
		public ClientProtocolSetup(Func<IDictionary, IClientTransportAdapter> transportAdapterFactory)
		{
			if (transportAdapterFactory == null)
                throw new ArgumentNullException("transportAdapterFactory");

			_transportAdapterFactory = transportAdapterFactory;
		}

		/// <summary>
		/// Creates a new ClientProtocolSetup with a specified channel factory method.
		/// </summary>
		/// <param name="transportAdapterFactory">Delegate to transport adapter factory method</param>
		/// <returns></returns>
		public static IClientProtocolSetup WithTransportAdapter(Func<IDictionary, IClientTransportAdapter> transportAdapterFactory)
		{
			return new ClientProtocolSetup(transportAdapterFactory);
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
		/// Creates and configures a transport adapter.
		/// </summary>
		/// <returns>Transport adapter</returns>
        public virtual IClientTransportAdapter CreateTransportAdapter()
		{
			IClientTransportAdapter channel = ClientTransportAdapterManager.Instance.GetTransportAdapter(_channelName);

			if (channel == null)
			{
				if (_transportAdapterFactory == null)
					throw new ApplicationException(LanguageResource.ApplicationException_NoChannelFactorySpecified);

				_channelSettings["name"] = _channelName;

				channel = _transportAdapterFactory(_channelSettings);
				return channel;
			}
			return channel;
		}
	}
}
