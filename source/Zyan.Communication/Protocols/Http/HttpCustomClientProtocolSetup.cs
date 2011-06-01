﻿using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.Encryption;

namespace Zyan.Communication.Protocols.Http
{
    /// <summary>
    /// Protokolleinstellungen für clientseitige HTTP-Kommunikation mit benutzerdefinierter Authentifizierung und Verschlüsselung.
    /// </summary>
    public class HttpCustomClientProtocolSetup : IClientProtocolSetup
    {
        // Felder
        private string _channelName = string.Empty;
        private bool _encryption = true;
        private string _algorithm = "3DES";
        private bool _oaep = false;
        private int _maxAttempts = 2;
        private bool _useBinaryFormatter = true;

        /// <summary>
        /// Erzeugt eine neue Instanz von HttpCustomClientProtocolSetup.
        /// </summary>
        public HttpCustomClientProtocolSetup()
        {
            // Zufälligen Kanalnamen vergeben
            _channelName = "HttpCustomClientProtocolSetup" + Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von HttpCustomClientProtocolSetup.
        /// </summary>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        public HttpCustomClientProtocolSetup(bool encryption)
            : this()
        {
            // Werte übernehmen
            _encryption = encryption;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von HttpCustomClientProtocolSetup.
        /// </summary>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>
        public HttpCustomClientProtocolSetup(bool encryption, string algorithm)
            : this()
        {
            // Werte übernehmen
            _encryption = encryption;
            _algorithm = algorithm;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von HttpCustomClientProtocolSetup.
        /// </summary>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>
        /// <param name="maxAttempts">Anzahl der maximalen Verbindungsversuche</param>
        public HttpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts)
            : this()
        {
            // Werte übernehmen
            _encryption = encryption;
            _algorithm = algorithm;
            _maxAttempts = maxAttempts;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von HttpCustomClientProtocolSetup.
        /// </summary>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>        
        /// <param name="oaep">Gibt an, ob OAEP Padding verwendet werden soll</param>
        public HttpCustomClientProtocolSetup(bool encryption, string algorithm, bool oaep)
            : this()
        {
            // Werte übernehmen
            _encryption = encryption;
            _algorithm = algorithm;
            _oaep = oaep;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von HttpCustomClientProtocolSetup.
        /// </summary>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>
        /// <param name="maxAttempts">Anzahl der maximalen Verbindungsversuche</param>
        /// <param name="oaep">Gibt an, ob OAEP Padding verwendet werden soll</param>
        public HttpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, bool oaep)
            : this()
        {
            // Werte übernehmen
            _encryption = encryption;
            _algorithm = algorithm;
            _maxAttempts = maxAttempts;
            _oaep = oaep;
        }

        /// <summary>
        /// Gibt den Namen des zu verwendenden symmetrischen Verschlüsselungsalgorithmus zurück, oder legt ihn fest.
        /// </summary>
        public string Algorithm
        {
            get { return _algorithm; }
            set { _algorithm = value; }
        }

        /// <summary>
        /// Gibt zurück, ob OEAP-Padding aktiviert werden soll, oder legt dies fest.
        /// </summary>
        public bool Oeap
        {
            get { return _oaep; }
            set { _oaep = value; }
        }

        /// <summary>
        /// Gibt zurück, ob binäre Formatierung aktivuert werden soll, oder legt dies fest.
        /// </summary>
        public bool UseBinaryFormatter
        {
            get { return _useBinaryFormatter; }
            set { _useBinaryFormatter = value; }
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
        /// Erzeugt einen fertig konfigurierten Remoting-Kanal.
        /// <remarks>
        /// Wenn der Kanal in der aktuellen Anwendungsdomäne bereits registriert wurde, wird null zurückgegeben.
        /// </remarks>
        /// </summary>
        /// <returns>Remoting Kanal</returns>
        public IChannel CreateChannel()
        {
            // Kanal suchen
            IChannel channel = ChannelServices.GetChannel(_channelName);

            // Wenn der Kanal nicht gefunden wurde ...
            if (channel == null)
            {
                // Konfiguration für den HTTP-Kanal erstellen
                System.Collections.IDictionary channelSettings = new System.Collections.Hashtable();
                channelSettings["name"] = _channelName;
                channelSettings["port"] = 0;

                // Variable für Clientformatierer
                IClientFormatterSinkProvider clientFormatter = null;

                // Wenn binäre Formatierung verwendet werden soll ...
                if (_useBinaryFormatter)
                    // Binären Clientformatierer erzeugen                
                    clientFormatter = new BinaryClientFormatterSinkProvider();
                else
                    // SOAP Clientformatierer erzeugen                
                    clientFormatter = new SoapClientFormatterSinkProvider();

                // Wenn die Kommunikation verschlüsselt werden soll ...
                if (_encryption)
                {
                    // Client-Verschlüsselungs-Kanalsenkenanbieter erzeugen
                    CryptoClientChannelSinkProvider clientEncryption = new CryptoClientChannelSinkProvider();

                    // Verschlüsselung konfigurieren
                    clientEncryption.Algorithm = _algorithm;
                    clientEncryption.Oaep = _oaep;
                    clientEncryption.MaxAttempts = _maxAttempts;

                    // Verschlüsselungs-Kanalsenkenanbieter hinter den Formatierer hängen
                    clientFormatter.Next = clientEncryption;
                }
                // Variable für ersten Server-Senkenanbieter in der Kette
                IServerChannelSinkProvider firstServerSinkProvider = null;

                // Variable für Serverformatierer
                IServerFormatterSinkProvider serverFormatter = null;

                // Wenn binäre Formatierung verwendet werden soll ...
                if (_useBinaryFormatter)
                {
                    // Binären Serverformatierer erzeugen                
                    serverFormatter = new BinaryServerFormatterSinkProvider();

                    // Serialisierung von komplexen Objekten aktivieren
                    ((BinaryServerFormatterSinkProvider)serverFormatter).TypeFilterLevel = TypeFilterLevel.Full;
                }
                else
                {
                    // SOAP Serverformatierer erzeugen                
                    serverFormatter = new SoapServerFormatterSinkProvider();

                    // Serialisierung von komplexen Objekten aktivieren
                    ((SoapServerFormatterSinkProvider)serverFormatter).TypeFilterLevel = TypeFilterLevel.Full;
                }
                // Wenn die Kommunikation verschlüsselt werden soll ...
                if (_encryption)
                {
                    // Server-Verschlüsselungs-Kanalsenkenanbieter erzeugen
                    CryptoServerChannelSinkProvider serverEncryption = new CryptoServerChannelSinkProvider();

                    // Verschlüsselung konfigurieren
                    serverEncryption.Algorithm = _algorithm;
                    serverEncryption.Oaep = _oaep;
                    serverEncryption.RequireCryptoClient = true;

                    // Formatierer hinter den Verschlüsselungs-Kanalsenkenanbieter hängen
                    serverEncryption.Next = serverFormatter;

                    // Verschlüsselungs-Kanalsenkenanbieter als ersten Senkenanbieter festlegen
                    firstServerSinkProvider = serverEncryption;
                }
                else
                    // Server-Formatierer als ersten Senkenanbieter festlegen
                    firstServerSinkProvider = serverFormatter;

                // Neuen HTTP-Kanal erzeugen
                channel = new HttpChannel(channelSettings, clientFormatter, firstServerSinkProvider);

                // Wenn Zyan nicht mit mono ausgeführt wird ...
                if (!MonoCheck.IsRunningOnMono)
                {
                    // Sicherstellen, dass vollständige Ausnahmeinformationen übertragen werden
                    if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
                        RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
                }
                // Kanal zurückgeben
                return channel;
            }
            // Nichts zurückgeben
            return null;
        }
    }
}
