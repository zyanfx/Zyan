using System;
using System.Security.Permissions;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

namespace Zyan.Communication.ChannelSinks.Counter
{
	/// <summary>
	/// Ausnahme, die geworfen wird, wenn bei dem Zählen der Remoting-Kommunikation etwas schief geht.
	/// </summary>
	[Serializable]
	public class CounterRemotingException : RemotingException, ISerializable
	{
		/// <summary>
		/// Erstellt eine neue Instanz von CounterRemotingException.
		/// </summary>
		public CounterRemotingException()
		{
		}

		/// <summary>
		/// Erstellt eine neue Instanz von CounterRemotingException.
		/// </summary>
		/// <param name="message">Fehlermeldung</param>
		public CounterRemotingException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Erstellt eine neue Instanz von CounterRemotingException.
		/// </summary>
		/// <param name="message">Fehlermeldung</param>
		/// <param name="innerException">Ausnahme, welche diese Ausnahme verursacht hat</param>
		public CounterRemotingException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Erstellt eine neue Instanz von CounterRemotingException.
		/// </summary>
		/// <param name="info">Serialisirungsinformationen</param>
		/// <param name="context">Streaming-Kontext der Serialisierung</param>
		protected CounterRemotingException(SerializationInfo info, StreamingContext context)
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
