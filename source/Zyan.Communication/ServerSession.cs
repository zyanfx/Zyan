using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using Zyan.Communication.SessionMgmt;

namespace Zyan.Communication
{
    [Serializable]
    public class ServerSession
    {
        private Guid _sessionID;
        private IIdentity _identity;
        private DateTime _timestamp;

        [NonSerialized]
        private SessionVariableAdapter _sessionVariableAdapter = null;

        internal ServerSession(Guid sessionID, IIdentity identity, SessionVariableAdapter sessionVariableAdapter) : this(sessionID, DateTime.Now, identity, sessionVariableAdapter) { }
	    
        internal ServerSession(Guid sessionID, DateTime timestamp, IIdentity identity, SessionVariableAdapter sessionVariableAdapter)
        {
            _timestamp = timestamp;
            _sessionID = sessionID;
            _identity = identity;
            _sessionVariableAdapter = sessionVariableAdapter;
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

        public SessionVariableAdapter SessionVariables
        {
            get { return _sessionVariableAdapter; }
        }

        [ThreadStatic]
        public static ServerSession CurrentSession;        
    }
}
