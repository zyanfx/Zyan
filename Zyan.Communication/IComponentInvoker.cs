using System;
using System.Collections;
using System.Collections.Generic;
using Zyan.Communication.Security;

namespace Zyan.Communication
{
    /// <summary>
    /// Schnittstelle für Fabriken zur Produktion verteilbarer Komponenten. 
    /// </summary>
    public interface IComponentInvoker
    {
        /// <summary>
        /// Ruft eine bestimmte Methode einer Komponente auf und übergibt die angegebene Nachricht als Parameter.
        /// Für jeden Aufruf wird temporär eine neue Instanz der Komponente erstellt.
        /// </summary>
        /// <param name="trackingID">Aufrufschlüssel zur Nachverfolgung</param>
        /// <param name="interfaceName">Name der Komponentenschnittstelle</param>
        /// <param name="outputPinCorrelationSet">Korrelationssatz für die Verdrahtung bestimmter Ausgabe-Pins mit entfernten Methoden</param>
        /// <param name="methodName">Methodenname</param>
        /// <param name="args">Parameter</param>
        /// <returns>Rückgabewert</returns>
        object Invoke(Guid trackingID, string interfaceName, ArrayList outputPinCorrelationSet, string methodName, params object[] args);

        /// <summary>
        /// Gibt eine Liste mit allen registrierten Komponenten zurück.
        /// </summary>
        /// <returns>Liste mit Namen der registrierten Komponenten</returns>
        string[] GetRegisteredComponents();

        /// <summary>
        /// Meldet einen Client am Applikationserver an.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel (wird vom Client erstellt)</param>
        /// <param name="credentials">Anmeldeinformationen</param>
        void Logon(Guid sessionID, Hashtable credentials);

        /// <summary>
        /// Meldet einen Client vom Applikationsserver ab.
        /// </summary>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        void Logoff(Guid sessionID);
    }
}
