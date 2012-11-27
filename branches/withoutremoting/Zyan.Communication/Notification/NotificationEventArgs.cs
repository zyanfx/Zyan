using System;

namespace Zyan.Communication.Notification
{
	/// <summary>
	/// Beschreibt Ereignisargumente für Benachrichtigungs-Ereignisse.
	/// </summary>
	[Serializable]
	public class NotificationEventArgs : EventArgs
	{
		/// <summary>
		/// Gibt die Nachricht zurück, oder legt sie fest.
		/// </summary>
		public object Message { get; set; }

		/// <summary>
		/// Erzeugt eine neue Instanz von NotificationEventArgs.
		/// </summary>
		/// <param name="message">Nachricht</param>
		public NotificationEventArgs(object message)
		{
			// Nachricht festlegen
			Message = message;
		}
	}
}
