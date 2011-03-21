using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;

namespace Zyan.Communication.Protocols.Udp
{
	/// <summary>
	/// UDP client channel (sender)
	/// </summary>
	public class UdpClientChannel : IChannelSender
	{
		public string ChannelName { get; private set; }

		public int ChannelPriority { get; private set; }

		IClientChannelSinkProvider ClientSinkProvider { get; set; }

		public UdpClientChannel()
			: this(null, null)
		{
		}

		public UdpClientChannel(IDictionary properties, IClientChannelSinkProvider provider)
		{
			ChannelName = properties.GetValue("name", UdpChannelHelper.DefaultName);
			ChannelPriority = properties.GetValue("priority", UdpChannelHelper.DefaultPriority);
			ClientSinkProvider = provider ?? UdpChannelHelper.CreateClientSinkProvider();
			SetupClientProviderChain(ClientSinkProvider, new UdpClientChannelSinkProvider());
		}

		void SetupClientProviderChain(IClientChannelSinkProvider clientChain, IClientChannelSinkProvider provider)
		{
			while (clientChain.Next != null)
			{
				clientChain = clientChain.Next;
			}

			clientChain.Next = provider;
		}

		public IMessageSink CreateMessageSink(string url, object remoteChannelData, out string objectUri)
		{
			objectUri = null;

			// is wellknown object url specified?
			if (url == null)
			{
				// client-activated object url is specified in the channel data store
				var dataStore = remoteChannelData as IChannelDataStore;
				if (dataStore == null || dataStore.ChannelUris.Length < 1)
				{
					return null;
				}

				url = dataStore.ChannelUris[0];
			}

			// validate url
			if (url != null && Parse(url, out objectUri) != null)
			{
				return (IMessageSink)ClientSinkProvider.CreateSink(this, url, remoteChannelData);
			}

			return null;
		}

		public string Parse(string url, out string objectURI)
		{
			return UdpChannelHelper.Parse(url, out objectURI);
		}
	}
}
