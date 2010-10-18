using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace Zyan.Communication.SessionMgmt
{
    /// <summary>
    /// Komponenten für prozessinterne Sitzungsverwaltung.
    /// </summary>
    public class SqlSessionManager : ISessionManager, IDisposable
    {
        // Sitzungsauflistung
        private Dictionary<Guid, ServerSession> _sessions = null;

        // Verbindungszeichenfolge
        private string _connectionString = string.Empty;

        /// <summary>
        /// Erzeugt eine neue Instanz von InProcSessionManager.
        /// </summary>
        /// <param name="connectionString">Verbindungszeichenfolge zur SQL Server Datenbank</param>
        public SqlSessionManager(string connectionString)
        {
            // Verbindungszeichenfolge übernehmen
            _connectionString = connectionString;

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
    }
}
