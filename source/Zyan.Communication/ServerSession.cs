using System;
using System.Security.Principal;
using Zyan.Communication.SessionMgmt;

namespace Zyan.Communication
{
    /// <summary>
    /// Beschreibt eine Sitzung auf dem Server.
    /// </summary>
    [Serializable]
    public class ServerSession
    {
        // Felder
        private Guid _sessionID;
        private IIdentity _identity;
        private DateTime _timestamp;

        // Adapter für den Zugriff auf Sitzungsvariablen
        [NonSerialized]
        private SessionVariableAdapter _sessionVariableAdapter = null;

        /// <summary>
        /// Erzeugt eine neue Instanz von ServerSession.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <param name="identity">Identität</param>
        /// <param name="sessionVariableAdapter">Adapter für den Zugriff auf Sitzungsvariablen</param>
        internal ServerSession(Guid sessionID, IIdentity identity, SessionVariableAdapter sessionVariableAdapter) : this(sessionID, DateTime.Now, identity, sessionVariableAdapter) { }

        /// <summary>
        /// Erzeugt eine neue Instanz von ServerSession.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <param name="timestamp">Zeitstempel der Sitzung</param>
        /// <param name="identity">Identität</param>
        /// <param name="sessionVariableAdapter">Adapter für den Zugriff auf Sitzungsvariablen</param>
        internal ServerSession(Guid sessionID, DateTime timestamp, IIdentity identity, SessionVariableAdapter sessionVariableAdapter)
        {
            _timestamp = timestamp;
            _sessionID = sessionID;
            _identity = identity;
            _sessionVariableAdapter = sessionVariableAdapter;
        }

        /// <summary>
        /// Gibt den Sitzungsschlüssel zurück.
        /// </summary>
        public Guid SessionID
        {
            get { return _sessionID; }
        }

        /// <summary>
        /// Gibt die Identität des Beutzers der Sitzung zurück.
        /// </summary>
        public IIdentity Identity
        {
            get { return _identity; }
        }

        /// <summary>
        /// Gibt den Zeitstempel der Sitzung zurück.
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        /// <summary>
        /// Gibt ein Adapter-Objekt für den Zugriff auf Sitzungsvariablen zurück.
        /// </summary>
        public SessionVariableAdapter SessionVariables
        {
            get { return _sessionVariableAdapter; }
        }

        /// <summary>
        /// Gibt die Sitzung des aktuellen Threads zurück.
        /// </summary>
        [ThreadStatic]
        public static ServerSession CurrentSession;        
    }
}
