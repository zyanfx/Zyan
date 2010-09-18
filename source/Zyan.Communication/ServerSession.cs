using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication
{
    [Serializable]
    public class ServerSession
    {
        private Guid _sessionID;
        private IIdentity _identity;
        private DateTime _timestamp;

        public ServerSession (Guid sessionID, IIdentity identity)
	    {
            _timestamp=DateTime.Now;
            _sessionID=sessionID;
            _identity=identity;
	    }

        public Guid SessionID
        {
            get { return _sessionID; }
        }

        public IIdentity Identity
        {
            get { return _identity; }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        [ThreadStatic]
        public static ServerSession CurrentSession;        
    }
}
