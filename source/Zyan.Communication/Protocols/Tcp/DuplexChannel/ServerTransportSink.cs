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
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	class ServerTransportSink : IServerChannelSink
	{
		private IServerChannelSink _nextSink;

		public ServerTransportSink(IServerChannelSink nextSink)
		{
			this._nextSink = nextSink;
		}

		public void ReceiveMessage(IAsyncResult ar)
		{
			// Request next message...
			Manager.BeginReadMessage(ar.AsyncState, null, new AsyncCallback(ReceiveMessage), ar.AsyncState);

			Connection connection;
			Message request = Manager.EndReadMessage(out connection, ar);

			request.Headers["__CustomErrorsEnabled"] = RemotingConfiguration.CustomErrorsEnabled(connection.IsLocalHost);

			IMessage responseMsg;
			ITransportHeaders responseHeaders;
			Stream responseStream;

			var channelData = connection.Channel.ChannelData as TcpExChannelData;

			if (channelData != null)
			{
				channelData.RemoteChannelID = connection.RemoteChannelID;
			}

			var serverProcessing = ProcessMessage(new ServerChannelSinkStack(), null, request.Headers, request.MessageBody, out responseMsg, out responseHeaders, out responseStream);
			if (serverProcessing != ServerProcessing.OneWay)
			{
				Message.Send(connection, request.Guid, responseHeaders, responseStream);
				responseStream.Close();
			}
		}

		#region Implementation of IServerChannelSink

		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers)
		{
			return null;
		}

		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			sinkStack.Push(this, null);
			ServerProcessing serverProcessing = _nextSink.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream, out responseMsg, out responseHeaders, out responseStream);
			switch (serverProcessing)
			{
				case ServerProcessing.Async:
					sinkStack.StoreAndDispatch(this, null);
					return serverProcessing;
				case ServerProcessing.OneWay:
				case ServerProcessing.Complete:
					sinkStack.Pop(this);
					return serverProcessing;
				default:
					return serverProcessing;
			}
		}

		public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			throw new NotImplementedException();
		}

		public IServerChannelSink NextChannelSink
		{
			get { return _nextSink; }
		}

		#endregion

		#region Implementation of IChannelSinkBase
		
		public IDictionary Properties
		{
			get { return null; }
		}

		#endregion
	}
}
