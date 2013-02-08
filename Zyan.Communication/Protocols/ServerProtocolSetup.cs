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
		/// Delegate to factory method, which creates the .NET Remoting channel instance.
		/// </summary>
		protected Func<IDictionary, IServerTransportAdapter> _channelFactory = null;

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
		/// <param name="transportAdapterFactory">Delegate to transport adapter factory method</param>
        public ServerProtocolSetup(Func<IDictionary, IServerTransportAdapter> transportAdapterFactory)
			: this()
		{
			if (transportAdapterFactory == null)
				throw new ArgumentNullException("channelFactory");

			_channelFactory = transportAdapterFactory;
		}

		/// <summary>
		/// Creates a new ServerProtocolSetup with a specified channel factory method.
		/// </summary>
		/// <param name="transportAdapterFactory">Delegate to transport adapter factory method</param>
		/// <returns></returns>
		public static IServerProtocolSetup WithChannel(Func<IDictionary, IServerTransportAdapter> transportAdapterFactory)
		{
			return new ServerProtocolSetup(transportAdapterFactory);
		}

		/// <summary>
		/// Gets a dictionary with channel settings.
		/// </summary>
		public virtual Dictionary<string, object> ChannelSettings { get { return _channelSettings; } }

        /// <summary>
        /// Creates and configures a transport adapter.
        /// </summary>
        /// <returns>Transport adapter</returns>
		public virtual IServerTransportAdapter CreateTransportAdapter()
		{
            var channel = ServerTransportAdapterManager.Instance.GetTransportAdapter(_channelName);

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
