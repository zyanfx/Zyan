using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Collections;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication.Protocols.Udp
{
	/// <summary>
	/// UDP channel common facade
	/// </summary>
	public class UdpChannel : IChannel, IChannelSender, IChannelReceiver
	{
		UdpServerChannel ServerChannel { get; set; }

		UdpClientChannel ClientChannel { get; set; }

		public UdpChannel()
		{
			ClientChannel = new UdpClientChannel();
		}

		public UdpChannel(int port)
		{
			var props = new Hashtable();
			props["port"] = port;
			ServerChannel = new UdpServerChannel(props, null);
		}

		public UdpChannel(IDictionary properties, IClientChannelSinkProvider clientChain, IServerChannelSinkProvider serverChain)
		{
			if (serverChain != null || (properties != null && properties.Contains("port")))
			{
				ServerChannel = new UdpServerChannel(properties, serverChain);
			}
			else
			{
				ClientChannel = new UdpClientChannel(properties, clientChain);
			}
		}

		public string ChannelName
		{
			get
			{
				return ClientChannel != null ? ClientChannel.ChannelName : ServerChannel.ChannelName;
			}
		}

		public int ChannelPriority
		{
			get
			{
				return ClientChannel != null ? ClientChannel.ChannelPriority : ServerChannel.ChannelPriority;
			}
		}

		public string Parse(string url, out string objectURI)
		{
			return UdpChannelHelper.Parse(url, out objectURI);
		}

		public IMessageSink CreateMessageSink(string url, object remoteChannelData, out string objectUri)
		{
			objectUri = null;
			return ClientChannel != null ? ClientChannel.CreateMessageSink(url, remoteChannelData, out objectUri) : null;
		}

		public object ChannelData
		{
			get { return ServerChannel != null ? ServerChannel.ChannelData : new ChannelDataStore(null); }
		}

		public string[] GetUrlsForUri(string objectUri)
		{
			return ServerChannel != null ? ServerChannel.GetUrlsForUri(objectUri) : new string[0];
		}

		public void StartListening(object data)
		{
			if (ServerChannel != null)
			{
				ServerChannel.StartListening(data);
			}
		}

		public void StopListening(object data)
		{
			if (ServerChannel != null)
			{
				ServerChannel.StopListening(data);
			}
		}
	}
}
