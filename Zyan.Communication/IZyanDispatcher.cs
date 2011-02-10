using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Zyan.Communication.Security;
using Zyan.Communication.Notification;

namespace Zyan.Communication
{
    /// <summary>
    /// Schnittstelle für Fabriken zur Produktion verteilbarer Komponenten. 
    /// </summary>
    public interface IZyanDispatcher
    {
        /// <summary>
        /// Ruft eine bestimmte Methode einer Komponente auf und übergibt die angegebene Nachricht als Parameter.
        /// Für jeden Aufruf wird temporär eine neue Instanz der Komponente erstellt.
        /// </summary>
        /// <param name="trackingID">Aufrufschlüssel zur Nachverfolgung</param>
        /// <param name="interfaceName">Name der Komponentenschnittstelle</param>
        /// <param name="delegateCorrelationSet">Korrelationssatz für die Verdrahtung bestimmter Delegaten und Ereignisse mit entfernten Methoden</param>
        /// <param name="methodName">Methodenname</param>
        /// <param name="paramDefs">Parameter-Definitionen</param>
        /// <param name="args">Parameter</param>        
        /// <returns>Rückgabewert</returns>
        object Invoke(Guid trackingID, string interfaceName, List<DelegateCorrelationInfo> delegateCorrelationSet, string methodName, ParameterInfo[] paramDefs, params object[] args);

        /// <summary>
        /// Gibt eine Liste mit allen registrierten Komponenten zurück.
        /// </summary>
        /// <returns>Liste mit Namen der registrierten Komponenten</returns>
        ComponentInfo[] GetRegisteredComponents();

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

        /// <summary>
        /// Registriert einen Client für den Empfang von Benachrichtigungen bei einem bestimmten Ereignis.
        /// </summary>
        /// <param name="eventName">Ereignisname</param>
        /// <param name="handler">Delegat auf Client-Ereignisprozedur</param>
        void Subscribe(string eventName, EventHandler<NotificationEventArgs> handler);

        /// <summary>
        /// Hebt eine Registrierung für den Empfang von Benachrichtigungen eines bestimmten Ereignisses auf.
        /// </summary>
        /// <param name="eventName">Ereignisname</param>
        /// <param name="handler">Delegat auf Client-Ereignisprozedur</param>
        void Unsubscribe(string eventName, EventHandler<NotificationEventArgs> handler);

        /// <summary>
        /// Gibt die maximale Sitzungslebensdauer (in Minuten) zurück.
        /// </summary>
        int SessionAgeLimit
        {
            get;
        }

        /// <summary>
        /// Verlängert die Sitzung des Aufrufers und gibt die aktuelle Sitzungslebensdauer zurück.
        /// </summary>
        /// <returns>Sitzungslebensdauer (in Minuten)</returns>
        int RenewSession();

        /// <summary>
        /// Abonniert ein Ereignis einer Serverkomponente.
        /// </summary>
        /// <param name="interfaceName">Schnittstellenname der Serverkomponente</param>
        /// <param name="correlation">Korrelationsinformation</param>
        void AddEventHandler(string interfaceName, DelegateCorrelationInfo correlation);

        /// <summary>
        /// Entfernt das Abonnement eines Ereignisses einer Serverkomponente.
        /// </summary>
        /// <param name="interfaceName">Schnittstellenname der Serverkomponente</param>
        /// <param name="correlation">Korrelationsinformation</param>
        void RemoveEventHandler(string interfaceName, DelegateCorrelationInfo correlation);
    }
}
