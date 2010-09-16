using System;
using System.Collections;

namespace Cyan.Communication
{
    /// <summary>
    /// Beschreibt die Verdrahtung eines entfernten Ausgabe-Pins.
    /// </summary>
    public class RemoteOutputPinWiring : MarshalByRefObject
    {
        /// <summary>
        /// Gibt den clientseitigen Empfängerdelegaten zurück, oder legt ihn fest.
        /// </summary>
        public object ClientReceiver
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt den serverseitigen Eigenschaftsnamen zurück, oder legt ihn fest.
        /// </summary>
        public string ServerPropertyName
        {
            get;
            set;
        }
                
        /// <summary>
        /// Ruft den verdrahteten Client-Pin auf.
        /// </summary>
        /// <param name="message">Nachricht</param>
        public void InvokeClientPin(object message)
        {
            // Nachricht weiterleiten
            ((Delegate)ClientReceiver).DynamicInvoke(message);
        }
    }
}
