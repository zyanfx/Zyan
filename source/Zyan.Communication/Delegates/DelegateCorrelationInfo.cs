using System;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Beschreibt Korrelationsinformationen zur Verdrahtung eines Server-Delegaten mit einer Client-Methode.
	/// </summary>
	[Serializable]
	public class DelegateCorrelationInfo : IDisposable
	{
		// Korrelationsschlüssel
		private Guid _correlationID;

		/// <summary>
		/// Erzeugt eine neue Instanz der DelegateCorrelationInfo-Klasse.
		/// </summary>
		public DelegateCorrelationInfo()
		{
			// Eindeutigen Korrelationsschlüssel erzeugen
			_correlationID = Guid.NewGuid();
		}

		/// <summary>
		/// Disposes of the <see cref="DelegateCorrelationInfo"/> instance.
		/// </summary>
		public void Dispose()
		{
			if (ClientDelegateInterceptor != null)
			{
				ClientDelegateInterceptor.Dispose();
				ClientDelegateInterceptor = null;
			}
		}

		/// <summary>
		/// Gibt den Name der serverseitigen Delegat-Eigenschaft oder der Ereignisses zurück, oder legt ihn fest.
		/// </summary>
		public string DelegateMemberName
		{
			get;
			set;
		}

		/// <summary>
		/// Gibt zurück, ob es sich um ein Ereignis handelt, oder legt diest fest.
		/// </summary>
		public bool IsEvent
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the event filter.
		/// </summary>
		public IEventFilter EventFilter
		{
			get;
			set;
		}

		/// <summary>
		/// Gibt den eindeutigen Korrelationsschlüssel zurück oder legt ihn fest.
		/// </summary>
		public Guid CorrelationID
		{
			get { return _correlationID; }
		}

		/// <summary>
		/// Gibt die clientseitige Delegaten-Abfangvorrichtung zurück, oder legt sie fest.
		/// </summary>
		public DelegateInterceptor ClientDelegateInterceptor
		{
			get;
			set;
		}
	}
}
