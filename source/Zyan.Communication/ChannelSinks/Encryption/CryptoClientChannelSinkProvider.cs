using System;
using System.Collections;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.ChannelSinks.Encryption
{
    /// <summary>
    /// Anbieter der clientseitigen Kanalsenke f�r verschl�sselte �bertragung.
    /// </summary>
	public class CryptoClientChannelSinkProvider : IClientChannelSinkProvider
	{
        // N�chster Senkenanbieter
		private IClientChannelSinkProvider _next = null;
		
        // Name des symmetrischen Verschl�sselungsalgorithmus, der verwendet werden soll
		private string _algorithm = "3DES";
		
        // Schalter f�r OAEP-Padding
		private bool _oaep = false;
		
        // Anzahl der Versuche
		private int _maxAttempts = 2;

        /// <summary>
        /// Gibt den Namen des zu verwendenden symmetrischen Verschl�sselungsalgorithmus zur�ck, oder legt ihn fest.
        /// </summary>
        public string Algorithm
        {
            get { return _algorithm; }
            set { _algorithm = value; }
        }

        /// <summary>
        /// Gibt zur�ck, ob OEAP-Padding aktivuert werden soll, oder legt dies fest.
        /// </summary>
        public bool Oaep
        {
            get { return _oaep; }
            set { _oaep = value; }
        }

        /// <summary>
        /// Gibt die Anzahl der Versuche zur�ck, oder legt sie fest.
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
					case "algorithm": // Verschl�sselungsalgorithmus
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
						
                        // Um eins hochz�jlen, da der erste Versuch auch gez�hlt wird
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
            // Variable f�r n�chste Kanalsenke
            IClientChannelSink nextSink = null;

            // Wenn ein Senkenanbieter f�r eine weitere Kanalsenke angegeben wurde ...
            if (_next != null)
            {
                // N�chste Kanalsenke vom angegebenen Senkenanbieter erstellen lassen
                nextSink = _next.CreateSink(channel, url, remoteChannelData);

                // Wenn keine Kanalsenke erzeugt wurde ...
                if (nextSink == null)
                    // null zur�ckgeben
                    return null;
            }
            // Kanalsenke erzeugen und in den Senkenkette einh�ngen
			return new CryptoClientChannelSink(nextSink, _algorithm, _oaep, _maxAttempts);
		}

        /// <summary>
        /// Gibt den n�chsten Senkenanbieter zur�ck, oder legt ihn fest.
        /// </summary>
        public IClientChannelSinkProvider Next
        {
            get { return _next; }
            set { _next = value; }
        }
	}
}
