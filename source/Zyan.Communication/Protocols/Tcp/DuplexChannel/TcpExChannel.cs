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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.Toolbox.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	/// <summary name="TcpExChannel">
	/// A replacement for the standard Tcp remoting channel that allows communication in both directions over a single tcp connection.
	/// </summary>
	/// <remarks>
	/// TcpExChannel only supports IPv4.
	/// <b>Remoting Configuration Parameters:</b>
	/// <list type="bullet">
	/// <item><term>port</term><description>The tcp port the channel should listen on.  If this is specified, the channel will automatically start listening on that port.</description></item>
	/// <item><term>listen</term><description>Indicates the channel should start listening.  This is not required if the port parameter is specified.  If no port is specified the channel will choose a random unused port.</description></item>
	/// <item><term>bufferSize</term><description>The size of the buffer to use when sending data over a connection.</description></item>
	/// <item><term>priority</term><description>The priority of the channel.</description></item>
	/// </list>
	/// </remarks>
	public class TcpExChannel : IChannel, IChannelSender, IChannelReceiver, IConnectionNotification, IDisposable
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
		private IPAddress _bindToAddress = IPAddress.Any;
		
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

		/// <summary>
		/// Gets or sets the value indicating whether the client-side should connect
		/// to the server during the creation of the transport channel.
		/// </summary>
		public bool ConnectDuringCreation
		{
			get; set;
		}

		#endregion

		#region Constructors, initialization

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpExChannel"/> class with default settings (client mode).
		/// </summary>
		public TcpExChannel()
		{
			Initialise(TypeFilterLevel.Low, null, null, 0, false, true, 30000, 1000, 10, 1000, IPAddress.Any);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpExChannel"/> class (server mode).
		/// </summary>
		/// <param name="port">Tcp port.</param>
		public TcpExChannel(int port)
		{
			Initialise(TypeFilterLevel.Low, null, null, port, true, true, 30000, 1000, 10, 1000, IPAddress.Any);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpExChannel"/> class.
		/// </summary>
		/// <param name="listen">if set to <c>true</c>, the channel will listen for incoming connections.</param>
		public TcpExChannel(bool listen)
		{
			Initialise(TypeFilterLevel.Low, null, null, 0, listen, true, 30000, 1000, 10, 1000, IPAddress.Any);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpExChannel"/> class.
		/// </summary>
		/// <param name="filterLevel">The type filter level.</param>
		/// <param name="listen">if set to <c>true</c>, the channel will listen for incoming connections.</param>
		public TcpExChannel(TypeFilterLevel filterLevel, bool listen)
		{
			Initialise(filterLevel, null, null, 0, listen, true, 30000, 1000, 10, 1000, IPAddress.Any);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpExChannel"/> class.
		/// </summary>
		/// <param name="filterLevel">The type filter level.</param>
		/// <param name="port">Tcp port.</param>
		public TcpExChannel(TypeFilterLevel filterLevel, int port)
		{
			Initialise(filterLevel, null, null, port, true, true, 30000, 1000,10,1000, IPAddress.Any);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpExChannel"/> class.
		/// </summary>
		/// <param name="properties">Channel initialization properties. <see cref="TcpExChannel"/></param>
		/// <param name="clientSinkProvider">The client sink provider.</param>
		/// <param name="serverSinkProvider">The server sink provider.</param>
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
			IPAddress bindToAddress = IPAddress.Any;

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
			if (properties.Contains("bindTo"))
				bindToAddress = IPAddress.Parse((string)properties["bindTo"]);
			if (properties.Contains("connectDuringCreation"))
				ConnectDuringCreation = Convert.ToBoolean(properties["connectDuringCreation"]);
			if (properties.Contains("typeFilterLevel"))
			{
				if (properties["typeFilterLevel"] is string)
					typeFilterLevel = (TypeFilterLevel)Enum.Parse(typeof(TypeFilterLevel), (string)properties["typeFilterLevel"]);
				else
					typeFilterLevel = (TypeFilterLevel)properties["typeFilterLevel"];
			}
			Initialise(typeFilterLevel, clientSinkProvider, serverSinkProvider, port, listen, tcpKeepAliveEnabled, tcpKeepAliveTime, tcpKeepAliveInterval, maxRetries, retryDelay, bindToAddress);
		}

		private void Initialise(TypeFilterLevel typeFilterLevel, IClientChannelSinkProvider clientSinkProvider, IServerChannelSinkProvider serverSinkProvider, int port, bool listen, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval, short maxRetries, int retryDelay, IPAddress bindToAddress)
		{
			_tcpKeepAliveEnabled = keepAlive;
			_tcpKeepAliveTime = keepAliveTime;
			_tcpKeepAliveInterval = KeepAliveInterval;
			_maxRetries = maxRetries;
			_retryDelay = retryDelay;
			_bindToAddress = bindToAddress;

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

			messageSink = new ServerTransportSink(ChannelServices.CreateServerChannelSinkChain(serverSinkProvider, this));
			serverSinkProvider.GetChannelData(channelData);

			if (listen)
			{
				StartListening(port);
			}

			channelData = new TcpExChannelData(this);

			Manager.BeginReadMessage(_channelID, null, new AsyncCallback(messageSink.ReceiveMessage), _channelID);
		}

		/// <summary>
		/// Unregisters all running connections of the current <see cref="TcpExChannel"/> instance.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Releases unmanaged and — optionally — managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Connection.UnregisterConnectionsOfChannel(this);
			}
		}

		internal string[] GetAddresses()
		{
			return Manager.GetAddresses(port, _channelID, true);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the unique identifier of the channel.
		/// </summary>
		public Guid ChannelID
		{
			get { return _channelID; }
		}

		/// <summary>
		/// Gets the Tcp port.
		/// </summary>
		public int Port
		{
			get { return port; }
		}

		/// <summary>
		/// Gets a value indicating whether this channel is listening to incoming connections.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is listening to incoming connections; otherwise, <c>false</c>.
		/// </value>
		public bool IsListening
		{
			get { return port != 0;	}
		}

		/// <summary>
		/// Gets or sets the maximum number of connection retry attempts.
		/// </summary>
		public short MaxRetries
		{
			get { return _maxRetries; }
		}

		/// <summary>
		/// Gets or sets the delay after a retry attempt in milliseconds.
		/// </summary>
		public int RetryDelay
		{
			get { return _retryDelay; }
		}

		#endregion

		#region Implementation of IChannel

		/// <summary>
		/// Returns the object URI as an out parameter, and the URI of the current channel as the return value.
		/// </summary>
		/// <param name="url">The URL of the object.</param>
		/// <param name="objectURI">When this method returns, contains a <see cref="T:System.String"/> that holds the object URI. This parameter is passed uninitialized.</param>
		/// <returns>
		/// The URI of the current channel, or null if the URI does not belong to this channel.
		/// </returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public string Parse(string url, out string objectURI)
		{
			return Manager.Parse(url, out objectURI);
		}

		/// <summary>
		/// Gets the name of the channel.
		/// </summary>
		/// <returns>The name of the channel.</returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		/// <PermissionSet>
		///  <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="Infrastructure"/>
		/// </PermissionSet>
		public string ChannelName
		{
			get { return name; }
		}

		/// <summary>
		/// Gets the priority of the channel.
		/// </summary>
		/// <returns>An integer that indicates the priority of the channel.</returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		/// <PermissionSet>
		///  <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="Infrastructure"/>
		/// </PermissionSet>
		public int ChannelPriority
		{
			get { return priority; }
		}

		#endregion

		#region Implementation of IChannelSender

		/// <summary>
		/// Returns a channel message sink that delivers messages to the specified URL or channel data object.
		/// </summary>
		/// <param name="url">The URL to which the new sink will deliver messages. Can be null.</param>
		/// <param name="remoteChannelData">The channel data object of the remote host to which the new sink will deliver messages. Can be null.</param>
		/// <param name="objectURI">When this method returns, contains a URI of the new channel message sink that delivers messages to the specified URL or channel data object. This parameter is passed uninitialized.</param>
		/// <returns>
		/// A channel message sink that delivers messages to the specified URL or channel data object, or null if the channel cannot connect to the given endpoint.
		/// </returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public IMessageSink CreateMessageSink(string url, object remoteChannelData, out string objectURI)
		{
			objectURI = null;

			if (url == null)
			{
				TcpExChannelData channelData = remoteChannelData as TcpExChannelData;
				if (channelData != null)
				{
					url = Manager.CreateUrl(LocalCallContextData.GetData("RemoteChannelID", channelData.ChannelID));
				}
				else
					return null;
			}
			if (Manager.Parse(url, out objectURI) != null)
			{
				IClientChannelSink clientChannelSink = clientSinkProvider.CreateSink(this, url, remoteChannelData);
				IMessageSink messageSink = clientChannelSink as IMessageSink;

				if (clientChannelSink != null && messageSink == null)
					throw new RemotingException(LanguageResource.RemotingException_MessageSinkNotSet);

				return messageSink;
			}
			else
				return null;
		}

		#endregion

		#region Implementation of IChannelReceiver

		/// <summary>
		/// Instructs the current channel to start listening for requests.
		/// </summary>
		/// <param name="data">Optional initialization information.</param>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public void StartListening(object data)
		{
			if (port != 0)
				throw new InvalidOperationException("Channel is already listening. TcpEx currently only allows listening on one port.");

			if (data is int)
			{
				port = (int)data;
				channelData = new TcpExChannelData(this);
				ListenerSocket = Manager.StartListening(port, this, _bindToAddress);
				ListenerAddresses = Manager.GetAddresses(port, Guid.Empty, true);

				foreach (string url in ListenerAddresses)
				{
					Manager.BeginReadMessage(url, null, new AsyncCallback(messageSink.ReceiveMessage), url);
				}
			}
		}

		/// <summary>
		/// Gets or sets the listener socket.
		/// </summary>
		private Socket ListenerSocket { get; set; }

		/// <summary>
		/// Gets or sets the addresses this channel is listening to.
		/// </summary>
		private string[] ListenerAddresses { get; set; }

		/// <summary>
		/// Instructs the current channel to stop listening for requests.
		/// </summary>
		/// <param name="data">Optional state information for the channel.</param>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public void StopListening(object data)
		{
			try
			{
				Manager.StopListening(this, ListenerAddresses);
			}
			finally
			{
				var listener = ListenerSocket;
				ListenerSocket = null;
				if (listener != null)
				{
					listener.Close();
				}
			}
        }

		/// <summary>
		/// Returns an array of all the URLs for a URI.
		/// </summary>
		/// <param name="objectURI">The URI for which URLs are required.</param>
		/// <returns>
		/// An array of the URLs.
		/// </returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public string[] GetUrlsForUri(string objectURI)
		{
			return Manager.GetUrlsForUri(objectURI, port, _channelID);
		}

		/// <summary>
		/// Gets the channel-specific data.
		/// </summary>
		/// <returns>The channel data.</returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="Infrastructure"/>
		/// </PermissionSet>
		public object ChannelData
		{
			get { return channelData; }
		}

		#endregion

		#region Implementation of IConnectionNotification

		/// <summary>
		/// Occurs when connection is established or restored.
		/// </summary>
		public event EventHandler ConnectionEstablished;

		/// <summary>
		/// Raises the <see cref="E:ConnectionEstablished" /> event.
		/// </summary>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		protected internal void OnConnectionEstablished(EventArgs e)
		{
			var connectionEstablished = ConnectionEstablished;
			if (connectionEstablished != null)
			{
				ConnectionEstablished(this, e);
			}
		}

		#endregion
	}
}