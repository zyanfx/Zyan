using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Net;

namespace Zyan.Communication.Protocols.Udp
{
	/// <summary>
	/// UDP client transport sink
	/// </summary>
	public class UdpClientChannelSink : IClientChannelSink
	{
		public UdpClientChannelSink(string url)
		{
			Port = UdpChannelHelper.DefaultPort;

			// get port from url
			var uri = new Uri(url);
			if (uri.Port >= 0)
			{
				Port = uri.Port;
			}

			// create Udp client and establish connection to the remote host
			foreach (var ip in Dns.GetHostEntry(uri.Host).AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					UdpClient = new UdpClient(AddressFamily.InterNetwork);
					ServerEndpoint = new IPEndPoint(ip, Port);
				}
			}
		}

		int Port { get; set; }

		UdpClient UdpClient { get; set; }

		IPEndPoint ServerEndpoint { get; set; }

		public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// add message Uri to headers
			var mcm = (IMethodCallMessage)msg;
			requestHeaders[CommonTransportKeys.RequestUri] = mcm.Uri;

			// send data and receive reply (FIXME: 1) add reliability, 2) handle exceptions)
			IPEndPoint remote;
			var transport = new UdpTransport(UdpClient);
			transport.Write(requestHeaders, requestStream, ServerEndpoint);
			transport.Read(out responseHeaders, out responseStream, out remote);
		}

		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders requestHeaders, Stream requestStream)
		{
			// add message Uri to headers
			var mcm = (IMethodCallMessage)msg;
			requestHeaders[CommonTransportKeys.RequestUri] = mcm.Uri;

			// send data (FIXME: 1) add reliability, 2) handle exceptions)
			var transport = new UdpTransport(UdpClient);
			transport.Write(requestHeaders, requestStream, ServerEndpoint);

			// if the call is not one-way, schedule an async call
			if (!RemotingServices.IsOneWay(mcm.MethodBase))
			{
				ThreadPool.QueueUserWorkItem((s) =>
				{
					try
					{
						ITransportHeaders responseHeaders;
						Stream responseStream;
						IPEndPoint remote;
						transport.Read(out responseHeaders, out responseStream, out remote);
						sinkStack.AsyncProcessResponse(responseHeaders, responseStream);
					}
					catch (Exception ex)
					{
						sinkStack.DispatchException(ex);
					}
				});
			}
		}

		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
		{
			// we are the last in the chain, so we don't need to implement this
			throw new NotSupportedException();
		}

		public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
		{
			// we don't need this
			return null;
		}

		public IClientChannelSink NextChannelSink
		{
			get { return null; } // we're always the last in the chain
		}

		public IDictionary Properties
		{
			get { return null; } // we don't have properties
		}
	}
}
