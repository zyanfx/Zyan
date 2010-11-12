using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Notification
{
    /// <summary>
    /// Benachrichtigungsdienst.
    /// </summary>
    public class NotificationService 
    {
        // Delegat für asynchrone Benachrichtigung
        private delegate void AsyncNotificationDelegate(string eventName, NotificationEventArgs e, EventHandler<NotificationEventArgs> eventDelegate);        
        
        // Ereignis-Registrierungen
        private Dictionary<string, EventHandler<NotificationEventArgs>> _subscriptions;
        
        // Sperr-Objekt für Thread-Synchronisierung
        private object _lockObject = new Object();

        /// <summary>
        /// Erzeugt eine neue Instanz von NotificationService.
        /// </summary>
        public NotificationService()
        {
            // Wörterbuch für Ereignis-Registrierungen erzeugen
            _subscriptions = new Dictionary<string, EventHandler<NotificationEventArgs>>();
        }

        /// <summary>
        /// Registriert einen Client für den Empfang von Benachrichtigungen bei einem bestimmten Ereignis.
        /// </summary>
        /// <param name="eventName">Ereignisname</param>
        /// <param name="handler">Delegat auf Client-Ereignisprozedur</param>
        public void Subscribe(string eventName, EventHandler<NotificationEventArgs> handler)
        {
            lock (_lockObject)
            {
                // Ereignisname einheitlich klein schreiben
                eventName = eventName.ToLower();

                // Wenn bereits ein Ereignis mit diesem Namen bekannt ist ...
                if (_subscriptions.ContainsKey(eventName))                
                    // Für das vorhandene Ereignis registrieren                    
                    _subscriptions[eventName] += handler;                
                else                
                    // Neues Ereignis zufügen und Registrierung eintragen
                    _subscriptions.Add(eventName, handler);                
            }
        }

        /// <summary>
        /// Hebt eine Registrierung für den Empfang von Benachrichtigungen eines bestimmten Ereignisses auf.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="handler"></param>
        public void Unsubscribe(string eventName, EventHandler<NotificationEventArgs> handler)
        {
            lock (_lockObject)
            {
                // Ereignisname einheitlich klein schreiben
                eventName = eventName.ToLower();
                                
                // Wenn ein Ereignis mit diesem Namen bekannt ist ...
                if (_subscriptions.ContainsKey(eventName))
                    // Registrierung aufheben
                    _subscriptions[eventName] -= handler;
            }
        }

        /// <summary>
        /// Feuert ein bestimmtes Ereignis.
        /// </summary>
        /// <param name="eventName">Ereignisname</param>
        /// <param name="message">Nachricht</param>
        public void RaiseEvent(string eventName, object message)
        {
            // Ereignisname einheitlich klein schreiben
            eventName = eventName.ToLower();
            
            // Ereignisbenachrichtigungen versenden
            OnServerEvent(eventName, new NotificationEventArgs(message));
        }              

        /// <summary>
        /// Versendet Benachrichtigungen über ein bestimmtes Ereignis an alle Registrierten Clients.
        /// </summary>
        /// <param name="eventName">Ereignisname</param>
        /// <param name="e">Ereignisargumente</param>
        private void OnServerEvent(string eventName, NotificationEventArgs e)
        {
            lock (_lockObject)
            {   
                // Wenn für ein Ereignis mit dem angegebnen Namen Registrierungen vorhanden sind ...
                if (_subscriptions.ContainsKey(eventName) && _subscriptions[eventName] != null)
                {
                    // Variablen für Ereignis-Delegaten und die Aufrufliste 
                    EventHandler<NotificationEventArgs> eventDelegate = null;
                    Delegate[] invocationList = null;

                    try
                    {
                        // Aufrufliste des Ereignisses abrufen
                        invocationList = _subscriptions[eventName].GetInvocationList();
                    }
                    catch (MemberAccessException ex)
                    {
                        // Ausnahme weiterwerfen
                        throw ex;
                    }
                    // Wenn die Aufrufliste abgerufen werden konnte ...
                    if (invocationList != null)
                    {
                        // Alle Einträge der Aufrufliste durchlaufen
                        foreach (Delegate invocationItem in invocationList)
                        {
                            // Aufruf in entsprechenden Delegattypen casten
                            eventDelegate = (EventHandler<NotificationEventArgs>)invocationItem;
                            
                            // Delegaten auf asynchrone Sende-Methode erzeugen
                            AsyncNotificationDelegate innerDelegate = new AsyncNotificationDelegate(BeginSend);
                            
                            // Benachrichtigung asynchron versenden
                            innerDelegate.BeginInvoke(eventName, e, eventDelegate, null, null);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sendet eine Ereignisbenachrichtigung asynchron.
        /// </summary>
        /// <param name="eventName">Ereignisname</param>
        /// <param name="e">Ereignisargumente</param>
        /// <param name="eventDelegate">Delegat auf registrierte Client-Ereignisprozedur</param>
        private void BeginSend(string eventName, NotificationEventArgs e, EventHandler<NotificationEventArgs> eventDelegate)
        {
            try
            {
                // Versuchen, Benachrichtigung an den Client zu senden
                eventDelegate(null, e);
            }
            catch (Exception)
            {
                // Benachrichtigung fehlgeschlagen! Registrierung aufheben
                _subscriptions[eventName] -= eventDelegate;
            }
        }
    }
}
