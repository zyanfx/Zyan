using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Zyan.Communication.SessionMgmt
{
    /// <summary>
    /// Komponenten für prozessinterne Sitzungsverwaltung.
    /// </summary>
    public class InProcSessionManager : ISessionManager, IDisposable
    {
        // Sitzungsauflistung
        private Dictionary<Guid, ServerSession> _sessions = null;

        /// <summary>
        /// Erzeugt eine neue Instanz von InProcSessionManager.
        /// </summary>
        public InProcSessionManager()
        {
            // Sitzungsliste erzeugen
            _sessions = new Dictionary<Guid, ServerSession>();

            // Aufräumvorgang starten
            StartSessionSweeper();
        }

        /// <summary>
        /// Prüft, ob eine Sitzung mit einer bestimmten Sitzungskennung.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <returns>Wahr, wenn die Sitzung existiert, ansonsten Falsch</returns>
        public bool ExistSession(Guid sessionID)
        {
            return _sessions.ContainsKey(sessionID);
        }

        /// <summary>
        /// Gibt eine bestimmte Sitzung zurück.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        /// <returns>Sitzung</returns>
        public ServerSession GetSessionBySessionID(Guid sessionID)
        {
            // Wenn eine Sitzung mit der angegebenen Sitzungskennung
            if (ExistSession(sessionID))
                // Sitzung abrufen und zurückgeben
                return _sessions[sessionID];

            // Nichts zurückgeben
            return null;
        }

        /// <summary>
        /// Speichert eine Sitzung.
        /// </summary>
        /// <param name="session">Sitzungsdaten</param>
        public void StoreSession(ServerSession session)
        {
            // Wenn die Sitzung noch nicht gespeichert ist ...
            if (!ExistSession(session.SessionID))
            {
                lock (_sessionLock)
                {
                    // Sitzung der Sitzungsliste zufüen
                    _sessions.Add(session.SessionID, session);
                }
            }
        }

        /// <summary>
        /// Löscht eine bestimmte Sitzung.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        public void RemoveSession(Guid sessionID)
        {
            // Wenn die Sitzung existiert ...
            if (ExistSession(sessionID))
            {
                lock (_sessionLock)
                {
                    // Sitzung aus der Sitzungsliste entfernen
                    _sessions.Remove(sessionID);
                }
            }
        }

        #region Aufräumvorgang

        // Zeitgeber für Sitzungs-Aufräumvorgang
        private System.Timers.Timer _sessionSweeper = null;

        // Sperrobjekte für Thread-Synchronisierung
        private object _sessionLock = new object();
        private object _sessionSweeperLock = new object();

        // Maximale Sitzungslebensdauer in Minuten
        private int _sessionAgeLimit = 240;

        // Intervall für den Sitzungs-Aufräumvorgang in Minuten
        private int _sessionSweepInterval = 15;

        /// <summary>
        /// Gibt die maximale Sitzungslebensdauer (in Minuten) zurück oder legt sie fest.
        /// </summary>
        public int SessionAgeLimit
        {
            get { return _sessionAgeLimit; }
            set { _sessionAgeLimit = value; }
        }

        /// <summary>
        /// Gibt den Intervall für den Sitzungs-Aufräumvorgang (in Minuten) zurück oder legt ihn fest.
        /// </summary>
        public int SessionSweepInterval
        {
            get { return _sessionSweepInterval; }
            set
            {
                // Zeitgeber stoppen
                StopSessionSweeper();

                // Intervall einstellen
                _sessionSweepInterval = value;

                // Zeitgeber starten
                StartSessionSweeper();
            }
        }

        /// <summary>
        /// Startet den Zeitgeber für den Sitzungs-Aufräumvorgang.
        /// </summary>
        private void StartSessionSweeper()
        {
            lock (_sessionSweeperLock)
            {
                // Wenn der Zeitgeber noch nicht existiert ...
                if (_sessionSweeper == null)
                {
                    // Zeitgeber für Sitzungs-Aufräumvorgang erzeugen
                    _sessionSweeper = new Timer(_sessionSweepInterval * 60000);

                    // Elapsed-Ereignis abonnieren
                    _sessionSweeper.Elapsed += new ElapsedEventHandler(_sessionSweeper_Elapsed);

                    // Zeitgeber starten
                    _sessionSweeper.Start();
                }
            }
        }

        /// <summary>
        /// Stoppt den Zeitgeber für den Sitzungs-Aufräumvorgang.
        /// </summary>
        private void StopSessionSweeper()
        {
            lock (_sessionSweeperLock)
            {
                // Wenn der Zeitgeber existiert ...
                if (_sessionSweeper != null)
                {
                    // Wenn der Zeitgeber läuft ...
                    if (_sessionSweeper.Enabled)
                        // Zeitgeber stopen
                        _sessionSweeper.Stop();

                    // Zeitgeber entsorgen
                    _sessionSweeper.Dispose();
                    _sessionSweeper = null;
                }
            }
        }

        /// <summary>
        /// Bei Intervall abgelaufene Sitzungen löschen.
        /// </summary>
        /// <param name="sender">Herkunftsobjekt</param>
        /// <param name="e">Ereignisargumente</param>
        private void _sessionSweeper_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_sessionLock)
            {
                // Abgelaufene Sitzung abrufen
                Guid[] expiredSessions = (from session in _sessions.Values
                                          where session.Timestamp.ToUniversalTime().AddMinutes(_sessionAgeLimit) < DateTime.Now.ToUniversalTime()
                                          select session.SessionID).ToArray();

                // Alle abgelaufenen Sitzungen durchlaufen
                foreach (Guid expiredSessionID in expiredSessions)
                {
                    // Sitzung entfernen
                    _sessions.Remove(expiredSessionID);
                }
            }
        }

        #endregion

        // Gibt an, ob Dispose bereits aufgerufen wurde, oder nicht
        private bool _isDisposed = false;

        /// <summary>
        /// Verwaltete Ressourcen freigeben.
        /// </summary>
        public void Dispose()
        {
            // Wenn Dispose noch nicht aufgerufen wurde ...
            if (!_isDisposed)
            {
                // Schalter setzen
                _isDisposed = true;

                // Wenn der Sitzungs-Aufräumzeitgeber noch existiert ...
                if (_sessionSweeper != null)
                {
                    // Wenn der Zeitgeber noch läuft ...
                    if (_sessionSweeper.Enabled)
                        // Zeitgeber anhalten
                        _sessionSweeper.Stop();

                    // Zeitgeber entsorgen
                    _sessionSweeper.Dispose();
                }
                // Wenn die Sitzungsliste noch existiert ...
                if (_sessions != null)
                {
                    // Sitzungsliste entsorgen
                    _sessions.Clear();
                    _sessions = null;
                }
            }
        }

        #region Sitzungsvariablen

        // Variablenspeicher
        private volatile Dictionary<Guid, Dictionary<string, object>> _sessionVarStore = null;

        // Sperrobjekt zur Threadsynchronisierunf für den Zugriff Variablenspeicher 
        private object _sessionVarsLockObject = new object();

        /// <summary>
        /// Gibt den Variablenspeicher zurück.
        /// <remarks>Zugriff ist threadsicher.</remarks>
        /// </summary>
        private Dictionary<Guid, Dictionary<string, object>> SessionVarStore
        {
            get
            {
                // Wenn noch kein Variablenspeicher existiert ...
                if (_sessionVarStore == null)
                {
                    lock (_sessionVarsLockObject)
                    {
                        // Wenn in der Zwischenzeit nicht durch einen anderen Thread ein Variablenspeicher erzeugt wurde ...
                        if (_sessionVarStore == null)
                            // Variablenspeicher erzeugen
                            _sessionVarStore = new Dictionary<Guid, Dictionary<string, object>>();
                    }
                }
                // Variablenspeicher zurückgeben
                return _sessionVarStore;
            }
        }

        /// <summary>
        /// Gibt die Variablen einer bestimmten Sitzung zurück.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <returns>Wörterbuch mit Variablen einer bestimmten Sitzung</returns>
        private Dictionary<string, object> GetSessionVars(Guid sessionID)
        {
            // Wenn noch keine Variablenauflistung existiert ...
            if (!SessionVarStore.ContainsKey(sessionID))
            {
                lock (_sessionVarsLockObject)
                {
                    // Wenn in der Zwischenzeit nicht durch einen anderen Thread eine Variablenauflistung erzeugt wurde ...
                    if (!SessionVarStore.ContainsKey(sessionID))
                        // Variablen-Auflistung erzeugen
                        SessionVarStore.Add(sessionID,new Dictionary<string, object>());
                }
            }
            // Variablenliste zurückgeben
            return SessionVarStore[sessionID];
        }

        /// <summary>
        /// Legt den Wert einer Sitzungsvariablen fest.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        /// <param name="name">Variablenname</param>
        /// <param name="value">Wert</param>
        public void SetSessionVariable(Guid sessionID, string name, object value)
        {
            // Wenn die angegebene Sitzung existiert ...
            if (ExistSession(sessionID))
            {
                lock (_sessionVarsLockObject)
                {
                    // Variablen-Auflistung abrufen
                    Dictionary<string, object> sessionVars = GetSessionVars(sessionID);

                    // Wenn bereits eine Variable mit dem angegebenen Namen existiert ...
                    if (sessionVars.ContainsKey(name))
                        // Wert ändern
                        sessionVars[name] = value;
                    else
                        // Neue Variable anlegen
                        sessionVars.Add(name, value);
                }
            }
        }

        /// <summary>
        /// Gibt den Wert einer Sitzungsvariablen zurück.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        /// <param name="name">Variablenname</param>
        /// <returns>Wert</returns>
        public object GetSessionVariable(Guid sessionID, string name)
        {
            // Wenn die angegebene Sitzung existiert ...
            if (ExistSession(sessionID))
            {
                lock (_sessionVarsLockObject)
                {
                    // Variablen-Auflistung abrufen
                    Dictionary<string, object> sessionVars = GetSessionVars(sessionID);

                    // Wenn bereits eine Variable mit dem angegebenen Namen existiert ...
                    if (sessionVars.ContainsKey(name))
                        // Wert zurückgeben
                        return sessionVars[name];             
                }
            }
            // null zurückgeben
            return null;
        }

        #endregion
    }
}
