using System;
using System.Collections;
using System.Runtime.Remoting.Channels;

namespace Util
{
    public class CompressionClientChannelSinkProvider : IClientChannelSinkProvider
    {
        // The next sink provider in the sink provider chain.
        private IClientChannelSinkProvider _next = null;
        // The compression threshold.
        private readonly int _compressionThreshold;

        public CompressionClientChannelSinkProvider()
        {
        }

        public CompressionClientChannelSinkProvider(IDictionary properties, ICollection contextData)
        {
            // read in web.config parameters
            foreach (DictionaryEntry entry in properties)
            {
                switch ((String) entry.Key)
                {
                    case "compressionThreshold":
                        _compressionThreshold = Convert.ToInt32((String) entry.Value);
                        break;
                    default:
                        throw new ArgumentException("Invalid configuration entry: " + (String) entry.Key);
                }
            }
        }

        #region IClientChannelSinkProvider Members

        public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
        {
            IClientChannelSink nextSink = null;

			if (_next != null)
			{
				// Call CreateSink on the next sink provider in the chain.  This will return
				// to us the actual next sink object.  If the next sink is null, uh oh!
				if ((nextSink = _next.CreateSink(channel, url, remoteChannelData)) == null) return null;
			}

			// Create this sink, passing to it the previous sink in the chain so that it knows
			// to whom messages should be passed.
			return new CompressionClientChannelSink(nextSink, _compressionThreshold);
        }

        /// <summary>
        /// Gets or sets the next sink provider in the channel sink provider chain.
        /// </summary>
        public IClientChannelSinkProvider Next
        {
            get { return _next; }
            set { _next = value; }
        }

        #endregion
    }
}
