using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Transport
{
    /// <summary>
    /// Manages transport channel instances.
    /// </summary>
    public class TransportChannelManager : IDisposable
    {
        #region Singleton implementation

        // Singleton instance
        private static TransportChannelManager _singleton = null;

        // Locking object for thread synchronization
        private static object _singletonLockObject = new object();

        /// <summary>
        /// Returns the singleton instance of TransportChannelManager.
        /// </summary>
        public static TransportChannelManager Instance
        {
            get
            {
                if (_singleton == null)
                {
                    lock (_singletonLockObject)
                    {
                        if (_singleton == null)
                            _singleton = new TransportChannelManager();
                    }
                }
                return _singleton;
            }
        }

        #endregion

        #region Construction

        /// <summary>
        /// Creates a new instance of the TransportChannelManager class.
        /// </summary>
        private TransportChannelManager()
        {
            _channels = new ConcurrentDictionary<string, IZyanTransportChannel>();
        }

        #endregion

        #region Transport channel registration

        // Central transport channel registry
        private ConcurrentDictionary<string, IZyanTransportChannel> _channels;

        /// <summary>
        /// Registers a specified Zyan transport channel.
        /// </summary>
        /// <param name="channel">Zyan transport channel</param>
        public void RegisterChannel(IZyanTransportChannel channel)
        {
            if (channel==null)
                throw new ArgumentNullException("channel");

            if (_channels.ContainsKey(channel.ChannelName))
                throw new ArgumentException(string.Format(LanguageResource.ArgumentException_DuplicateChannelName, channel.ChannelName), "channel");

            _channels.TryAdd(channel.ChannelName, channel);
        }

        /// <summary>
        /// Unregisters a channel by its name.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        public void UnregisterChannel(string channelName)
        {
            IZyanTransportChannel removedChannel;
            _channels.TryRemove(channelName, out removedChannel);
        }

        /// <summary>
        /// Unregisters a channel.
        /// </summary>
        /// <param name="channel">Transport channel</param>
        public void UnregisterChannel(IZyanTransportChannel channel)
        {
            if (channel == null)
                throw new ArgumentNullException("channel");

            IZyanTransportChannel removedChannel;
            _channels.TryRemove(channel.ChannelName, out removedChannel);
        }

        /// <summary>
        /// Gets a specified registered transport channel.
        /// </summary>
        /// <param name="channelName">Unique channel name</param>
        /// <returns>Registered transport channel</returns>
        public IZyanTransportChannel GetChannel(string channelName)
        {
            return _channels[channelName];
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Release managed resources.
        /// </summary>
        public void Dispose()
        {
            if (_channels != null)
            {
                //TODO: Implement clean up for registered channel instances.
                _channels.Clear();
                _channels = null;
            }
        }

        #endregion
    }
}
