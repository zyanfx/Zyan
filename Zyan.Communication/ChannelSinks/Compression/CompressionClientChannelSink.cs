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
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using Zyan.Communication.Toolbox.Compression;

namespace Zyan.Communication.ChannelSinks.Compression
{
	internal class CompressionClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink
	{
		// The next sink in the sink chain.
		private readonly IClientChannelSink _next = null;

		// The compression threshold.
		private readonly int _compressionThreshold;

		// The compression method.
		private readonly CompressionMethod _compressionMethod;

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressionClientChannelSink"/> class.
		/// </summary>
		/// <param name="nextSink">Next sink.</param>
		/// <param name="compressionThreshold">Compression threshold. If 0, compression is disabled globally.</param>
		/// <param name="compressionMethod">The compression method.</param>
		public CompressionClientChannelSink(IClientChannelSink nextSink, int compressionThreshold, CompressionMethod compressionMethod)
		{
			_next = nextSink;
			_compressionThreshold = compressionThreshold;
			_compressionMethod = compressionMethod;
		}

		/// <summary>
		/// Requests asynchronous processing of a method call on the current sink.
		/// </summary>
		/// <param name="sinkStack">A stack of channel sinks that called this sink.</param>
		/// <param name="msg">The message to process.</param>
		/// <param name="headers">The headers to add to the outgoing message heading to the server.</param>
		/// <param name="stream">The stream headed to the transport sink.</param>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			// Push this onto the sink stack.
			sinkStack.Push(this, null);

			// Send the request to the client.
			_next.AsyncProcessRequest(sinkStack, msg, headers, stream);
		}

		/// <summary>
		/// Requests asynchronous processing of a response to a method call on the current sink.
		/// </summary>
		/// <param name="sinkStack">A stack of sinks that called this sink.</param>
		/// <param name="state">Information generated on the request side that is associated with this sink.</param>
		/// <param name="headers">The headers retrieved from the server response stream.</param>
		/// <param name="stream">The stream coming back from the transport sink.</param>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
		{
			// Send the request to the server.
			sinkStack.AsyncProcessResponse(headers, stream);
		}

		/// <summary>
		/// Returns the <see cref="T:System.IO.Stream"/> onto which the provided message is to be serialized. Always returns null.
		/// </summary>
		/// <param name="msg">The <see cref="T:System.Runtime.Remoting.Messaging.IMethodCallMessage"/> containing details about the method call.</param>
		/// <param name="headers">The headers to add to the outgoing message heading to the server.</param>
		/// <returns>
		/// The <see cref="T:System.IO.Stream"/> onto which the provided message is to be serialized.
		/// </returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
		{
			// Always return null
			return null;
		}

		/// <summary>
		/// Gets the next client channel sink in the client sink chain.
		/// </summary>
		/// <returns>The next client channel sink in the client sink chain.</returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public IClientChannelSink NextChannelSink
		{
			get { return _next; }
		}

		/// <summary>
		/// Returns true if the message contains the compression exempt parameters, marked as NonCompressible.
		/// </summary>
		/// <param name="msg">Message</param>
		/// <returns>True if the message should not be compressed.</returns>
		public static bool IsCompressionExempt(IMessage msg)
		{
			if (msg != null && msg.Properties.Contains("__Args"))
			{
				var args = (object[])msg.Properties["__Args"];
				foreach (var obj in args)
				{
					if (obj == null)
					{
						continue;
					}

					var type = obj.GetType();
					if (type.IsDefined(typeof(NonCompressibleAttribute), false))
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
			}

			return false;
		}

		/// <summary>
		/// Requests message processing from the current sink.
		/// </summary>
		/// <param name="msg">The message to process.</param>
		/// <param name="requestHeaders">The headers to add to the outgoing message heading to the server.</param>
		/// <param name="requestStream">The stream headed to the transport sink.</param>
		/// <param name="responseHeaders">When this method returns, contains a <see cref="T:System.Runtime.Remoting.Channels.ITransportHeaders"/> interface that holds the headers that the server returned. This parameter is passed uninitialized.</param>
		/// <param name="responseStream">When this method returns, contains a <see cref="T:System.IO.Stream"/> coming back from the transport sink. This parameter is passed uninitialized.</param>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// If the request stream length is greater than the threshold
			// and message is not exempt from compression, compress the stream.
			if (_compressionThreshold > 0 &&
				requestStream.Length > _compressionThreshold &&
				!IsCompressionExempt(msg))
			{
				// Process the message and compress it.
				requestStream = CompressionHelper.Compress(requestStream, _compressionMethod);

				// Send the compression flag to the server.
				requestHeaders[CommonHeaders.CompressionEnabled] = true;
			}

			// Send the compression supported flag to the server.
			requestHeaders[CommonHeaders.CompressionSupported] = true;
			requestHeaders[CommonHeaders.CompressionMethod] = (int)_compressionMethod;

			// Send the request to the server.
			_next.ProcessMessage(
				msg, requestHeaders, requestStream,
				out responseHeaders, out responseStream);

			// If the response has the compression flag, decompress the stream.
			if (responseHeaders[CommonHeaders.CompressionEnabled] != null)
			{
				// Determine compression method
				var method = CompressionMethod.Default;
				if (responseHeaders[CommonHeaders.CompressionMethod] != null)
				{
					method = (CompressionMethod)Convert.ToInt32(responseHeaders[CommonHeaders.CompressionMethod]);
				}

				// Process the message and decompress it.
				responseStream = CompressionHelper.Decompress(responseStream, method);
			}
		}
	}
}
