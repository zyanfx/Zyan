using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Zyan.Communication.HotUpdate
{
    /// <summary>
    /// Client side channel sink for envelope support.
    /// </summary>
    public class EnvelopeClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink
    {
		// Next channel sink
		private readonly IClientChannelSink _nextSink;

		// Lock object for thread synchronization
        private readonly object _lockObject = new object();

        // Target application name
        private string _targetApplicationName = string.Empty;

        // Serializer for serializing envelopes
        private BinaryFormatter _serializer = null;

        /// <summary>Creates a new instance of the EnvelopeClientChannelSink class</summary>
		/// <param name="nextSink">Next channel sink in sink chain</param>
        /// <param name="targetApplicationName">Target application name</param>
        /// <param name="serializer">Serializer for serializing envelopes</param>
        public EnvelopeClientChannelSink(IClientChannelSink nextSink, string targetApplicationName, BinaryFormatter serializer)
		{
            _targetApplicationName = targetApplicationName;
            _serializer = serializer;

			_nextSink = nextSink;
		}

        /// <summary>
        /// Creates a new envelope for a Remoting request stream.
        /// </summary>
        /// <param name="requestStream">Request stream</param>
        /// <param name="requestHeaders">Request transport headers</param>
        /// <returns>Envelope with request data</returns>
        private Stream CreateEnvelope(Stream requestStream, ITransportHeaders requestHeaders)
        {               
            byte[] blob;

            using (var requestStreamCopy = new MemoryStream())
            {
                requestStream.CopyTo(requestStreamCopy);
                blob = requestStreamCopy.ToArray();
            }

            var envelope = new Envelope()
            {
                TargetApplicationName = _targetApplicationName,
                MessageBlob = blob
            };

            var modifiedRequestStream = new MemoryStream();
            _serializer.Serialize(modifiedRequestStream, envelope);            
            modifiedRequestStream.Position = 0L;

            requestHeaders[TransportHeaderNames.HEADER_ENVELOPE] = true;

            return modifiedRequestStream;
        }

        /// <summary>
        /// Opens an existing envelope and restores its content.
        /// </summary>
        /// <param name="responseStream">Response stream</param>
        /// <param name="responseHeaders">Response transport headers</param>
        /// <returns>Envelope's content</returns>
        private Stream OpenEnvelope(Stream responseStream, ITransportHeaders responseHeaders)
        { 
            if (responseHeaders == null)
                return responseStream;

            if (!Convert.ToBoolean((string)responseHeaders[TransportHeaderNames.HEADER_ENVELOPE]))
                return responseStream;

            var envelope = (Envelope)_serializer.Deserialize(responseStream);

            if (envelope.TargetApplicationName != _targetApplicationName)
                throw new InvalidOperationException(LangaugeResource.ResponseDoesNotMatchTargetApplication);

            var modifiedResponseStream = new MemoryStream(envelope.MessageBlob);
            modifiedResponseStream.Position = 0L;

            return modifiedResponseStream;
        }

        /// <summary>
        /// Processes an asynchronous request.
        /// </summary>
        /// <param name="sinkStack">Channel sink stack</param>
        /// <param name="msg">Message to processed</param>
        /// <param name="headers">Transport headers</param>
        /// <param name="stream">Request stream</param>
        public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream)
        {
            stream = CreateEnvelope(stream, headers);

            sinkStack.Push(this, null);            
            _nextSink.AsyncProcessRequest(sinkStack, msg, headers, stream);
        }

        /// <summary>
        /// Processes an asynchronous response.
        /// </summary>
        /// <param name="sinkStack">Channel sink stack</param>
        /// <param name="state">State data</param>
        /// <param name="headers">Transport headers</param>
        /// <param name="stream">Response stream</param>
        public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
        {
            stream = OpenEnvelope(stream, headers);
            sinkStack.AsyncProcessResponse(headers, stream);
        }

        /// <summary>
        /// Gets the request stream.
        /// </summary>
        /// <param name="msg">Remoting Message</param>
        /// <param name="headers">Transport headers</param>
        /// <returns>Request stream</returns>
        public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
        {
            return null;
        }

        /// <summary>
        /// Gets the next channel sink in sink chain.
        /// </summary>
        public IClientChannelSink NextChannelSink
        {
            get { return _nextSink; }
        }

        /// <summary>
        /// Process a message synchronously.
        /// </summary>
        /// <param name="msg">Remoting message</param>
        /// <param name="requestHeaders">Request transport headers</param>
        /// <param name="requestStream">Request stream</param>
        /// <param name="responseHeaders">Response transport headers</param>
        /// <param name="responseStream">Response straem</param>
        public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, System.IO.Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
        {
            requestStream = CreateEnvelope(requestStream, requestHeaders);

            _nextSink.ProcessMessage
            (
                msg, 
                requestHeaders, 
                requestStream,
                out responseHeaders, 
                out responseStream
            );

            responseStream = OpenEnvelope(responseStream, responseHeaders);
        }
    }
}
