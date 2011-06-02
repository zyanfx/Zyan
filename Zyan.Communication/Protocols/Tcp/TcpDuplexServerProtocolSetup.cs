using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Protocols.Tcp.DuplexChannel;
using Zyan.Communication.Security;
using Zyan.Communication.ChannelSinks.ClientAddress;

namespace Zyan.Communication.Protocols.Tcp
{
    /// <summary>
    /// Protokolleinstellungen für serverseitige bi-direktionale TCP-Kommunikation mit benutzerdefinierter Authentifizierung und Verschlüsselung.
    /// </summary>
    public class TcpDuplexServerProtocolSetup : IServerProtocolSetup
    {
        // Felder
        private string _channelName = string.Empty;
        private bool _encryption = true;
        private string _algorithm = "3DES";
        private bool _oaep = false;
        private int _tcpPort = 0;
        private IAuthenticationProvider _authProvider = null;

        private bool _tcpKeepAliveEnabled = true;
        private ulong _tcpKeepAliveTime = 30000;
        private ulong _tcpKeepAliveInterval = 1000;
        
        /// <summary>
        /// Enables or disables TCP KeepAlive.        
        /// </summary>
        public bool TcpKeepAliveEnabled
        {
            get { return _tcpKeepAliveEnabled; }
            set { _tcpKeepAliveEnabled = value; }
        }

        /// <summary>
        /// Gets or sets the TCP KeepAlive time in milliseconds.
        /// </summary>
        public ulong TcpKeepAliveTime
        {
            get { return _tcpKeepAliveTime; }
            set { _tcpKeepAliveTime = value; }
        }

        /// <summary>
        /// Gets or sets the TCP KeepAlive interval in milliseconds
        /// </summary>
        public ulong TcpKeepAliveInterval
        {
            get { return _tcpKeepAliveInterval; }
            set { _tcpKeepAliveInterval = value; }
        }

