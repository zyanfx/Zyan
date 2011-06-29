using System;

namespace Zyan.Communication
{
	/// <summary>
	/// Beschreibt Ereignisargumente für Aufrufabbruch-Ereignisse.
	/// </summary>
	public class InvokeCanceledEventArgs : EventArgs
	{
		/// <summary>
		/// Gibt den Nachverfolgungsschlüssel des Methodenaufrufs zurück, oder legt ihn fest.
		/// </summary>
		public Guid TrackingID { get; set; }

		/// <summary>
		/// Gibt die Ausnahme für den Abbruch zurück, oder legt sie fest.
		/// </summary>
		public Exception CancelException { get; set; }
	}
}
