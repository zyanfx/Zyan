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
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using Zyan.Communication.Protocols.Tcp.DuplexChannel.Diagnostics;
using System.Threading;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	class ClientTransportSink : IClientChannelSink
	{
		static BinaryFormatter formatter = new BinaryFormatter();

		string server;
		TcpExChannel channel;

		public ClientTransportSink(string server, TcpExChannel channel)
		{
			Debug.Assert(server != null);

			this.server = server;
			this.channel = channel;

			Connection.GetConnection(server, channel, channel.TcpKeepAliveEnabled, channel.TcpKeepAliveTime, channel.TcpKeepAliveInterval, channel.MaxRetries, channel.RetryDelay); // Try to connect so we fail during creation if the other side isn't listening
		}

		#region Implementation of IClientChannelSink

		Connection PrepareRequest(IMessage msg, ref ITransportHeaders requestHeaders)
		{
			string url = (string)msg.Properties["__Uri"];
			string objectID;
			Manager.Parse(url, out objectID);
			if (objectID != null)
				requestHeaders["__RequestUri"] = objectID;
			else
				requestHeaders["__RequestUri"] = url;

            Connection connection = Connection.GetConnection(server, channel, channel.TcpKeepAliveEnabled, channel.TcpKeepAliveTime, channel.TcpKeepAliveInterval, channel.MaxRetries,channel.RetryDelay);

			System.Diagnostics.Debug.Assert(connection != null, "Manager.GetConnection returned null", "Manager.GetConnection returned null in ClientTransportSink.ProcessMessage for server - " + server);
			return connection;
		}

		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			if (stream.Length > int.MaxValue)
				throw new NotImplementedException("TcpEx Channel can only accept messages up to int.MaxValue in size."); // TODO: FixMe

			Connection connection = PrepareRequest(msg, ref headers);

			Guid msgGuid = Guid.NewGuid();
			Manager.BeginReadMessage(msgGuid, connection, new AsyncCallback(ProcessResponse), sinkStack);
			Message.Send(connection, msgGuid, headers, stream);
		}

		public void ProcessResponse(IAsyncResult ar)
		{
			Connection connection;
			Exception exception;
			Message reply = Manager.EndReadMessage(out connection, out exception, ar);

			IClientChannelSinkStack sinkStack = (IClientChannelSinkStack)ar.AsyncState;
			if (exception != null)
				sinkStack.DispatchException(exception);

			sinkStack.AsyncProcessResponse(reply.Headers, reply.MessageBody);
		}

		public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			if (requestStream.Length > int.MaxValue)
				throw new NotImplementedException("TcpEx Channel can only accept messages up to int.MaxValue in size."); // TODO: FixMe

			Connection connection = PrepareRequest(msg, ref requestHeaders);

			Guid msgGuid = Guid.NewGuid();
			IAsyncResult ar = Manager.BeginReadMessage(msgGuid, connection, null, null);
            Message.Send(connection, msgGuid, requestHeaders, requestStream); 

          	ar.AsyncWaitHandle.WaitOne();
			Connection replyConnection;
			Message reply = Manager.EndReadMessage(out replyConnection, ar);
			Debug.Assert(connection == replyConnection);
			responseHeaders = reply.Headers;
			responseStream = reply.MessageBody;
		}

		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
		{
			throw new NotImplementedException();
		}

		public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
		{
			return null;
		}

		public IClientChannelSink NextChannelSink
		{
			get
			{
				return null;
			}
		}
		#endregion

		#region Implementation of IChannelSinkBase
		public System.Collections.IDictionary Properties
		{
			get
			{
				return null;
			}
		}
		#endregion
	}

	class ClientTransportSinkProvider : IClientChannelSinkProvider
	{
		#region Implementation of IClientChannelSinkProvider
		public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
		{
			string objectUri;
			string server = Manager.Parse(url, out objectUri);
			return new ClientTransportSink(server, (TcpExChannel)channel);
		}

		public IClientChannelSinkProvider Next
		{
			get
			{
				return null;
			}

			set
			{
				throw new InvalidOperationException("May not set Next property on transport sink.");
			}
		}
		#endregion
	}
}
