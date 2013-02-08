using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Transport
{
    /// <summary>
    /// Manages server transport apater instances.
    /// </summary>
    public class ServerTransportAdapterManager : IDisposable
    {
         #region Singleton implementation

        // Singleton instance
        private static ServerTransportAdapterManager _singleton = null;

        // Locking object for thread synchronization
        private static object _singletonLockObject = new object();

        /// <summary>
        /// Returns the singleton instance of TransportAdapterManager.
        /// </summary>
        public static ServerTransportAdapterManager Instance
        {
            get
            {
                if (_singleton == null)
                {
                    lock (_singletonLockObject)
                    {
                        if (_singleton == null)
                            _singleton = new ServerTransportAdapterManager();
                    }
                }
                return _singleton;
            }
        }

        #endregion

        #region Construction

        /// <summary>
        /// Creates a new instance of the TransportAdapterManager class.
        /// </summary>
        private ServerTransportAdapterManager()
        {
            _transportAdapters = new ConcurrentDictionary<string, IServerTransportAdapter>();
        }

        #endregion

        #region Transport adapter registration

        // Central transport adapter registry
        private ConcurrentDictionary<string, IServerTransportAdapter> _transportAdapters;

        /// <summary>
        /// Registers a specified Zyan transport adapter.
        /// </summary>
        /// <param name="adapter">Zyan transport adapter</param>
        public void Register(IServerTransportAdapter adapter)
        {
            if (adapter==null)
                throw new ArgumentNullException("adapter");

            if (_transportAdapters.ContainsKey(adapter.UniqueName))
                throw new ArgumentException(string.Format(LanguageResource.ArgumentException_DuplicateChannelName, adapter.UniqueName), "channel");

            _transportAdapters.TryAdd(adapter.UniqueName, adapter);
        }

        /// <summary>
        /// Unregisters a Zyan transport adapter by its name.
        /// </summary>
        /// <param name="adapterName">Adapter name</param>
        public void Unregister(string adapterName)
        {
            IServerTransportAdapter removedChannel;
            _transportAdapters.TryRemove(adapterName, out removedChannel);
        }

        /// <summary>
        ///Unregisters a Zyan transport adapter.
        /// </summary>
        /// <param name="adapter">Transport adapter</param>
        public void Unregister(IClientTransportAdapter adapter)
        {
            if (adapter == null)
                throw new ArgumentNullException("adapter");

            IServerTransportAdapter removedAdapter;
            _transportAdapters.TryRemove(adapter.UniqueName, out removedAdapter);
        }

        /// <summary>
        /// Gets a specified registered Zyan transport adapter.
        /// </summary>
        /// <param name="adapterName">Unique adapter name</param>
        /// <returns>Registered transport adapter</returns>
        public IServerTransportAdapter GetTransportAdapter(string adapterName)
        {
            IServerTransportAdapter adapter = null;
            _transportAdapters.TryGetValue(adapterName, out adapter);
            return adapter;
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Release managed resources.
        /// </summary>
        public void Dispose()
        {
            if (_transportAdapters != null)
            {
                //TODO: Implement clean up for registered adapter instances.
                _transportAdapters.Clear();
                _transportAdapters = null;
            }
        }

        #endregion
    }
}
