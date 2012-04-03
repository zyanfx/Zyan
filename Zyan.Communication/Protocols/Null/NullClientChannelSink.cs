using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zyan.Communication.Protocols.Null
{
	/// <summary>
	/// Client channel sink for the <see cref="NullChannel"/>.
	/// </summary>
	public class NullClientChannelSink : IClientChannelSink, IMessageSink
	{
		/// <summary>
		/// Client channel sink provider for the <see cref="NullChannel"/>.
		/// </summary>
		public class Provider : IClientChannelSinkProvider
		{
			/// <summary>
			/// Creates the <see cref="NullClientChannelSink"/>.
			/// </summary>
			/// <param name="channel"><see cref="NullChannel"/> instance.</param>
			/// <param name="url">Object url.</param>
			/// <param name="remoteChannelData">Channel-specific data for the remote channel.</param>
			/// <returns><see cref="NullClientChannelSink"/> for the given url.</returns>
			public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
			{
				string objectUrl;
				string channelName = NullChannel.ParseUrl(url, out objectUrl);
				return new NullClientChannelSink(channelName);
			}

			/// <summary>
			/// Gets or sets the next <see cref="IClientChannelSinkProvider"/> in the chain.
			/// </summary>
			public IClientChannelSinkProvider Next { get; set; }
		}

		/// <summary>
		/// Ininitializes a new instance of the <see cref="NullChannel"/> class.
		/// </summary>
		/// <param name="channelName">Channel name.</param>
		public NullClientChannelSink(string channelName)
		{
			ChannelName = channelName;
		}

		private string ChannelName { get; set; }

		// =============== IClientChannelSink implementation ========================

		/// <summary>
		/// Request synchronous processing of a method call on the current sink.
		/// </summary>
		/// <param name="msg"><see cref="IMessage"/> to process.</param>
		/// <param name="requestHeaders">Request <see cref="ITransportHeaders"/>.</param>
		/// <param name="requestStream">Request <see cref="Stream"/>.</param>
		/// <param name="responseHeaders">Response <see cref="ITransportHeaders"/>.</param>
		/// <param name="responseStream">Response <see cref="Stream"/>.</param>
		public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			IMessage replyMsg;

			// process serialized message
			InternalProcessMessage(msg, requestHeaders, requestStream, out replyMsg, out responseHeaders, out responseStream);
		}

		/// <summary>
		/// Requests asynchronous processing of a method call on the current sink.
		/// </summary>
		/// <param name="sinkStack"><see cref="IClientChannelSinkStack"/> to process the request asynchronously.</param>
		/// <param name="msg"><see cref="IMessage"/> to process.</param>
		/// <param name="headers"><see cref="ITransportHeaders"/> for the message.</param>
		/// <param name="stream"><see cref="Stream"/> to serialize the message.</param>
		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			// TODO
			throw new NotImplementedException();
		}

		/// <summary>
		/// Requests asynchronous processing of a response to a method call on the current sink.
		/// </summary>
		/// <param name="sinkStack"><see cref="IClientResponseChannelSinkStack"/> to process the response.</param>
		/// <param name="state">State object.</param>
		/// <param name="headers"><see cref="ITransportHeaders"/> of the response message.</param>
		/// <param name="stream"><see cref="Stream"/> with the serialized response message.</param>
		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
		{
			// we're the last sink in the chain, so we don't have to implement this
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the <see cref="Stream"/> onto which the provided message is to be serialized.
		/// </summary>
		/// <param name="msg"><see cref="IMessage"/> to be serialized.</param>
		/// <param name="headers"><see cref="ITransportHeaders"/> for the message.</param>
		/// <returns>Request <see cref="Stream"/>.</returns>
		public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
		{
			// we don't need this
			return null;
		}

		/// <summary>
		/// Gets the next client channel sink in the client sink chain.
		/// </summary>
		public IClientChannelSink NextChannelSink
		{
			// we're the last sink in the chain
			get { return null; }
		}

		/// <summary>
		/// Gets a dictionary through which properties on the sink can be accessed.
		/// </summary>
		public IDictionary Properties
		{
			// we don't have any properties
			get { return null; }
		}

		// =============== IMessageSink implementation ========================

		/// <summary>
		/// Asynchronously processes the given message.
		/// </summary>
		/// <param name="msg">The message to process. </param>
		/// <param name="replySink">The reply sink for the reply message.</param>
		/// <returns>Returns an <see cref="IMessageCtrl"/> interface that provides a way to control asynchronous messages after they have been dispatched.</returns>
		public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
		{
			// TODO
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the next message sink in the sink chain.
		/// </summary>
		public IMessageSink NextSink
		{
			// we're the last sink in the chain
			get { return null; }
		}

		/// <summary>
		/// Synchronously processes the given message.
		/// </summary>
		/// <param name="msg">The message to process. </param>
		/// <returns>Response message.</returns>
		public IMessage SyncProcessMessage(IMessage msg)
		{
			IMessage replyMsg;
			ITransportHeaders requestHeaders = new TransportHeaders();
			//requestHeaders["Content-Type"] = "application/octet-stream";
			//requestHeaders["__RequestVerb"] = "POST";
			//requestHeaders["__ConnectionId"] = 2;
			//requestHeaders["__IPAddress"] = IPAddress.Loopback.ToString();

			ITransportHeaders responseHeaders;
			Stream responseStream;

			// process non-serialized message
			InternalProcessMessage(msg, requestHeaders, null, out replyMsg, out responseHeaders, out responseStream);
			return replyMsg;
		}

		/// <summary>
		/// Processes the given method call message synchronously.
		/// </summary>
		private void InternalProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage replyMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// add message Uri to the transport headers
			var mcm = (IMethodCallMessage)msg;
			requestHeaders[CommonTransportKeys.RequestUri] = mcm.Uri;

			// create the request message
			var requestMessage = new NullMessages.RequestMessage
			{
				Message = msg,
				RequestHeaders = requestHeaders,
				RequestStream = requestStream
			};

			// process the request and receive the response message
			var responseMessage = NullMessages.ProcessRequest(ChannelName, requestMessage);

			// return processed message parts
			responseHeaders = responseMessage.ResponseHeaders;
			responseStream = responseMessage.ResponseStream;
			replyMsg = responseMessage.Message;
		}
	}
}
