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
using System.Net.Sockets;
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
		int port = 0;
		int priority;
		Guid guid = Guid.NewGuid();
		string name = "ExtendedTcp";
		TcpExChannelData channelData;
		internal ServerTransportSink messageSink;
		IClientChannelSinkProvider clientSinkProvider;

		#region Constructors
		public TcpExChannel()
		{
			Initialise(TypeFilterLevel.Low, null, null, 0, false);
		}

		public TcpExChannel(int port)
		{
			Initialise(TypeFilterLevel.Low, null, null, port, true);
		}

		public TcpExChannel(bool listen)
		{
			Initialise(TypeFilterLevel.Low, null, null, 0, listen);
		}

		public TcpExChannel(TypeFilterLevel filterLevel, bool listen)
		{
			Initialise(filterLevel, null, null, 0, listen);
		}

		public TcpExChannel(TypeFilterLevel filterLevel, int port)
		{
			Initialise(filterLevel, null, null, port, true);
		}

		public TcpExChannel(IDictionary properties, IClientChannelSinkProvider clientSinkProvider, IServerChannelSinkProvider serverSinkProvider)
		{
			int port = 0;
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
            if (properties.Contains("typeFilterLevel"))
            {
                if (properties["typeFilterLevel"] is string)
                    typeFilterLevel = (TypeFilterLevel)Enum.Parse(typeof(TypeFilterLevel), (string)properties["typeFilterLevel"]);
                else
                    typeFilterLevel = (TypeFilterLevel)properties["typeFilterLevel"];
            }
			Initialise(typeFilterLevel, clientSinkProvider, serverSinkProvider, port, listen);
		}

		void Initialise(TypeFilterLevel typeFilterLevel, IClientChannelSinkProvider clientSinkProvider, IServerChannelSinkProvider serverSinkProvider, int port, bool listen)
		{
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

			Manager.BeginReadMessage(guid, null, new AsyncCallback(messageSink.ReceiveMessage), guid);
		}
		#endregion

		internal string[] GetAddresses()
		{
			return Manager.GetAddresses(port, guid);
		}

		#region Properties
		public Guid Guid
		{
			get
			{
				return guid;
			}
		}

		public int Port
		{
			get
			{
				return port;
			}
		}

		public bool IsListening
		{
			get
			{
				return port != 0;
			}
		}
		#endregion

		#region Implementation of IChannel
		public string Parse(string url, out string objectURI)
		{
			return Manager.Parse(url, out objectURI);
		}

		public string ChannelName
		{
			get
			{
				return name;
			}
		}

		public int ChannelPriority
		{
			get
			{
				return priority;
			}
		}
		#endregion

		#region Implementation of IChannelSender
		public IMessageSink CreateMessageSink(string url, object remoteChannelData, out string objectURI)
		{
			if (url == null)
			{
				TcpExChannelData channelData = remoteChannelData as TcpExChannelData;
				if (channelData != null)
					url = Manager.CreateUrl(channelData.Guid);
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
					Manager.BeginReadMessage(url, null, new AsyncCallback(messageSink.ReceiveMessage), url);
			}
		}

		public void StopListening(object data)
		{
			throw new NotImplementedException();
		}

		public string[] GetUrlsForUri(string objectURI)
		{
			return Manager.GetUrlsForUri(objectURI, port, guid);
		}

		public object ChannelData
		{
			get
			{
				return channelData;
			}
		}
		#endregion
	}
}