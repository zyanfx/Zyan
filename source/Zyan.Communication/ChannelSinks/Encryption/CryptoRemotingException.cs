using System;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
    /// Ausnahme, die geworfen wird, wenn bei dem ver- bzw. entschlüsseln der Remoting-Kommunikation etwas schief geht.
    /// </summary>
	[Serializable]
	public class CryptoRemotingException : RemotingException, ISerializable
	{	
        /// <summary>
        /// Erstellt eine neue Instanz von CryptoRemotingException.
        /// </summary>
		public CryptoRemotingException()
		{
		}
		
		/// <summary>
        /// Erstellt eine neue Instanz von CryptoRemotingException.
        /// </summary>
        /// <param name="message">Fehlermeldung</param>
		public CryptoRemotingException(string message) : base(message)
		{
		}

		/// <summary>
        /// Erstellt eine neue Instanz von CryptoRemotingException.
        /// </summary>
        /// <param name="message">Fehlermeldung</param>
        /// <param name="innerException">Ausnahme, welche diese Ausnahme verursacht hat</param>
		public CryptoRemotingException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
        /// Erstellt eine neue Instanz von CryptoRemotingException.
        /// </summary>
		/// <param name="info">Serialisirungsinformationen</param>
        /// <param name="context">Streaming-Kontext der Serialisierung</param>
		protected CryptoRemotingException(SerializationInfo info, StreamingContext context) : base(info, context)
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
