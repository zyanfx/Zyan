using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cyan.Communication
{
    /// <summary>
    /// Verwaltet Benutzersitzungen.
    /// </summary>
    public class SessionManager
    {
        #region Singleton-Implementierung

        // Sperr-Objekt
        private static volatile object _lockObject = new object();

        /// <summary>
        /// Erstellt eine neue Instanz von SessionManager.
        /// </summary>
        private SessionManager()
        {

        }

        // Singleton-Instanz
        private static SessionManager _singelton = new SessionManager();

        /// <summary>
        /// Gibt die Singleton-Instanz des Sitzungs-Managers zurück.
        /// </summary>
        public static SessionManager Instance
        {
            get 
            { 
                // Wenn keine Singleton-Instanz existiert ...
                if (_singelton == null)
                {
                    lock (_lockObject)
                    {
                        // Erneute Prüfung, ob die Instanz existiert (Double Lock-Methode)
                        if (_singelton==null)
                            // Singleton-Instanz erzeugen
                            _singelton = new SessionManager();
                    }
                }
                // Singleton-Instanz zurückgeben
                return _singelton;
            }
        }

        #endregion

        #region Sitzungen verwalten



        #endregion
    }
}
