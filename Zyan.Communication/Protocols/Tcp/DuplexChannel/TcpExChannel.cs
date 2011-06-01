/*
 THIS CODE IS BASED ON:
 -------------------------------------------------------------------------------------------------------------- 
 TcpEx Remoting Channel
 Version 1.2 - 18 November, 2003
 Richard Mason - r.mason@qut.edu.au
 Originally published at GotDotNet:
 http://www.gotdotnet.com/Community/UserSamples/Details.aspx?SampleGuid=3F46C102-9970-48B1-9225-8758C38905B1
 Copyright © 2003 Richard Mason. All Rights Reserved. 
 --------------------------------------------------------------------------------------------------------------
*/
using System;
using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.Protocols.Tcp.DuplexChannel.Diagnostics;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	/// <include file='TcpExChannel.docs' path='TcpExChannel/summary[@name="TcpExChannel"]'/>
	public class TcpExChannel : IChannel, IChannelSender, IChannelReceiver
	{
		private int port = 0;
		private int priority;
		private Guid _channelID = Guid.NewGuid();
		private string name = "ExtendedTcp";
		private TcpExChannelData channelData;
		internal ServerTransportSink messageSink;
		private IClientChannelSinkProvider clientSinkProvider;
        private bool _tcpKeepAliveEnabled = true;
        private ulong _tcpKeepAliveTime = 30000;
        private ulong _tcpKeepAliveInterval = 1000;
        private short _maxRetries = 10;
        private int _retryDelay = 1000;

        #region TCP KeepAlive

        /// <summary>
        /// Enables or disables TCP KeepAlive.        
        /// </summary>
        public bool TcpKeepAliveEnabled
        {
            get { return _tcpKeepAliveEnabled; }            
        }

        /// <summary>
        /// Gets or sets the TCP KeepAlive time in milliseconds.
        /// </summary>
        public ulong TcpKeepAliveTime
        {
            get { return _tcpKeepAliveTime; }            
        }

        /// <summary>
        /// Gets or sets the TCP KeepAlive interval in milliseconds
        /// </summary>
        public ulong TcpKeepAliveInterval
        {
            get { return _tcpKeepAliveInterval; }            
        }

        #endregion

        #region Constructors

        public TcpExChannel()
		{
            Initialise(TypeFilterLevel.Low, null, null, 0, false, true, 30000, 1000, 10, 1000);
		}

		public TcpExChannel(int port)
		{
            Initialise(TypeFilterLevel.Low, null, null, port, true, true, 30000, 1000, 10, 1000);
		}

		public TcpExChannel(bool listen)
		{
            Initialise(TypeFilterLevel.Low, null, null, 0, listen, true, 30000, 1000, 10, 1000);
		}

		public TcpExChannel(TypeFilterLevel filterLevel, bool listen)
		{
            Initialise(filterLevel, null, null, 0, listen, true, 30000, 1000, 10, 1000);
		}

		public TcpExChannel(TypeFilterLevel filterLevel, int port)
		{
			Initialise(filterLevel, null, null, port, true, true, 30000, 1000,10,1000);
		}

		public TcpExChannel(IDictionary properties, IClientChannelSinkProvider clientSinkProvider, IServerChannelSinkProvider serverSinkProvider)
		{
			int port = 0;
            bool tcpKeepAliveEnabled = true;
            ulong tcpKeepAliveTime = 30000;
            ulong tcpKeepAliveInterval = 1000;
            short maxRetries = 10;
            int retryDelay = 1000;
			bool listen = false;
			TypeFilterLevel typeFilterLevel = TypeFilterLevel.Low;

			if (properties.Contains("port"))
			{
				port = Convert.ToInt32(properties["port"]);
				listen = true;
			}
			if (properties.Contains("priority"))
				priority = Convert.ToInt32(properties["priority"]);
			if (properties.Contains("name"))
				name = (string)properties["name"];
			if (properties.Contains("listen"))
				listen = Convert.ToBoolean(properties["listen"]);
			if (properties.Contains("bufferSize"))
				Connection.BufferSize = Convert.ToInt32(properties["bufferSize"]);
            if (properties.Contains("keepAlive"))
                tcpKeepAliveEnabled = Convert.ToBoolean(properties["keepAlive"]);
            if (properties.Contains("keepAliveEnabled"))
                tcpKeepAliveEnabled = Convert.ToBoolean(properties["keepAliveEnabled"]);
            if (properties.Contains("keepAliveTime"))
                tcpKeepAliveTime = Convert.ToUInt64(properties["keepAliveTime"]);
            if (properties.Contains("keepAliveInterval"))
                tcpKeepAliveInterval = Convert.ToUInt64(properties["keepAliveInterval"]);
            if (properties.Contains("maxRetries"))
                maxRetries = Convert.ToInt16(properties["maxRetries"]);
            if (properties.Contains("retryDelay"))
                retryDelay = Convert.ToInt32(properties["retryDelay"]);
            if (properties.Contains("typeFilterLevel"))
            {
                if (properties["typeFilterLevel"] is string)
                    typeFilterLevel = (TypeFilterLevel)Enum.Parse(typeof(TypeFilterLevel), (string)properties["typeFilterLevel"]);
                else
                    typeFilterLevel = (TypeFilterLevel)properties["typeFilterLevel"];
            }
			Initialise(typeFilterLevel, clientSinkProvider, serverSinkProvider, port, listen, tcpKeepAliveEnabled, tcpKeepAliveTime, tcpKeepAliveInterval, maxRetries, retryDelay);
		}

        private void Initialise(TypeFilterLevel typeFilterLevel, IClientChannelSinkProvider clientSinkProvider, IServerChannelSinkProvider serverSinkProvider, int port, bool listen, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval, short maxRetries, int retryDelay)
		{
            _tcpKeepAliveEnabled = keepAlive;
            _tcpKeepAliveTime = keepAliveTime;
            _tcpKeepAliveInterval = KeepAliveInterval;
            _maxRetries = maxRetries;
            _retryDelay = retryDelay;

			if (clientSinkProvider == null)
				clientSinkProvider = new BinaryClientFormatterSinkProvider();
			if (serverSinkProvider == null)
			{
				Trace.WriteLine("Setting serialization filter: {0}", typeFilterLevel);
				BinaryServerFormatterSinkProvider tempProvider = new BinaryServerFormatterSinkProvider();
				tempProvider.TypeFilterLevel = typeFilterLevel;
				serverSinkProvider = tempProvider;
			}

			// Initialise clientSinkProvider
			this.clientSinkProvider = clientSinkProvider;
			while (clientSinkProvider.Next != null)
				clientSinkProvider = clientSinkProvider.Next;
			clientSinkProvider.Next = new ClientTransportSinkProvider();

			messageSink = new ServerTransportSink(ChannelServices.CreateServerChannelSinkChain(serverSinkProvider, this), this);
			serverSinkProvider.GetChannelData(channelData);

			if (listen)
			{	
				StartListening(port);

				channelData = new TcpExChannelData(this);
			}
			else
				channelData = new TcpExChannelData(this);

			Manager.BeginReadMessage(_channelID, null, new AsyncCallback(messageSink.ReceiveMessage), _channelID);
		}
		
        #endregion

		internal string[] GetAddresses()
		{
			return Manager.GetAddresses(port, _channelID);
		}

		#region Properties
		
        public Guid ChannelID
		{
			get { return _channelID; }
		}

		public int Port
		{
			get { return port; }
		}

		public bool IsListening
		{
			get { return port != 0;	}
		}

        public short MaxRetries
        {
            get { return _maxRetries; }
        }

        public int RetryDelay
        {
            get { return _retryDelay; }
        }

        #endregion

		#region Implementation of IChannel

		public string Parse(string url, out string objectURI)
		{
			return Manager.Parse(url, out objectURI);
		}

		public string ChannelName
		{
			get { return name; }
		}

		public int ChannelPriority
		{
			get { return priority; }
		}

		#endregion

		#region Implementation of IChannelSender

		public IMessageSink CreateMessageSink(string url, object remoteChannelData, out string objectURI)
		{
			if (url == null)
			{
				TcpExChannelData channelData = remoteChannelData as TcpExChannelData;
				if (channelData != null)
					url = Manager.CreateUrl(channelData.ChannelID);
				else
				{
					objectURI = null;
					return null;
				}
			}

			if (Manager.Parse(url, out objectURI) != null)
				return (IMessageSink)clientSinkProvider.CreateSink(this, url, remoteChannelData);
			else
				return null;
		}

		#endregion

		#region Implementation of IChannelReceiver

		public void StartListening(object data)
		{
			if (this.port != 0)
				throw new InvalidOperationException("Channel is already listening.  TcpEx currently only allows listening on one port.");

			if (data is int)
			{
				this.port = Manager.StartListening((int)data, this);
				channelData = new TcpExChannelData(this);

                foreach (string url in Manager.GetAddresses(this.port, Guid.Empty))
                {
                    Manager.BeginReadMessage(url, null, new AsyncCallback(messageSink.ReceiveMessage), url);
                }
			}
		}

		public void StopListening(object data)
		{
            Manager.StopListening(this);
		}

		public string[] GetUrlsForUri(string objectURI)
		{
			return Manager.GetUrlsForUri(objectURI, port, _channelID);
		}

		public object ChannelData
		{
			get { return channelData; }
		}

		#endregion
	}
}