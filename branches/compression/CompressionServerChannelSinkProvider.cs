using System;
using System.Collections;
using System.Runtime.Remoting.Channels;

namespace Util
{
    public class CompressionServerChannelSinkProvider : IServerChannelSinkProvider
    {
        // The next sink provider in the sink provider chain.
        private IServerChannelSinkProvider _next = null;
        // The compression threshold.
        private readonly int _compressionThreshold;

        public CompressionServerChannelSinkProvider(IDictionary properties, ICollection providerData)
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

        #region IServerChannelSinkProvider Members

        public IServerChannelSink CreateSink(IChannelReceiver channel)
        {
            IServerChannelSink nextSink = null;
            if (_next != null)
            {
                // Call CreateSink on the next sink provider in the chain.  This will return
                // to us the actual next sink object.  If the next sink is null, uh oh!
                if ((nextSink = _next.CreateSink(channel)) == null) return null;
            }

            // Create this sink, passing to it the previous sink in the chain so that it knows
            // to whom messages should be passed.
            return new CompressionServerChannelSink(nextSink, _compressionThreshold);
        }

        public void GetChannelData(IChannelDataStore channelData)
        {
            // Do nothing.  No channel specific data.
        }

        /// <summary>
        /// Gets or sets the next sink provider in the channel sink provider chain.
        /// </summary>
        public IServerChannelSinkProvider Next
        {
			get { return _next; }
			set { _next = value; }
        }

        #endregion
    }
}
