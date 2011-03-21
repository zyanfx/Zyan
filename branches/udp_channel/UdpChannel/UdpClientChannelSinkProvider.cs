using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.Protocols.Udp
{
	/// <summary>
	/// UDP client channel sink provider
	/// </summary>
	public class UdpClientChannelSinkProvider : IClientChannelSinkProvider
	{
		public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
		{
			return new UdpClientChannelSink(url);
		}

		public IClientChannelSinkProvider Next
		{
			get { return null; }
			set { throw new NotSupportedException(); }
		}
	}
}
