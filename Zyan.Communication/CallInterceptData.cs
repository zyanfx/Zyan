using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Zyan.Communication
{
    /// <summary>
    /// Konkrete Aufrufabfangaktion.
    /// </summary>
    public class CallInterceptData
    {
        /// <summary>
        /// Erstellt eine neue Instanz der CallInterceptData-Klasse.
        /// </summary>
        /// <param name="subscription">Aufrufabfangregistrierung</param>
        /// <param name="parameters">Parameterwerte des abgefangenen Aufrufs</param>
        public CallInterceptData(CallInterceptor subscription, object[] parameters)
        {
            // Felder füllen
            Intercepted = false;
            ReturnValue = null;
            Subscription = subscription;
            Parameters = parameters;
        }

        /// <summary>
        /// Gibt zurück, ob der Aufruf abgefangen wurde, oder legt dies fest.
        /// </summary>
        public bool Intercepted
        {
            get;
            set;
        }

        /// <summary>
        /// Gibt den zu verwendenden Rückgabewert zurück, oder legt ihn fest.
        /// </summary>
        public object ReturnValue
        {
            get;
            set;
        }

        public CallInterceptor Subscription
        {
            get;
            private set;
        }

        /// <summary>
        /// Gibt ein Array der Parameterwerten zurück, mit welchen die abzufangende Methode aufgerufen wurde, oder legt sie fest.
        /// </summary>
        public object[] Parameters
        {
            get;
            set;
        }
    }
}