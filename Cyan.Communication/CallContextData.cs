using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;

namespace Cyan.Communication
{
    [Serializable]
    public class LogicalCallContextData : ILogicalThreadAffinative
    {
        private Hashtable _store = null;

        public LogicalCallContextData()
        {
            _store = new Hashtable();
        }

        public Hashtable Store
        {
            get { return _store; }
        }
    }

}
