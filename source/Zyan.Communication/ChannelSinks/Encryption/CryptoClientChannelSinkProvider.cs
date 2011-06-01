using System;
using System.Collections;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.ChannelSinks.Encryption
{
    /// <summary>
    /// Anbieter der clientseitigen Kanalsenke für verschlüsselte Übertragung.
    /// </summary>
	public class CryptoClientChannelSinkProvider : IClientChannelSinkProvider
	{
        // Nächster Senkenanbieter
		private IClientChannelSinkProvider _next = null;
		
        // Name des symmetrischen Verschlüsselungsalgorithmus, der verwendet werden soll
		private string _algorithm = "3DES";
		
        // Schalter für OAEP-Padding
		private bool _oaep = false;
		
        // Anzahl der Versuche
		private int _maxAttempts = 2;

        /// <summary>
        /// Gibt den Namen des zu verwendenden symmetrischen Verschlüsselungsalgorithmus zurück, oder legt ihn fest.
        /// </summary>
        public string Algorithm
        {
            get { return _algorithm; }
            set { _algorithm = value; }
        }

        /// <summary>
        /// Gibt zurück, ob OEAP-Padding aktivuert werden soll, oder legt dies fest.
        /// </summary>
        public bool Oaep
        {
            get { return _oaep; }
            set { _oaep = value; }
        }

        /// <summary>
        /// Gibt die Anzahl der Versuche zurück, oder legt sie fest.
        /// </summary>
        public int MaxAttempts
        {
            get { return _maxAttempts; }
            set { _maxAttempts = value; }
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von CryptoClientChannelSinkProvider.
        /// </summary>
		public CryptoClientChannelSinkProvider()
		{
			// Standardeinstellungen verwenden
		}

		/// <summary>
        /// Erzeugt eine neue Instanz von CryptoClientChannelSinkProvider.
        /// </summary>
		/// <param name="properties">Konfigurationseinstellungen (z.B. aus der App.config)</param>
		/// <param name="providerData">Optionale Anbieterdaten</param>
        public CryptoClientChannelSinkProvider(IDictionary properties, ICollection providerData) 
		{
		    // Alle Konfigurationseinstellungen durchlaufen
			foreach (DictionaryEntry entry in properties)
			{
                // Aktuelle Konfigurationseinstellunge auswerten
				switch ((String)entry.Key)
				{
					case "algorithm": // Verschlüsselungsalgorithmus
						_algorithm = (string)entry.Value; 
						break;

					case "oaep": // OAEP Padding-Einstellung
						_oaep = bool.Parse((string)entry.Value); 
						break;

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
			return new CryptoClientChannelSink(nextSink, _algorithm, _oaep, _maxAttempts);
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
