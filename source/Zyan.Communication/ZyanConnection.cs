using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;

namespace Zyan.Communication
{
    /// <summary>
    /// Verbindung zu einem Zyan Applikatonsserver oder einer benutzerdefinierten Zyan-Serveranwendung.
    /// </summary>
    public class ZyanConnection : IDisposable
    {
        // URL zum Server-Prozess
        private string _serverUrl = string.Empty;
        
        // Liste mit allen registrierten Komponenten des verbundenen Servers
        private List<string> _registeredComponents = null;

        // Sitzungsschlüssel
        private Guid _sessionID = Guid.Empty;

        // Protokoll-Einstellungen
        private IClientProtocolSetup _protocolSetup = null;

        // Name des entfernten Komponentenhosts
        private string _componentHostName = string.Empty;
        
        /// <summary>
        /// Gibt den URL zum Server-Prozess zurück.
        /// </summary>
        public string ServerUrl
        {
            get { return _serverUrl; }           
        }

        /// <summary>
        /// Gibt den Namen des Komponentenhosts zurück.
        /// </summary>
        public string ComponentHostName
        {
            get { return _componentHostName; }
        }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="serverUrl">Server-URL (z.B. "tcp://server1:46123/ebcserver")</param>                
        public ZyanConnection(string serverUrl)
            : this(serverUrl, new TcpBinaryClientProtocolSetup() ,null)
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="serverUrl">Server-URL (z.B. "tcp://server1:46123/ebcserver")</param>                
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>
        public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup)
            : this(serverUrl, protocolSetup, null)
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="serverUrl">Server-URL (z.B. "tcp://server1:46123/ebcserver")</param>        
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>
        /// <param name="credentials">Anmeldeinformationen</param>
        public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup, Hashtable credentials)
        {
            // Wenn kein Server-URL angegeben wurde ...
            if (string.IsNullOrEmpty(serverUrl))
                // Ausnahme werfen
                throw new ArgumentException("Es wurde kein Server-URL angegeben! Bitte geben Sie einen gültigen Server-URL an, da sonst keine Verbindung zum Komponentenhost aufgebaut werden kann.", "serverUrl");

            // Wenn keine Protokoll-Einstellungen angegeben wurde ...
            if (protocolSetup == null)
                // Ausnahme werfen
                throw new ArgumentNullException("protocolSetup");

            // Protokoll-Einstellungen übernehmen
            _protocolSetup = protocolSetup;

            // Eindeutigen Sitzungsschlüssel generieren
            _sessionID = Guid.NewGuid();

            // Server-URL übernehmen
            _serverUrl = serverUrl;

            // Server-URL in Bestandteile zerlegen
            string[] addressParts=_serverUrl.Split('/');

            // Name des Komponentenhots speichern
            _componentHostName = addressParts[3];
                        
            // TCP-Kommunikationskanal öffnen
            IChannel channel = (IChannel)_protocolSetup.CreateChannel();

            // Wenn der Kanal erzeugt wurde ...
            if (channel != null)
                // Kanal registrieren
                ChannelServices.RegisterChannel(channel, false);

            // Am Server anmelden
            RemoteComponentFactory.Logon(_sessionID, credentials);

            // Registrierte Komponenten vom Server abrufen
            _registeredComponents = new List<string>(RemoteComponentFactory.GetRegisteredComponents());
        }

        /// <summary>
        /// Erzeugt im Server-Prozess eine neue Instanz einer bestimmten Komponente und gibt einen Proxy dafür zurück.
        /// </summary>
        /// <typeparam name="T">Typ der öffentlichen Schnittstelle der zu konsumierenden Komponente</typeparam>        
        /// <returns>Proxy</returns>
        public T CreateProxy<T>()
        { 
            // Andere Überladung aufrufen
            return CreateProxy<T>(false);             
        }

        /// <summary>
        /// Erzeugt im Server-Prozess eine neue Instanz einer bestimmten Komponente und gibt einen Proxy dafür zurück.
        /// </summary>
        /// <typeparam name="T">Typ der öffentlichen Schnittstelle der zu konsumierenden Komponente</typeparam>
        /// <param name="implicitTransactionTransfer">Implizite Transaktionsübertragung</param>
        /// <returns>Proxy</returns>
        public T CreateProxy<T>(bool implicitTransactionTransfer)
        {
            // Typeninformationen lesen
            Type interfaceType = typeof(T);

            // Schnittstellenname lesen
            string interfaceName=interfaceType.FullName;

            // Wenn keine Schnittstelle angegeben wurde ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ApplicationException(string.Format("Der angegebene Typ '{0}' ist keine Schnittstelle! Für die Erzeugung einer entfernten Komponenteninstanz, wird deren öffentliche Schnittstelle benötigt!", interfaceName));

            // Wenn für die Schnittstelle auf dem verbundenen Server keine Komponente registriert ist ...
            if (!_registeredComponents.Contains(interfaceName))
                // Ausnahme werfne
                throw new ApplicationException(string.Format("Für Schnittstelle '{0}' ist auf dem Server '{1}' keine Komponente registriert.", interfaceName, _serverUrl));

            // Fabrik abrufen
            IComponentInvoker factory=RemoteComponentFactory;
            
            // Proxy erzeugen
            ZyanProxy proxy = new ZyanProxy(typeof(T), factory,implicitTransactionTransfer,_sessionID,_componentHostName);

            // Proxy transparent machen und zurückgeben
            return (T)proxy.GetTransparentProxy();
        }

        // Proxy für den Zugriff auf die entfernte Komponentenfabrik des Komponentenhosts
        private IComponentInvoker _remoteComponentFactory = null;

        /// <summary>
        /// Gibt einen Proxy für den Zugriff auf die entfernte Komponentenfabrik des Komponentenhosts zurück.
        /// </summary>
        private IComponentInvoker RemoteComponentFactory
        {
            get
            { 
                // Wenn noch keine Verbindung zur entfernten Komponentenfabrik existiert ...
                if (_remoteComponentFactory == null)
                {
                    // Verbindung zur entfernten Komponentenfabrik herstellen
                    _remoteComponentFactory = (IComponentInvoker)Activator.GetObject(typeof(IComponentInvoker), _serverUrl);
                }
                // Fabrik-Proxy zurückgeben
                return _remoteComponentFactory;
            }
        }

        /// <summary>
        /// Öffnet einen TCP-Kanal zum senden von Nachrichten an einen Komponentenhost.
        /// </remarks>
        /// </summary>      
        /// <param name="enableSecurity">Schalter für Sicherheit</param>
        /// <param name="impersonate">Schalter für Impersonierung</param>
        private void OpenTcpChannel(bool enableSecurity, bool impersonate)
        {
            // Kanalnamen erzeugen
            string channelName = _serverUrl;

            // Kanal suchen
            IChannel channel = ChannelServices.GetChannel(channelName);

            // Wenn der Kanal nicht gefunden wurde ...
            if (channel == null)
            {
                // Konfiguration für den TCP-Kanal erstellen
                System.Collections.IDictionary channelSettings = new System.Collections.Hashtable();
                channelSettings["name"] = channelName;
                channelSettings["port"] = 0;
                channelSettings["secure"] = enableSecurity;
                channelSettings["socketCacheTimeout"] = 0;
                channelSettings["socketCachePolicy"] = SocketCachePolicy.Default;
                                
                // Wenn Sicherheit aktiviert ist ...
                if (enableSecurity)
                {
                    // Impersonierung entsprechend der Einstellung aktivieren oder deaktivieren
                    channelSettings["tokenImpersonationLevel"] = impersonate ? System.Security.Principal.TokenImpersonationLevel.Impersonation : System.Security.Principal.TokenImpersonationLevel.Identification;

                    // Signatur und Verschlüssung explizit aktivieren
                    channelSettings["protectionLevel"] = System.Net.Security.ProtectionLevel.EncryptAndSign;
                }
                // Binäre Serialisierung von komplexen Objekten aktivieren
                BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
                provider.TypeFilterLevel = TypeFilterLevel.Full;
                BinaryClientFormatterSinkProvider clientFormatter = new BinaryClientFormatterSinkProvider();
                
                // Neuen TCP-Kanal erzeugen
                channel = new TcpChannel(channelSettings, clientFormatter, provider);

                // Sicherstellen, dass vollständige Ausnahmeinformationen übertragen werden
                RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

                // Kanal registrieren
                ChannelServices.RegisterChannel(channel, enableSecurity);
            }
        }

        // Schalter der angibt, ob Dispose bereits aufgerufen wurde
        private bool _isDisposed = false;

        /// <summary>
        /// Verwaltete Ressourcen freigeben.
        /// </summary>
        public void Dispose()
        {
            // Wenn Dispose nicht bereits ausgeführt wurde ...
            if (!_isDisposed)
            { 
                // Schalter setzen
                _isDisposed = true;

                // Vom Server abmelden
                RemoteComponentFactory.Logoff(_sessionID);

                // Variablen freigeben
                _registeredComponents = null;
                _remoteComponentFactory = null;
                _serverUrl = string.Empty;
                _sessionID = Guid.Empty;

                // Nicht auf Finalisierer warten (da keine unverwalteten Ressourcen freigegeben werden müssen)
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Destruktor.
        /// </summary>
        ~ZyanConnection()
        { 
            // Ressourcen freigeben
            Dispose();
        }
    }
}
