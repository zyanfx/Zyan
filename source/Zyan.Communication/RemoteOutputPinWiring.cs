using System;
using System.Collections;
using System.Threading;

namespace Zyan.Communication
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
        /// Gibt zurück, ob es sich um ein Ereignis handelt, oder legt diest fest.
        /// </summary>
        public bool IsEvent
        {
            get;
            set;
        }

        /// <summary>
        /// Ruft den verdrahteten Client-Pin dynamisch auf.
        /// </summary>
        /// <param name="args">Argumente</param>
        public object InvokeDynamicClientPin(params object[] args)
        {
            // Clientempfänger als Delegat casten
            Delegate clientReceiver = (Delegate)ClientReceiver;

            // Aufruf ausführen
            return clientReceiver.DynamicInvoke(args);            
        }
    }
}