        /// <summary>
        /// Gibt die TCP-Anschlußnummer zurück, oder legt sie fest.
        /// </summary>
        public int TcpPort
        {
            get { return _tcpPort; }
            set
            {
                // Wenn keine gültige Anschlussnummer angegeben wurde...
                if (_tcpPort < 0 || _tcpPort > 65535)
                    // Ausnahme werfen
                    throw new ArgumentOutOfRangeException("tcpPort", LanguageResource.ArgumentOutOfRangeException_InvalidTcpPortRange);

                // Wert ändern
                _tcpPort = value;
            }
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
        /// Gibt zurück, ob OEAP-Padding aktivuert werden soll, oder legt dies fest.
        /// </summary>
        public bool Oeap
        {
            get { return _oaep; }
            set { _oaep = value; }
        }
        
        /// <summary>
        /// Erstellt eine neue Instanz von TcpDuplexServerProtocolSetup.
        /// </summary>        
        public TcpDuplexServerProtocolSetup()
        {
            // Zufälligen Kanalnamen vergeben
            _channelName = "TcpDuplexServerProtocolSetup" + Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Erstellt eine neue Instanz von TcpDuplexServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider)
            : this()
        {
            // Werte übernehmen
            TcpPort = tcpPort;
            AuthenticationProvider = authProvider;
        }

        /// <summary>
        /// Erstellt eine neue Instanz von TcpDuplexServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
        /// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
        /// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
        public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
            : this()
        {
            // Werte übernehmen
            TcpPort = tcpPort;
            AuthenticationProvider = authProvider;
            TcpKeepAliveEnabled = keepAlive;
            TcpKeepAliveTime = keepAliveTime;
            TcpKeepAliveInterval = KeepAliveInterval;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TcpDuplexServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption)
            : this()
        {
            // Werte übernehmen
            TcpPort = tcpPort;
            AuthenticationProvider = authProvider;
            _encryption = encryption;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TcpDuplexServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
        /// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
        /// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
        public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
            : this()
        {
            // Werte übernehmen
            TcpPort = tcpPort;
            AuthenticationProvider = authProvider;
            _encryption = encryption;
            TcpKeepAliveEnabled = keepAlive;
            TcpKeepAliveTime = keepAliveTime;
            TcpKeepAliveInterval = KeepAliveInterval;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TcpDuplexServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>
        public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm)
            : this()
        {
            // Werte übernehmen
            TcpPort = tcpPort;
            AuthenticationProvider = authProvider;
            _encryption = encryption;
            _algorithm = algorithm;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TcpDuplexServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>
        /// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
        /// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
        /// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
        public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
            : this()
        {
            // Werte übernehmen
            TcpPort = tcpPort;
            AuthenticationProvider = authProvider;
            _encryption = encryption;
            _algorithm = algorithm;
            TcpKeepAliveEnabled = keepAlive;
            TcpKeepAliveTime = keepAliveTime;
            TcpKeepAliveInterval = KeepAliveInterval;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TcpDuplexServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>        
        /// <param name="oaep">Gibt an, ob OAEP Padding verwendet werden soll</param>
        public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep)
            : this()
        {
            // Werte übernehmen
            TcpPort = tcpPort;
            AuthenticationProvider = authProvider;
            _encryption = encryption;
            _algorithm = algorithm;
            _oaep = oaep;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TcpDuplexServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>        
        /// <param name="oaep">Gibt an, ob OAEP Padding verwendet werden soll</param>
        /// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
        /// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
        /// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
        public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
            : this()
        {
            // Werte übernehmen
            TcpPort = tcpPort;
            AuthenticationProvider = authProvider;
            _encryption = encryption;
            _algorithm = algorithm;
            _oaep = oaep;
            TcpKeepAliveEnabled = keepAlive;
            TcpKeepAliveTime = keepAliveTime;
            TcpKeepAliveInterval = KeepAliveInterval;
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
                // Konfiguration für den TCP-Kanal erstellen
                System.Collections.IDictionary channelSettings = new System.Collections.Hashtable();
                channelSettings["name"] = _channelName;
                channelSettings["port"] = _tcpPort;
                channelSettings["listen"] = true;
                channelSettings["typeFilterLevel"] = TypeFilterLevel.Full;
                channelSettings["keepAliveEnabled"] = _tcpKeepAliveEnabled;
                channelSettings["keepAliveTime"] = _tcpKeepAliveTime;
                channelSettings["keepAliveInterval"] = _tcpKeepAliveInterval;
                
                // Binären Clientformatierer erzeugen
                BinaryClientFormatterSinkProvider clientFormatter = new BinaryClientFormatterSinkProvider();

                // Wenn die Kommunikation verschlüsselt werden soll ...
                if (_encryption)
                {
                    // Client-Verschlüsselungs-Kanalsenkenanbieter erzeugen
                    CryptoClientChannelSinkProvider clientEncryption = new CryptoClientChannelSinkProvider();

                    // Verschlüsselung konfigurieren
                    clientEncryption.Algorithm = _algorithm;
                    clientEncryption.Oaep = _oaep;

                    // Verschlüsselungs-Kanalsenkenanbieter hinter den Formatierer hängen
                    clientFormatter.Next = clientEncryption;
                }
                // Variable für ersten Server-Senkenanbieter in der Kette
                IServerChannelSinkProvider firstServerSinkProvider = null;

                // Binären Serverformatierer erzeugen                
                BinaryServerFormatterSinkProvider serverFormatter = new BinaryServerFormatterSinkProvider();

                // Binäre Serialisierung von komplexen Objekten aktivieren
                serverFormatter.TypeFilterLevel = TypeFilterLevel.Full;

                serverFormatter.Next = new ClientAddressServerChannelSinkProvider();

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

                // Neuen TCP-Kanal erzeugen
                channel = new TcpExChannel(channelSettings, clientFormatter, firstServerSinkProvider);
                
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

        /// <summary>
        /// Gibt den Authentifizierungsanbieter zurück.
        /// </summary>
        public IAuthenticationProvider AuthenticationProvider
        {
            get
            {
                // Authentifizierungsanbieter zurückgeben
                return _authProvider;
            }
            set
            {
                // Wenn null übergeben wurde ...
                if (value == null)
                    // Null-Authentifizierungsanbieter verwenden
                    _authProvider = new NullAuthenticationProvider();
                else
                    // Angegebenen Authentifizierungsanbieter verwenden
                    _authProvider = value;
            }
        }
    }
}
