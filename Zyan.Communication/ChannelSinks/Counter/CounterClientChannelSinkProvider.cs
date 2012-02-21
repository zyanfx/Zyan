using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication.ChannelSinks.Counter
{
    /// <summary>
    /// Anbieter der clientseitigen Kanalsenke für Gezählte Übertragung.
    /// </summary>
	public class CounterClientChannelSinkProvider : IClientChannelSinkProvider
	{
        // Nächster Senkenanbieter
		private IClientChannelSinkProvider _next = null;
			
        // Anzahl der Versuche
		private int _maxAttempts = 2;

        /// <summary>
        /// Gibt die Anzahl der Versuche zurück, oder legt sie fest.
        /// </summary>
        public int MaxAttempts
        {
            get { return _maxAttempts; }
            set { _maxAttempts = value; }
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von CounterClientChannelSinkProvider.
        /// </summary>
        public CounterClientChannelSinkProvider()
		{
			// Standardeinstellungen verwenden
		}

		/// <summary>
        /// Erzeugt eine neue Instanz von CounterClientChannelSinkProvider.
        /// </summary>
		/// <param name="properties">Konfigurationseinstellungen (z.B. aus der App.config)</param>
		/// <param name="providerData">Optionale Anbieterdaten</param>
        public CounterClientChannelSinkProvider(IDictionary properties, ICollection providerData) 
		{
		    // Alle Konfigurationseinstellungen durchlaufen
			foreach (DictionaryEntry entry in properties)
			{
                // Aktuelle Konfigurationseinstellunge auswerten
				switch ((String)entry.Key)
				{

					case "maxRetries": // Anzahl der Wiederholungsversuche 
						_maxAttempts = Convert.ToInt32((string)entry.Value); 
						
                        // Wenn die angegebene Anzahl kleiner 1 ist ...
                        if (_maxAttempts < 1) 
                            // Ausnahme werfen
                            throw new ArgumentException(LanguageResource.ArgumentException_MaxAttempts, "maxAttempts");
						
                        // Um eins hochzäjlen, da der erste Versuch auch gezählt wird
                        _maxAttempts++; 
						break;

					default: // Ansonsten ...
                        // Ausnahme werfen
						throw new ArgumentException(string.Format(LanguageResource.ArgumentException_InvalidConfigSetting,(String)entry.Key));
				}
			}
		}

        /// <summary>
        /// Erzeugt eine Senkenkette.
        /// </summary>
        /// <param name="channel">Kanal, welcher den aktuellen Senkenanbieter erzeugt hat</param>
        /// <param name="url">URL des entfernten Objekts</param>
        /// <param name="remoteChannelData">Beschreibung des Serverkanals</param>
        /// <returns>Verkettete Kanalsenke, oder null, wenn keine erstellt wurde</returns>
        public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
		{
            // Variable für nächste Kanalsenke
            IClientChannelSink nextSink = null;

            // Wenn ein Senkenanbieter für eine weitere Kanalsenke angegeben wurde ...
            if (_next != null)
            {
                // Nächste Kanalsenke vom angegebenen Senkenanbieter erstellen lassen
                nextSink = _next.CreateSink(channel, url, remoteChannelData);

                // Wenn keine Kanalsenke erzeugt wurde ...
                if (nextSink == null)
                    // null zurückgeben
                    return null;
            }
            // Kanalsenke erzeugen und in den Senkenkette einhängen
			return new CounterClientChannelSink(nextSink, _maxAttempts);
		}

        /// <summary>
        /// Gibt den nächsten Senkenanbieter zurück, oder legt ihn fest.
        /// </summary>
        public IClientChannelSinkProvider Next
        {
            get { return _next; }
            set { _next = value; }
        }
	}
}
