using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Zyan.Communication.HotUpdate
{
    /// <summary>
    /// Client side sink provider for envelope support. 
    /// </summary>
    public class EnvelopeClientChannelSinkProvider : IClientChannelSinkProvider
    {
        // Next client sink in sink chain
        private IClientChannelSinkProvider _nextSink = null;

        // Target application name
        private string _targetApplicationName = string.Empty;

        // Binary formatter for envelope serialization
        private BinaryFormatter _serializer = null;

        // Lock objekt for synchonizing multi-threaded access while serializer is created
        private object _serializerLockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvelopeClientChannelSinkProvider"/> class.
        /// <remarks>
        /// 'DEFAULT' is used as target application name.
        /// </remarks>
        /// </summary>
        public EnvelopeClientChannelSinkProvider()
        {
            _targetApplicationName = "DEFAULT";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvelopeClientChannelSinkProvider"/> class.
        /// </summary>
        /// <param name="targetApplicationName">Name of the target server application</param>
        public EnvelopeClientChannelSinkProvider(string targetApplicationName)
        {
            if (string.IsNullOrEmpty(targetApplicationName))
                throw new ArgumentException(LangaugeResource.TargetApplicationNameMustNotBeEmpty, "targetApplicationName");

            _targetApplicationName = targetApplicationName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvelopeClientChannelSinkProvider"/> class.
		/// </summary>
		/// <param name="properties">Sink properties</param>
		/// <param name="contextData">Context data (ignored)</param>
        public EnvelopeClientChannelSinkProvider(IDictionary properties, ICollection contextData)
        {
            foreach (DictionaryEntry entry in properties)
            {
                switch ((string)entry.Key)
                {
                    case "targetApplicationName":
                        _targetApplicationName = (string)entry.Value;
                        break;

                    default:
                        throw new ArgumentException(string.Format(LangaugeResource.InvalidChannelConfigSetting, (String)entry.Key));
                }
            }
        }

        /// <summary>
        /// Creates a client channel sink instance.
        /// </summary>
        /// <param name="channel">Channel for which the current sink chain is being constructed.</param>
        /// <param name="url">The URL of the object to connect to. This parameter can be null if the connection is based entirely on the information contained in the <paramref name="remoteChannelData"/> parameter.</param>
        /// <param name="remoteChannelData">A channel data object that describes a channel on the remote server.</param>
        /// <returns>
        /// The first sink of the newly formed channel sink chain, or null, which indicates that this provider will not or cannot provide a connection for this endpoint.
        /// </returns>
        /// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
        public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
        {
            IClientChannelSink nextSink = null;

			if (_nextSink != null)
			{
				// Call CreateSink on the next sink provider in the chain.  This will return
				// to us the actual next sink object.  If the next sink is null, uh oh!
				if ((nextSink = _nextSink.CreateSink(channel, url, remoteChannelData)) == null) 
                    return null;
			}

            InitializeSerializer();

            var sink = new EnvelopeClientChannelSink(nextSink, _targetApplicationName, _serializer);
            return sink;
        }

        /// <summary>
        /// Gets or sets the next sink provider in the channel sink provider chain.
        /// </summary>
        public IClientChannelSinkProvider Next
        {
            get { return _nextSink; }
            set { _nextSink = value; }
        }
        
        /// <summary>
        /// Initializes the serializer for serializing envelopes.
        /// </summary>
        private void InitializeSerializer()
        {
            if (_serializer == null)
            {
                lock (_serializerLockObject)
                {
                    if (_serializer == null)
                        _serializer =
                            new BinaryFormatter()
                            {
                                AssemblyFormat = FormatterAssemblyStyle.Simple,
                                FilterLevel = TypeFilterLevel.Full,
                                TypeFormat = FormatterTypeStyle.TypesAlways
                            };
                }
            }
        }
    }
}
