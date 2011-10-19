using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace Util
{
    internal class CompressionClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink
    {
        // The next sink in the sink chain.
        private readonly IClientChannelSink _next = null;

        // The compression threshold.
        private readonly int _compressionThreshold;

        #region IClientChannelSink Members

        /// <summary>
        /// Constructor with properties.
        /// </summary>
        /// <param name="nextSink">Next sink.</param>
        /// <param name="compressionThreshold">Compression threshold. If 0, compression is disabled globally.</param>
        public CompressionClientChannelSink(
            IClientChannelSink nextSink,
            int compressionThreshold)
        {
            // Set the next sink.
            _next = nextSink;
            // Set the compression threshold.
            _compressionThreshold = compressionThreshold;
        }

        public void AsyncProcessRequest(
            IClientChannelSinkStack sinkStack,
            IMessage msg,
            ITransportHeaders headers,
            Stream stream)
        {
            // Push this onto the sink stack.
            sinkStack.Push(this, null);
            // Send the request to the client.
            _next.AsyncProcessRequest(sinkStack, msg, headers, stream);
        }

        public void AsyncProcessResponse(
            IClientResponseChannelSinkStack sinkStack,
            object state,
            ITransportHeaders headers,
            Stream stream)
        {
            // Send the request to the server.
            sinkStack.AsyncProcessResponse(headers, stream);
        }

        public Stream GetRequestStream(
            IMessage msg,
            ITransportHeaders headers)
        {
            // Always return null
            return null;
        }

        public IClientChannelSink NextChannelSink
        {
            get { return _next; }
        }

        /// <summary>
        /// Returns true if the message contains the compression exempt parameters, marked as
        /// NonCompressible
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool IsCompressionExempt(IMessage msg)
        {
            bool compressionExempt = false;
            if (msg != null && msg.Properties.Contains("__Args"))
            {
                foreach (object obj in (object[]) msg.Properties["__Args"])
                {
                    Type type = obj.GetType();
                    if (type.IsDefined(typeof (NonCompressible), false))
                    {
                        compressionExempt = true;
                        break;
                    }
                    if (obj is ICompressible)
                    {
                        if ( ((ICompressible)obj).PerformCompression() == false )
                        {
                            compressionExempt = true;
                            break;
                        }
                    }
                }
            }
            return compressionExempt;
        }

        public void ProcessMessage(
            IMessage msg,
            ITransportHeaders requestHeaders,
            Stream requestStream,
            out ITransportHeaders responseHeaders,
            out Stream responseStream)
        {
            // If the request stream length is greater than the threshold
            // and message is not exempt from compression, compress the stream.
            if (_compressionThreshold > 0 &&
                requestStream.Length > _compressionThreshold &&
                !IsCompressionExempt(msg))
            {
                // Process the message and compress it.
                requestStream = CompressHelper.Compress(requestStream);

                // Send the compression flag to the server.
                requestHeaders[CommonHeaders.CompressionEnabled] = true;
            }

            // Send the compression supported flag to the server.
            requestHeaders[CommonHeaders.CompressionSupported] = true;

            // Send the request to the server.
            _next.ProcessMessage(
                msg, requestHeaders, requestStream,
                out responseHeaders, out responseStream);

            // If the response has the compression flag, decompress the stream.
            if (responseHeaders[CommonHeaders.CompressionEnabled] != null)
            {
                // Process the message and decompress it.
                responseStream = CompressHelper.Decompress(responseStream);
            }
        }

        #endregion
    }
}
