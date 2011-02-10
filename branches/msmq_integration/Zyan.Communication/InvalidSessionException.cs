using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Zyan.Communication
{
    /// <summary>
    /// Ausnahme, die geworfen wird, wenn die Sitzung ungültig oder aufgelaufen ist.
    /// </summary>
	[Serializable]
	public class InvalidSessionException : Exception, ISerializable
	{	
        /// <summary>
        /// Erstellt eine neue Instanz von InvalidSessionException.
        /// </summary>
		public InvalidSessionException()
		{
		}
		
		/// <summary>
        /// Erstellt eine neue Instanz von InvalidSessionException.
        /// </summary>
        /// <param name="message">Fehlermeldung</param>
		public InvalidSessionException(string message) : base(message)
		{
		}

		/// <summary>
        /// Erstellt eine neue Instanz von InvalidSessionException.
        /// </summary>
        /// <param name="message">Fehlermeldung</param>
        /// <param name="innerException">Ausnahme, welche diese Ausnahme verursacht hat</param>
		public InvalidSessionException(string message, Exception innerException) : base(message, innerException)
		{
		}

        /// <summary>
        /// Erstellt eine neue Instanz von InvalidSessionException.
        /// </summary>
        /// <param name="info">Serialisierungsinformationen</param>
        /// <param name="context">Streaming-Kontext der Serialisierung</param>
        protected InvalidSessionException(SerializationInfo info, StreamingContext context) : base(info, context)
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
