using System;
using System.Collections;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication
{
    /// <summary>
    /// Speichert Daten, die über Prozessgrenzen hinweg implizit im Aufrufkontext übertragen werden können.
    /// </summary>
    [Serializable]
    public class LogicalCallContextData : ILogicalThreadAffinative
    {
        // Datenspeicher
        private Hashtable _store = null;

        /// <summary>
        /// Erstellt eine neue Instanz von LogicalCallContextData.
        /// </summary>
        public LogicalCallContextData()
        {
            // Neuen Datenspeicher erzeugen
            _store = new Hashtable();
        }

        /// <summary>
        /// Gibt den Datenspeicher zurück.
        /// </summary>
        public Hashtable Store
        {
            get { return _store; }
        }
    }

}
