using System;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication.ChannelSinks.ClientAddress
{
	internal class ClientAddressServerChannelSink : BaseChannelObjectWithProperties, IServerChannelSink, IChannelSinkBase
	{
		private IServerChannelSink _nextSink;

		public ClientAddressServerChannelSink(IServerChannelSink next)
		{
			_nextSink = next;
		}

		public IServerChannelSink NextChannelSink
		{
			get { return _nextSink; }
			set { _nextSink = value; }
		}

		public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, Object state, IMessage message, ITransportHeaders headers, Stream stream)
		{
			IPAddress ip = headers[CommonTransportKeys.IPAddress] as IPAddress;
			CallContext.SetData("Zyan_ClientAddress", ip);
			sinkStack.AsyncProcessResponse(message, headers, stream);
		}

		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, Object state, IMessage message, ITransportHeaders headers)
		{
			return null;
		}

		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			if (_nextSink != null)
			{
				IPAddress ip = requestHeaders[CommonTransportKeys.IPAddress] as IPAddress;
				CallContext.SetData("Zyan_ClientAddress", ip);
				ServerProcessing spres = _nextSink.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream, out responseMsg, out responseHeaders, out responseStream);
				return spres;
			}
			else
			{
				responseMsg = null;
				responseHeaders = null;
				responseStream = null;
				return new ServerProcessing();
			}
		}
	}
}