using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.IO;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Zyan.Communication.Protocols.Udp
{
	/// <summary>
	/// UDP server transport sink
	/// </summary>
	public class UdpServerChannelSink : IServerChannelSink
	{
		public IServerChannelSink NextChannelSink { get; private set; }

		public UdpServerChannelSink(IServerChannelSink next)
		{
			NextChannelSink = next;
		}

		public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			throw new NotSupportedException();
		}

		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers)
		{
			return null;
		}

		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			throw new NotSupportedException();
		}

		public IDictionary Properties
		{
			get { return null; }
		}

		internal void Listen(int port)
		{
			var udpClient = new UdpClient(port);

			while (true)
			{
				ITransportHeaders requestHeaders;
				Stream requestStream;
				IPEndPoint remote;
				var transport = new UdpTransport(udpClient);
				transport.Read(out requestHeaders, out requestStream, out remote);

				ThreadPool.QueueUserWorkItem(s => ProcessMessage(remote, requestHeaders, requestStream));
			}
		}

		void ProcessMessage(IPEndPoint remote, ITransportHeaders requestHeaders, Stream requestStream)
		{
			// parse request uri
			var url = requestHeaders[CommonTransportKeys.RequestUri].ToString();
			string objectUri;
			UdpChannelHelper.Parse(url, out objectUri);
			objectUri = objectUri ?? url;
			requestHeaders[CommonTransportKeys.RequestUri] = objectUri;

			IMessage responseMsg;
			ITransportHeaders responseHeaders;
			Stream responseStream;

			// process message
			var stack = new ServerChannelSinkStack();
			stack.Push(this, null);
			var operation = NextChannelSink.ProcessMessage(stack, null, requestHeaders, requestStream,
				out responseMsg, out responseHeaders, out responseStream);

			switch (operation)
			{
				case ServerProcessing.Complete:
					stack.Pop(this);
					var transport = new UdpTransport(new UdpClient());
					transport.Write(responseHeaders, responseStream, remote);
					break;

				case ServerProcessing.Async:
					stack.StoreAndDispatch(NextChannelSink, null);
					break;

				case ServerProcessing.OneWay:
					break;
			}
		}
	}
}
