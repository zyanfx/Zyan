using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace Util
{
    internal class CompressionServerChannelSink : BaseChannelSinkWithProperties, IServerChannelSink
    {
        // The next sink in the sink chain.
        private readonly IServerChannelSink _next = null;

        // The compression threshold.
        private readonly int _compressionThreshold;

        #region IServerChannelSink Members

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

        public IServerChannelSink NextChannelSink
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
            if (msg != null && msg.Properties.Contains("__Return"))
            {
                object obj = msg.Properties["__Return"];
                if (obj.GetType().IsDefined(typeof (NonCompressible), false))
                {
                    compressionExempt = true;
                }
                if (obj is ICompressible)
                {
                    if (((ICompressible)obj).PerformCompression() == false)
                    {
                        compressionExempt = true;
                    }
                }
            }
            return compressionExempt;
        }

        public ServerProcessing ProcessMessage(
            IServerChannelSinkStack sinkStack,
            IMessage requestMsg,
            ITransportHeaders requestHeaders,
            Stream requestStream,
            out IMessage responseMsg,
            out ITransportHeaders responseHeaders,
            out Stream responseStream)
        {
            // Push this onto the sink stack
            sinkStack.Push(this, null);

            // If the request has the compression flag, decompress the stream.
            if (requestHeaders[CommonHeaders.CompressionEnabled] != null)
            {
                // Process the message and decompress it.
                requestStream = CompressHelper.Decompress(requestStream);
            }

            // Retrieve the response from the server.
            ServerProcessing processingResult = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders,
                                                    requestStream, out responseMsg, out responseHeaders,
                                                    out responseStream);

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
                responseStream = CompressHelper.Compress(responseStream);

                // Send the compression flag to the client.
                responseHeaders[CommonHeaders.CompressionEnabled] = true;
            }

            // Take off the stack and return the result.
            sinkStack.Pop(this);
            return processingResult;
        }

        #endregion
    }
}
