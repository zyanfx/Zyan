/*
 THIS CODE IS BASED ON:
 -------------------------------------------------------------------------------------------------------------- 
 Remoting Compression Channel Sink

 November, 12, 2008 - Initial revision.
 Alexander Schmidt - http://www.alexschmidt.net

 Originally published at CodeProject:
 http://www.codeproject.com/KB/IP/remotingcompression.aspx

 Copyright © 2008 Alexander Schmidt. All Rights Reserved.
 Distributed under the terms of The Code Project Open License (CPOL).
 --------------------------------------------------------------------------------------------------------------
*/
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using Zyan.Communication.Toolbox.Compression;

namespace Zyan.Communication.ChannelSinks.Compression
{
	internal class CompressionServerChannelSink : BaseChannelSinkWithProperties, IServerChannelSink
	{
		// The next sink in the sink chain.
		private readonly IServerChannelSink _next = null;

		// The compression threshold.
		private readonly int _compressionThreshold;

		/// <summary>
		/// Constructor with properties.
		/// </summary>
		/// <param name="nextSink">Next sink.</param>
		/// <param name="compressionThreshold">Compression threshold. If 0, compression is disabled globally.</param>
		public CompressionServerChannelSink(
			IServerChannelSink nextSink,
			int compressionThreshold)
		{
			// Set the next sink.
			_next = nextSink;
			// Set the compression threshold.
			_compressionThreshold = compressionThreshold;
		}

		public void AsyncProcessResponse(
			IServerResponseChannelSinkStack sinkStack,
			object state,
			IMessage msg,
			ITransportHeaders headers,
			Stream stream)
		{
			// Send the response to the client.
			sinkStack.AsyncProcessResponse(msg, headers, stream);
		}

		public Stream GetResponseStream(
			IServerResponseChannelSinkStack sinkStack,
			object state,
			IMessage msg,
			ITransportHeaders headers)
		{
			// Always return null
			return null;
		}

		/// <summary>
		/// Gets the next server channel sink in the server sink chain.
		/// </summary>
		/// <returns>The next server channel sink in the server sink chain.</returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have the required <see cref="F:System.Security.Permissions.SecurityPermissionFlag.Infrastructure"/> permission. </exception>
		public IServerChannelSink NextChannelSink
		{
			get { return _next; }
		}

		/// <summary>
		/// Returns true if the message return value is marked as NonCompressible.
		/// </summary>
		/// <param name="msg">Message to check.</param>
		public static bool IsCompressionExempt(IMessage msg)
		{
			if (msg != null && msg.Properties.Contains("__Return"))
			{
				var obj = msg.Properties["__Return"];
				if (obj == null)
				{
					return false;
				}

				if (obj.GetType().IsDefined(typeof(NonCompressibleAttribute), false))
				{
					return true;
				}

				if (obj is ICompressible)
				{
					if (((ICompressible)obj).PerformCompression == false)
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Requests message processing from the current sink.
		/// </summary>
		/// <param name="sinkStack">A stack of channel sinks that called the current sink.</param>
		/// <param name="requestMsg">The message that contains the request.</param>
		/// <param name="requestHeaders">Headers retrieved from the incoming message from the client.</param>
		/// <param name="requestStream">The stream that needs to be to processed and passed on to the deserialization sink.</param>
		/// <param name="responseMsg">When this method returns, contains a <see cref="T:System.Runtime.Remoting.Messaging.IMessage"/> that holds the response message. This parameter is passed uninitialized.</param>
		/// <param name="responseHeaders">When this method returns, contains a <see cref="T:System.Runtime.Remoting.Channels.ITransportHeaders"/> that holds the headers that are to be added to return message heading to the client. This parameter is passed uninitialized.</param>
		/// <param name="responseStream">When this method returns, contains a <see cref="T:System.IO.Stream"/> that is heading back to the transport sink. This parameter is passed uninitialized.</param>
		/// <returns>
		/// A <see cref="T:System.Runtime.Remoting.Channels.ServerProcessing"/> status value that provides information about how message was processed.
		/// </returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// Push this onto the sink stack
			sinkStack.Push(this, null);

			// If the request has the compression flag, decompress the stream.
			if (requestHeaders[CommonHeaders.CompressionEnabled] != null)
			{
				// Process the message and decompress it.
				requestStream = CompressionHelper.Decompress(requestStream);
			}

			// Retrieve the response from the server.
			var processingResult = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream,
				out responseMsg, out responseHeaders, out responseStream);

			// If the response stream length is greater than the threshold,
			// message is not exempt from compression, and client supports compression,
			// compress the stream.
			if (processingResult == ServerProcessing.Complete
				&& _compressionThreshold > 0
				&& responseStream.Length > _compressionThreshold
				&& !IsCompressionExempt(responseMsg)
				&& requestHeaders[CommonHeaders.CompressionSupported] != null)
			{
				// Process the message and compress it.
				responseStream = CompressionHelper.Compress(responseStream);

				// Send the compression flag to the client.
				responseHeaders[CommonHeaders.CompressionEnabled] = true;
			}

			// Take off the stack and return the result.
			sinkStack.Pop(this);
			return processingResult;
		}
	}
}
