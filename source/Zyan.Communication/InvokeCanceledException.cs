using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Zyan.Communication
{    
    /// <summary>
    /// Ausnahme, die geworfen wird, wenn ein Methodenaufruf abgebrochen wurde.
    /// </summary>
    [Serializable]
    public class InvokeCanceledException : Exception, ISerializable
    {
        /// <summary>
        /// Erstellt eine neue Instanz von InvokeCanceledException.
        /// </summary>
        public InvokeCanceledException()
        {
        }

        /// <summary>
        /// Erstellt eine neue Instanz von InvokeCanceledException.
        /// </summary>
        /// <param name="message">Fehlermeldung</param>
        public InvokeCanceledException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Erstellt eine neue Instanz von InvokeCanceledException.
        /// </summary>
        /// <param name="message">Fehlermeldung</param>
        /// <param name="innerException">Ausnahme, welche diese Ausnahme verursacht hat</param>
        public InvokeCanceledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Erstellt eine neue Instanz von InvokeCanceledException.
        /// </summary>
        /// <param name="info">Serialisierungsinformationen</param>
        /// <param name="context">Streaming-Kontext der Serialisierung</param>
        protected InvokeCanceledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gibt Objektdaten für die Serialisierung zurück.
        /// </summary>
        /// <param name="info">Serialisierungsinformationen</param>
        /// <param name="context">Streaming-Kontext der Serialisierung</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
