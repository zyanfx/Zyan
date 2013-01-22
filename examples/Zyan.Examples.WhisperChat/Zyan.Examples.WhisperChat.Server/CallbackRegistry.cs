using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace Zyan.Examples.WhisperChat.Server
{
    internal class CallbackRegistry
    {
        private ConcurrentDictionary<string, Action<string, string>> _registry;

        private static object _singletonLock = new object();
        private static CallbackRegistry _singleton;

        public static CallbackRegistry Instance
        {
            get
            {
                if (_singleton == null)
                {
                    lock (_singletonLock)
                    {
                        if (_singleton == null)
                            _singleton = new CallbackRegistry();
                    }
                }
                return _singleton;
            }
        }

        private CallbackRegistry()
        {
            _registry = new ConcurrentDictionary<string, Action<string, string>>();
        }

        public bool Register(string name, Action<string, string> callback)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Parameter 'name' must not be empty.","name");

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (_registry.ContainsKey(name))
                return false;

            bool success = _registry.TryAdd(name, callback);

            if (success)
                Console.WriteLine("{0}: Registered '{1}'.", DateTime.Now.ToString(), name);

            return success;
        }

        public bool Unregister(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Parameter 'name' must not be empty.", "name");

            Action<string, string> removedCallback;
            bool success = _registry.TryRemove(name, out removedCallback);

            if (success)
                Console.WriteLine("{0}: Unregistered '{1}'.", DateTime.Now.ToString(), name);

            return success;
        }

        public Action<string, string> GetCallbackByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Parameter 'name' must not be empty.", "name");

            Action<string, string> callback = null;

            _registry.TryGetValue(name, out callback);
             return callback;
        }
    }
}
