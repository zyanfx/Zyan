using System;

namespace Zyan.Communication.Notification
{
	/// <summary>
	/// Clientseitige Empfangsvorrichtung für Benachrichtigungen vom Server.
	/// </summary>
	public sealed class NotificationReceiver : MarshalByRefObject, IDisposable
	{
		/// <summary>
		/// Ereignis: Bei Benachrichtigung vom Server.
		/// </summary>
		private EventHandler<NotificationEventArgs> _clientHandler;

		// Ereignisname
		private string _eventName = string.Empty;

		/// <summary>
		/// Erzeugt eine neue Instanz von NotificationReceiver.
		/// </summary>
		/// <param name="eventName">Ereignisname</param>
		/// <param name="clientHandler">Delegat auf Client-Ereignisprozedur</param>
		public NotificationReceiver(string eventName, EventHandler<NotificationEventArgs> clientHandler)
		{
			// Felder füllen
			_eventName = eventName;
			_clientHandler = clientHandler;
		}

		/// <summary>
		/// Gibt den Ereignisnamen zurück.
		/// </summary>
		public string EventName
		{
			get { return _eventName; }
		}

		/// <summary>
		/// Feuert das Notify-Ereignis auf dem Client.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void FireNotifyEvent(object sender, NotificationEventArgs e)
		{
			// Wenn eine Ereignisprozedur registriert ist ...
			if (_clientHandler != null)
				// Ereignisprozedur aufrufen
				_clientHandler(this, e);
		}

		/// <summary>
		/// Inizialisiert die Lebenszeitsteuerung der Instanz.
		/// </summary>
		/// <returns></returns>
		public override object InitializeLifetimeService()
		{
			// Keine Lease zurückgeben (Objekt lebt ewig)
			return null;
		}

		//TODO: Dispose-Pattern vollständig implementieren!
		/// <summary>
		/// Gibt verwendete Ressourcen frei.
		/// </summary>
		public void Dispose()
		{
			// Wenn ein Ereignishandler existiert
			if (_clientHandler != null)
				// Ereignishandler entsorgen
				_clientHandler = null;

			// Finalisierer nicht mehr aufrufen
			GC.SuppressFinalize(this);
		}
	}
}
