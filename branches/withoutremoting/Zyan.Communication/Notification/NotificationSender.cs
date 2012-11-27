using System;

namespace Zyan.Communication.Notification
{
	/// <summary>
	/// Serverseitige Versendevorrichtung für Benachrichtigungen vom Server.
	/// </summary>
	public class NotificationSender
	{
		// Benachrichtigungsdienst
		private NotificationService _service = null;

		// Ereignisname
		private string _eventName = string.Empty;

		/// <summary>
		/// Erzeugt eine neue Instanz von NotificationSender.
		/// </summary>
		/// <param name="service">Benachrichtigunsgdienst über welchen die Benachrichtigung(en) verschickt werden</param>
		/// <param name="eventName">Ereignisname</param>
		public NotificationSender(NotificationService service, string eventName)
		{
			// Wenn kein Dienst angegeben wurde ...
			if (service == null)
				// Ausnahme werfen
				throw new ArgumentNullException("service");

			// Felder füllen
			_service = service;
			_eventName = eventName;
		}

		/// <summary>
		/// Behandelt ein Ereignis einer Serverkomponente.
		/// </summary>
		/// <param name="sender">Herkunftsobjekt</param>
		/// <param name="e">Ereignisargumente</param>
		public void HandleServerEvent(object sender, NotificationEventArgs e)
		{
			// Ereignis feuern und Benachrichtigungen an die Clients versenden
			_service.RaiseEvent(_eventName, e.Message);
		}
	}
}
