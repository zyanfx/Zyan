using System;
using System.Net;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.ChannelSinks.Counter
{
	/// <summary>
	/// Anbieter f�r die serverseitige Kanalsenke zum Counter
	/// </summary>
	public class CounterServerChannelSinkProvider : IServerChannelSinkProvider
	{
		// N�chster Senkenanbieter
		private IServerChannelSinkProvider _next = null;

		/// <summary>
		/// Erzeugt eine neue Instanz von CounterServerChannelSinkProvider.
		/// </summary>
		public CounterServerChannelSinkProvider()
		{
			// Standardeinstellungen verwenden
		}

		/// <summary>
		/// Erzeugt eine neue Instanz von CounterServerChannelSinkProvider.
		/// </summary>
		/// <param name="properties">Konfigurationseinstellungen (z.B. aus der App.config)</param>
		/// <param name="providerData">Optionale Anbieterdaten</param>
		public CounterServerChannelSinkProvider(IDictionary properties, ICollection providerData)
		{
			// Alle Konfigurationseinstellungen durchlaufen
			foreach (DictionaryEntry entry in properties)
			{
				// Aktuelle Konfigurationseinstellunge auswerten
				switch ((String)entry.Key)
				{

					case "sampledata":
						break;


					default: // Ansonsten ...
						// Ausnahme werfen
						throw new ArgumentException(string.Format("Ung�ltige Konfigurationseinstellung: {0}", (String)entry.Key));
				}
			}
		}

		/// <summary>
		/// Erzeugt eine Senkenkette.
		/// </summary>
		/// <param name="channel">Kanal, f�r welchen die Senkenkette erstellt werden soll</param>
		/// <returns>Verkettete Kanalsenke, oder null, wenn keine erstellt wurde</returns>
		public IServerChannelSink CreateSink(IChannelReceiver channel)
		{
			// Variable f�r n�chste Kanalsenke
			IServerChannelSink nextSink = null;

			// Wenn ein Senkenanbieter f�r eine weitere Kanalsenke angegeben wurde ...
			if (_next != null)
			{
				// N�chste Kanalsenke vom angegebenen Senkenanbieter erstellen lassen
				nextSink = _next.CreateSink(channel);

				// Wenn keine Kanalsenke erzeugt wurde ...
				if (nextSink == null)
					// null zur�ckgeben
					return null;
			}
			// Kanalsenke erzeugen und in den Senkenkette einh�ngen
			return new CounterServerChannelSink(nextSink);
		}

		/// <summary>
		/// Ruft Einstellungen des zu Grunde liegenden Kanals ab.
		/// </summary>
		/// <param name="channelData">Kanal-Einstellungen</param>
		public void GetChannelData(System.Runtime.Remoting.Channels.IChannelDataStore channelData)
		{
			// Nichts tun, wenn keine bestimmten Kanal-Einstellungen ausgewertet werden m�sen
		}

		/// <summary>
		/// Gibt den n�chsten Senkenanbieter zur�ck, oder legt ihn fest.
		/// </summary>
		public IServerChannelSinkProvider Next
		{
			get { return _next; }
			set { _next = value; }
		}
	}
}
