using System;
using System.Collections;
using System.Threading;

namespace Zyan.Communication
{
    /// <summary>
    /// Abfangvorrichtung für Delegaten.
    /// </summary>
    public class DelegateInterceptor : MarshalByRefObject
    {
        /// <summary>
        /// Erzeugt eine neue Instanz der DelegateInterceptor-Klasse.
        /// </summary>
        public DelegateInterceptor()
        {
        }

        /// <summary>
        /// Gibt den clientseitigen Empfängerdelegaten zurück, oder legt ihn fest.
        /// </summary>
        public object ClientDelegate
        {
            get;
            set;
        }       
                
        /// <summary>
        /// Ruft den verdrahteten Client-Delegaten dynamisch auf.
        /// </summary>
        /// <param name="args">Argumente</param>
        public object InvokeClientDelegate(params object[] args)
        {
            // Clientdelegat als Delegat casten
            Delegate clientDelegate = (Delegate)ClientDelegate;

            // Aufruf ausführen
            return clientDelegate.DynamicInvoke(args);            
        }
    }
}
