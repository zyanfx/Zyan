using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Zyan.Communication
{
    /// <summary>
    /// Beschreibt eine konkretee Aufrufabfangaktion.
    /// </summary>
    public class CallInterceptionData
    {        
        /// <summary>
        /// Erstellt eine neue Instanz der CallInterceptionData-Klasse.
        /// </summary>        
        /// <param name="parameters">Parameterwerte des abgefangenen Aufrufs</param>
        public CallInterceptionData(object[] parameters)
        {
            // Felder füllen
            Intercepted = false;
            ReturnValue = null;            
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