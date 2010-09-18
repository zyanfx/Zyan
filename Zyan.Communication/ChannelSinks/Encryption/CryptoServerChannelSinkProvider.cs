using System;
using System.Net;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.ChannelSinks.Encryption
{
    /// <summary>
    /// Anbieter für die serverseitige Kanalsenke zur verschlüsselten Kommunikation.
    /// </summary>
	public class CryptoServerChannelSinkProvider : IServerChannelSinkProvider
	{
	    // Nächster Senkenanbieter
		private IServerChannelSinkProvider _next = null;

        // Name des symmetrischen Verschlüsselungsalgorithmus, der verwendet werden soll
        private string _algorithm = "3DES";

        // Schalter für OAEP-Padding
        private bool _oaep = false;
		
        // Gibt an, ob clientseitig auch entsprechende Verschlüsselungs-Kanalsenken vorhanden sein müssen
		private bool _requireCryptoClient = false;
		
        // Lebenszeit einer Clientverbindung in Sekunden
		private double _connectionAgeLimit = 60.0;
		
        // Intervall für den Aufräumvorgang alter Verbindungen in Sekunden
		private double _sweepFrequency = 15.0;
		
        // Client-IP Ausnahmeliste
		private IPAddress [] _securityExemptionList = null;

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
        /// Gibt zurück, ob der zwingend auch verschlüsseln muss, oder legt dies fest.
        /// </summary>
        public bool RequireCryptoClient
        {
            get { return _requireCryptoClient; }
            set { _requireCryptoClient = value; }
        }

		/// <summary>
        /// Erzeugt eine neue Instanz von CryptoServerChannelSinkProvider.
        /// </summary>
        public CryptoServerChannelSinkProvider()
		{
            // Standardeinstellungen verwenden
		}

		/// <summary>
        /// Erzeugt eine neue Instanz von CryptoServerChannelSinkProvider.
        /// </summary>
        /// <param name="properties">Konfigurationseinstellungen (z.B. aus der App.config)</param>
        /// <param name="providerData">Optionale Anbieterdaten</param>
        public CryptoServerChannelSinkProvider(IDictionary properties, ICollection providerData)
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

					case "connectionAgeLimit": // Maximale Lebenszeit einer Verbindung
						_connectionAgeLimit = double.Parse((string)entry.Value);
                         
						if (_connectionAgeLimit < 0)
                            throw new ArgumentException("Einstellung 'connectionAgeLimit' muss 0 oder größer sein.", "_connectionAgeLimit");
						break;

					case "sweepFrequency":
						_sweepFrequency = double.Parse((string)entry.Value);
 
						if (_sweepFrequency < 0) 
                            throw new ArgumentException("Einstellung 'sweepFrequency' muss 0 oder größer sein.", "_sweepFrequency");
						break;

                    case "requireCryptoClient":
						_requireCryptoClient = bool.Parse((string)entry.Value);
						break;

					case "securityExemptionList":
						string ipList = (string)entry.Value;
						if (ipList != null && ipList != string.Empty) 
						{
							string [] values = ipList.Split(';');
							_securityExemptionList = new IPAddress[values.Length];
							for(int i=0; i<values.Length; i++) _securityExemptionList[i] = IPAddress.Parse(values[i].Trim());
						}
						break;

                    default: // Ansonsten ...
                        // Ausnahme werfen
                        throw new ArgumentException(string.Format("Ungültige Konfigurationseinstellung: {0}", (String)entry.Key));
				}
			}
		}

        /// <summary>
        /// Erzeugt eine Senkenkette.
        /// </summary>
        /// <param name="channel">Kanal, für welchen die Senkenkette erstellt werden soll</param>
        /// <returns>Verkettete Kanalsenke, oder null, wenn keine erstellt wurde</returns>
        public IServerChannelSink CreateSink(IChannelReceiver channel)
		{
            // Variable für nächste Kanalsenke
            IServerChannelSink nextSink = null;

            // Wenn ein Senkenanbieter für eine weitere Kanalsenke angegeben wurde ...
            if (_next != null)
            {
                // Nächste Kanalsenke vom angegebenen Senkenanbieter erstellen lassen
                nextSink = _next.CreateSink(channel);

                // Wenn keine Kanalsenke erzeugt wurde ...
                if (nextSink == null)
                    // null zurückgeben
                    return null;
            }
            // Kanalsenke erzeugen und in den Senkenkette einhängen
			return new CryptoServerChannelSink(nextSink, _algorithm, _oaep, 
				                               _connectionAgeLimit, _sweepFrequency, 
				                               _requireCryptoClient, _securityExemptionList);
		}

        /// <summary>
        /// Ruft Einstellungen des zu Grunde liegenden Kanals ab.
        /// </summary>
        /// <param name="channelData">Kanal-Einstellungen</param>
        public void GetChannelData(System.Runtime.Remoting.Channels.IChannelDataStore channelData)
        {
            // Nichts tun, wenn keine bestimmten Kanal-Einstellungen ausgewertet werden müsen
        }

        /// <summary>
        /// Gibt den nächsten Senkenanbieter zurück, oder legt ihn fest.
        /// </summary>
        public IServerChannelSinkProvider Next
        {
            get { return _next; }
            set { _next = value; }
        }		
	}
}
