using System;
using System.Net;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	[Serializable]
	public class TcpExChannelData : IChannelDataStore
	{
		int port;
		Guid guid;
		ArrayList addresses; // ArrayList<string>
		HybridDictionary properties;
		[NonSerialized]
		string[] channelUris;

		public TcpExChannelData(TcpExChannel channel)
		{
			this.port = channel.Port;
			this.guid = channel.Guid;

			if (port != 0)
			{
				addresses = new ArrayList();
				IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
				foreach (IPAddress address in hostEntry.AddressList)
					addresses.Add(string.Format("{0}:{1}", address, port));
			}
		}
		
		#region Properties
		public int Port
		{
			get
			{
				return port;
			}
		}

		public Guid Guid
		{
			get
			{
				return guid;
			}

			set
			{
				guid = value;
			}
		}

		public IList Addresses // IList<string>
		{
			get
			{
				return addresses;
			}
		}
		#endregion

		#region IChannelDataStore Members
		public object this[object key]
		{
			get
			{
				if (properties == null)
					return null;
				return properties[key];
			}

			set
			{
				if (properties == null)
					properties = new HybridDictionary();
				properties[key] = value;
			}
		}

		public string[] ChannelUris
		{
			get
			{
				if (channelUris == null)
					channelUris = Manager.GetUrlsForUri(null, port, guid);
				return channelUris;
			}
		}
		#endregion
	}
}