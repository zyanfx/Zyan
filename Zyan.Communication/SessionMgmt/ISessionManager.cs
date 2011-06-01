using System;

namespace Zyan.Communication.SessionMgmt
{
    /// <summary>
    /// Schnittstelle für Sitzungsverwaltungskomponente.
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Gibt die maximale Sitzungslebensdauer (in Minuten) zurück oder legt sie fest.
        /// </summary>
        int SessionAgeLimit
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt den Intervall für den Sitzungs-Aufräumvorgang (in Minuten) zurück oder legt ihn fest.
        /// </summary>
        int SessionSweepInterval
        {
            get;
            set;
        }

        /// <summary>
        /// Prüft, ob eine Sitzung mit einer bestimmten Sitzungskennung.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        /// <returns>Wahr, wenn die Sitzung existiert, ansonsten Falsch</returns>
        bool ExistSession(Guid sessionID);

        /// <summary>
        /// Gibt eine bestimmte Sitzung zurück.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        /// <returns>Sitzung</returns>
        ServerSession GetSessionBySessionID(Guid sessionID);

        /// <summary>
        /// Speichert eine Sitzung.
        /// </summary>
        /// <param name="session">Sitzungsdaten</param>
        void StoreSession(ServerSession session);

        /// <summary>
        /// Löscht eine bestimmte Sitzung.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        void RemoveSession(Guid sessionID);

        /// <summary>
        /// Legt den Wert einer Sitzungsvariablen fest.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        /// <param name="name">Variablenname</param>
        /// <param name="value">Wert</param>
        void SetSessionVariable(Guid sessionID, string name, object value);

        /// <summary>
        /// Gibt den Wert einer Sitzungsvariablen zurück.
        /// </summary>
        /// <param name="sessionID">Sitzungskennung</param>
        /// <param name="name">Variablenname</param>
        /// <returns>Wert</returns>
        object GetSessionVariable(Guid sessionID, string name);

        /// <summary>
        /// Verwaltete Ressourcen freigeben.
        /// </summary>
        void Dispose();
    }
}
