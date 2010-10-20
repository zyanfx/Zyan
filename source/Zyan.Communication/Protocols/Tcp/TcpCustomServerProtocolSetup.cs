using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Net.Security;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.Security;
using Zyan.Communication.ChannelSinks.Encryption;

namespace Zyan.Communication.Protocols.Tcp
{
    /// <summary>
    /// Protokolleinstellungen für serverseitige TCP-Kommunikation mit benutzerdefinierter Authentifizierung und Verschlüsselung.
    /// </summary>
    public class TcpCustomServerProtocolSetup : IServerProtocolSetup
    {        
        // Felder
        private string _channelName = string.Empty;
        private bool _encryption = true;
        private string _algorithm = "3DES";
        private bool _oaep = false;
        private int _tcpPort = 0;
        private IAuthenticationProvider _authProvider=null;

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
                    throw new ArgumentOutOfRangeException("tcpPort", "Die TCP-Anschlussnummer muss im Bereich 0-65535 liegen!");
                
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
        /// Gibt zurück, ob Socket-Caching aktiviert ist, oder legt dies fest.
        /// </summary>
        public bool SocketCachingEnabled
        { get; set; }

        /// <summary>
        /// Erstellt eine neue Instanz von TcpCustomServerProtocolSetup.
        /// </summary>        
        public TcpCustomServerProtocolSetup()
        {
            // Socket-Caching standardmäßig einschalten
            SocketCachingEnabled = true;

            // Zufälligen Kanalnamen vergeben
            _channelName = "TcpCustomServerProtocolSetup_" + Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Erstellt eine neue Instanz von TcpCustomServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        public TcpCustomServerProtocolSetup(int tcpPort,IAuthenticationProvider authProvider) : this()
        {
            // Werte übernehmen
            TcpPort = tcpPort;
            AuthenticationProvider=authProvider;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TcpCustomServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        public TcpCustomServerProtocolSetup(int tcpPort,IAuthenticationProvider authProvider,bool encryption) : this()
        { 
            // Werte übernehmen
            TcpPort=tcpPort;
            AuthenticationProvider=authProvider;
            _encryption = encryption;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TcpCustomServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>
        public TcpCustomServerProtocolSetup(int tcpPort,IAuthenticationProvider authProvider,bool encryption, string algorithm) : this()
        {
            // Werte übernehmen
            TcpPort=tcpPort;
            AuthenticationProvider=authProvider;
            _encryption = encryption;
            _algorithm = algorithm;
        }

        /// <summary>
        /// Erzeugt eine neue Instanz von TcpCustomServerProtocolSetup.
        /// </summary>
        /// <param name="tcpPort">TCP-Anschlußnummer</param>
        /// <param name="authProvider">Authentifizierungsanbieter</param>
        /// <param name="encryption">Gibt an, ob die Kommunikation verschlüssel werden soll</param>
        /// <param name="algorithm">Verschlüsselungsalgorithmus (z.B. "3DES")</param>        
        /// <param name="oaep">Gibt an, ob OAEP Padding verwendet werden soll</param>
        public TcpCustomServerProtocolSetup(int tcpPort,IAuthenticationProvider authProvider,bool encryption, string algorithm, bool oaep) : this()
        {
            // Werte übernehmen
            TcpPort=tcpPort;
            AuthenticationProvider=authProvider;
            _encryption = encryption;
            _algorithm = algorithm;
            _oaep = oaep;
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
                channelSettings["socketCacheTimeout"] = 0;
                channelSettings["socketCachePolicy"] = SocketCachingEnabled ? SocketCachePolicy.Default : SocketCachePolicy.AbsoluteTimeout;

                // Standardmäßige Windows-Authentifizierung des Remoting TCP-Kanals abschalten
                channelSettings["secure"] = false;
                
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
                IServerChannelSinkProvider firstServerSinkProvider=null;
                
                // Binären Serverformatierer erzeugen                
                BinaryServerFormatterSinkProvider serverFormatter = new BinaryServerFormatterSinkProvider();

                // Binäre Serialisierung von komplexen Objekten aktivieren
                serverFormatter.TypeFilterLevel = TypeFilterLevel.Full;

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
                channel = new TcpChannel(channelSettings, clientFormatter, firstServerSinkProvider);

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
                if (value==null)
                    // Null-Authentifizierungsanbieter verwenden
                    _authProvider=new NullAuthenticationProvider();
                else
                    // Angegebenen Authentifizierungsanbieter verwenden
                    _authProvider=value;
            }
        }
    }
}
