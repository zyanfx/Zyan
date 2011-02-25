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
using System.IO;
using System.Net.Sockets;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	class ServerTransportSink : IServerChannelSink
	{
		static readonly BinaryFormatter formatter = new BinaryFormatter();

		TcpExChannel channel;
		IServerChannelSink nextSink;

		public ServerTransportSink(IServerChannelSink nextSink, TcpExChannel channel)
		{
			this.nextSink = nextSink;
			this.channel = channel;
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
			ProcessMessage(new ServerChannelSinkStack(), null, request.Headers, request.MessageBody, out responseMsg, out responseHeaders, out responseStream);

			Message.Send(connection, request.Guid, responseHeaders, responseStream);
			responseStream.Close();
		}

		#region Implementation of IServerChannelSink
		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers)
		{
			return null;
		}

		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			sinkStack.Push(this, null);
			ServerProcessing serverProcessing = nextSink.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream, out responseMsg, out responseHeaders, out responseStream);
			switch (serverProcessing)
			{
				case ServerProcessing.Async:
					throw new NotImplementedException(); // TODO: Asynchronous support
				case ServerProcessing.Complete:
					return serverProcessing;
				case ServerProcessing.OneWay:
					throw new NotImplementedException(); // TODO: OneWay support
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
			get
			{
				return nextSink;
			}
		}
		#endregion

		#region Implementation of IChannelSinkBase
		public IDictionary Properties
		{
			get
			{
				return null;
			}
		}
		#endregion
	}
}
