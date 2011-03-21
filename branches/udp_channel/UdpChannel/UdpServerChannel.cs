using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Collections;
using System.Net;
using System.Threading;

namespace Zyan.Communication.Protocols.Udp
{
	/// <summary>
	/// UDP server channel (receiver)
	/// </summary>
	public class UdpServerChannel : IChannelReceiver
	{
		public string ChannelName { get; private set; }

		public int ChannelPriority { get; private set; }

		public object ChannelData { get { return ChannelDataStore; } }

		private IServerChannelSinkProvider ServerSinkProvider { get; set; }

		private UdpServerChannelSink ServerChannelSink { get; set; }

		private ChannelDataStore ChannelDataStore { get; set; }

		private string MachineName { get; set; }

		private int Port { get; set; }

		private Thread ServerThread { get; set; }

		public UdpServerChannel()
			: this(null, null)
		{ 
		}

		public UdpServerChannel(IDictionary properties, IServerChannelSinkProvider provider)
		{
			ChannelName = properties.GetValue("name", UdpChannelHelper.DefaultName);
			ChannelPriority = properties.GetValue("priority", UdpChannelHelper.DefaultPriority);
			MachineName = properties.GetValue("machineName", Dns.GetHostName());
			Port = properties.GetValue("port", UdpChannelHelper.DefaultPort);
			ChannelDataStore = UdpChannelHelper.CreateChannelDataStore(MachineName, Port);
			ServerSinkProvider = provider ?? UdpChannelHelper.CreateServerSinkProvider();
			SetupServerSinkChain(ServerSinkProvider);
			StartListening(null);
		}

		void SetupServerSinkChain(IServerChannelSinkProvider providerChain)
		{
			// collect channel data
			var provider = providerChain;
			while (provider != null)
			{
				provider.GetChannelData(ChannelDataStore);
				provider = provider.Next;
			}

			// setup server sink chain
			var next = ChannelServices.CreateServerChannelSinkChain(providerChain, this);
			ServerChannelSink = new UdpServerChannelSink(next);
		}

		public string[] GetUrlsForUri(string objectUri)
		{
			if (!objectUri.StartsWith("/"))
			{
				objectUri = "/" + objectUri;
			}

			var urls = new List<String>();
			foreach (var url in ChannelDataStore.ChannelUris)
			{
				urls.Add(url + objectUri);
			}

			return urls.ToArray();
		}

		public void StartListening(object data)
		{
			ServerThread = new Thread(() =>
			{
				ServerChannelSink.Listen(Port);
			});

			ServerThread.IsBackground = true;
			ServerThread.Start();
		}

		public void StopListening(object data)
		{
			if (ServerThread != null)
			{
				ServerThread.Abort();
				ServerThread = null;
			}
		}

		public string Parse(string url, out string objectUri)
		{
			return UdpChannelHelper.Parse(url, out objectUri);
		}
	}
}
