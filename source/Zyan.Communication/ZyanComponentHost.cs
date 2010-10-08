using System;
using System.Timers;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.SessionMgmt;

namespace Zyan.Communication
{    
    /// <summary>
    /// Laufzeitumgebung für Ereignisbasierte Komponenten (Komponentenhost).
    /// </summary>
    public class ZyanComponentHost : IDisposable
    {
        // Felder
        private ComponentInvoker _invoker = null;

        // Authentifizierungsanbieter
        private IAuthenticationProvider _authProvider = null;
        
        // Protokoll-Einstellungen
        private IServerProtocolSetup _protocolSetup = null;               

        #region Konstruktor
      
        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="name">Name des Komponentenhosts</param>        
        /// <param name="tcpPort">TCP-Anschlussnummer</param>                
        public ZyanComponentHost(string name, int tcpPort) : this(name, new TcpBinaryServerProtocolSetup(tcpPort), new InProcSessionManager())
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="name">Name des Komponentenhosts</param>        
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>        
        public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup) : this(name,protocolSetup,new InProcSessionManager())
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="name">Name des Komponentenhosts</param>        
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>        
        public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup, ISessionManager sessionManager)
        {
            // Wenn kein Name angegeben wurde ...
            if (string.IsNullOrEmpty(name))
                // Ausnahme werfen
                throw new ArgumentException("Für den Komponentenhost wurde kein Name angegeben!","name");

            // Wenn keine Protokoll-Einstellungen angegeben wurde ...
            if (protocolSetup == null)
                // Ausnahme werfen
                throw new ArgumentNullException("protocolSetup");

            // Wenn keine Sitzungsverwaltung übergeben wurde ...
            if (sessionManager == null)
                // Ausnahme werfen
                throw new ArgumentNullException("sessionManager");

            // Werte übernehmen
            _name = name;
            _protocolSetup = protocolSetup;
            _sessionManager = sessionManager;
                        
            // Komponentenaufrufer erzeugen
            _invoker = new ComponentInvoker(this);

            // Authentifizierungsanbieter übernehmen und verdrahten
            _authProvider = protocolSetup.AuthenticationProvider;
            this.Authenticate = _authProvider.Authenticate;

            // Beginnen auf Client-Anfragen zu horchen
            StartListening();
        }
        
        #endregion
        
        #region Authentifizierung

        /// <summary>
        /// Ausgangs-Pin: Authentifizierungsanfrage
        /// </summary>
        public Func<AuthRequestMessage, AuthResponseMessage> Authenticate;     

        #endregion

        #region Sitzungsverwaltung        

        // Sitzungsverwaltung
        private ISessionManager _sessionManager = null;

        /// <summary>
        /// Gibt die Sitzungsverwaltung zurück.
        /// </summary>
        public ISessionManager SessionManager
        {
            get { return _sessionManager; }            
        }

        #endregion

        #region Komponenten-Hosting

        // Liste der Registrierten Komponenten
        private Dictionary<string, ComponentRegistration> _componentRegistry = null;

        /// <summary>
        /// Gibt die Liste der Registrierten Komponenten zurück.
        /// <remarks>Falls die Liste noch nicht existiert, wird sie automatisch erstellt.</remarks>
        /// </summary>
        internal Dictionary<string, ComponentRegistration> ComponentRegistry
        { 
            get
            {
                // Wenn die Liste der Registrierten Komponenten noch nicht erzeugt wurde ...
                if (_componentRegistry == null)
                    // Liste erzeugen
                    _componentRegistry = new Dictionary<string, ComponentRegistration>();

                // Liste zurückgeben
                return _componentRegistry;
            }
        }

        /// <summary>
        /// Hebt die Registrierung einer bestimmten Komponente auf.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        public void UnregisterComponent<I>()
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);

            // Wenn eine Komponente mit der angegebenen Schnittstelle registriert ist ...
            if (ComponentRegistry.ContainsKey(interfaceType.FullName))
            { 
                // Registrierung aufheben
                ComponentRegistry.Remove(interfaceType.FullName);
            }
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>        
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        public void RegisterComponent<I, T>()
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);
            Type implementationType = typeof(T);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Schnittstellentyp ist keine Schnittstelle!", "interfaceType");

            // Wenn der Implementierungstyp keine Klasse ist ...
            if (!implementationType.IsClass)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Implementierungstyp ist keine Klasse!", "interfaceType");

            // Name der Schnittstelle abfragen
            string interfaceName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, implementationType);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(interfaceName, registration);
            }
        }
        
        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <param name="factoryMethod">Delegat auf Fabrikmethode, die sich um die Erzeugung und Inizialisierung der Komponente kümmert</param>
        public void RegisterComponent<I>(Func<object> factoryMethod)
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);
            
            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ApplicationException("Der angegebene Schnittstellentyp ist keine Schnittstelle!");

            // Wenn kein Delegat auf eine Fabrikmethode angegeben wurde ...
            if (factoryMethod==null)
                // Ausnahme werfen
                throw new ArgumentException("Keinen Delegaten für Komponentenerzeugung angegeben.","factoryMethod");

            // Name der Schnittstelle abfragen
            string interfaceName = interfaceType.FullName;
            
            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, factoryMethod);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(interfaceName, registration);
            }
        }

        /// <summary>
        /// Registriert eine bestimmte Komponenteninstanz.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        /// <param name="instance">Instanz</param>
        public void RegisterComponent<I, T>(T instance)
        {
            // Typinformationen abrufen
            Type interfaceType = typeof(I);
            Type implementationType = typeof(T);

            // Wenn der Schnittstellentyp keine Schnittstelle ist ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Schnittstellentyp ist keine Schnittstelle!", "interfaceType");

            // Wenn der Implementierungstyp keine Klasse ist ...
            if (!implementationType.IsClass)
                // Ausnahme werfen
                throw new ArgumentException("Der angegebene Implementierungstyp ist keine Klasse!", "interfaceType");

            // Name der Schnittstelle abfragen
            string interfaceName = interfaceType.FullName;

            // Wenn für die angegebene Schnittstelle noch keine Komponente registriert wurde ...
            if (!ComponentRegistry.ContainsKey(interfaceName))
            {
                // Registrierungseintrag erstellen
                ComponentRegistration registration = new ComponentRegistration(interfaceType, instance);

                // Registrierungseintrag der Liste zufügen
                ComponentRegistry.Add(interfaceName, registration);
            }
        }

        /// <summary>
        /// Gibt eine Liste mit allen registrierten Komponenten zurück.
        /// </summary>
        /// <returns>Liste der registrierten Komponenten</returns>
        public List<string> GetRegisteredComponents()
        { 
            // Wörterbuch erzeugen 
            List<string> result = new List<string>();

            // Komponentenregistrierung druchlaufen
            foreach (ComponentRegistration registration in ComponentRegistry.Values)
            { 
                // Neuen Eintrag erstellen
                result.Add(registration.InterfaceType.FullName);
            }
            // Wörterbuch zurückgeben
            return result;
        }

        #endregion               

        #region Netzwerk-Kommunikation

        // TCP-Anschlussnummer dieses Komponentenhosts
        private int _tcpPort = 0;

        // Name dieses Komponentenhosts
        private string _name = string.Empty;

        /// <summary>
        /// Gibt den Namen des Komponentenhosts zurück.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Startet das Horchen auf Client-Anfragen.
        /// </summary>
        private void StartListening()
        {
            // TCP-Kommunikationskanal öffnen
            IChannel channel = (IChannel)_protocolSetup.CreateChannel();
                        
            // Wenn der Kanal erzeugt wurde ...
            if (channel != null)
                // Kanal registrieren
                ChannelServices.RegisterChannel(channel, false);

            // Komponentenhost für entfernte Zugriffe veröffentlichen            
            RemotingServices.Marshal(_invoker, _name);            
        }

        /// <summary>
        /// Beendet das Horchen auf Client-Anfragen.
        /// </summary>
        private void StopListening()
        {
            // Veröffentlichung des Komponentenhosts für entfernte Zugriffe löschen
            RemotingServices.Disconnect(_invoker);

            // TCP-Kommunikationskanal schließen
            CloseTcpChannel();                                        
        }

        /// <summary>
        /// Gibt den internen Namen des Kommunikationskanals dieses Komponentenhosts zurück.
        /// </summary>
        private string InternalChannelName
        {
            get { return "RainbirdEbcComponentHost" + Convert.ToString(_tcpPort); }
        }

        /// <summary>
        /// Schließt den TCP-Kanal, falls dieser geöffent ist.
        /// </summary>
        private void CloseTcpChannel()
        { 
            // Kanalnamen abrufen
            string channelName = InternalChannelName;

            // Kanal suchen
            IChannel channel = ChannelServices.GetChannel(channelName);

            // Wenn der Kanal gefunden wurde ...
            if (channel != null)             
                // Kanalregistrierung aufheben
                ChannelServices.UnregisterChannel(channel);            
        }

        #endregion

        // Gibt an, ob Dispose bereits aufgerufen wurde, oder nicht
        private bool _isDisposed = false;

        /// <summary>
        /// Verwaltete Ressourcen freigeben.
        /// </summary>
        public void Dispose()
        {
            // Wenn Dispose noch nicht aufgerufen wurde ...
            if (!_isDisposed)
            {
                // Schalter setzen
                _isDisposed = true;

                // Horchen auf Client-Anfragen beenden
                StopListening();
                
                // Wenn der Komponentenaufrufer existiert ...
                if (_invoker != null)
                    // Komponnetenaufrufer entsorgen
                    _invoker = null;
                
                // Wenn die Sitzungsverwaltung existiert ...
                if (_sessionManager != null)
                {
                    // Sitzungsverwaltung entsorgen
                    _sessionManager.Dispose();
                    _sessionManager = null;
                }
                // Wenn die Authentifizierung verdrahtet ist ...
                if (this.Authenticate != null)
                    // Verdrahtung aufheben
                    this.Authenticate = null;

                // Wenn der Authentifizierungsanbieter existiert ...
                if (_authProvider != null)
                    // Authentifizierungsanbieter entsorgen
                    _authProvider = null;

                // Wenn die Liste der Registrierten Komponenten existiert ...
                if (_componentRegistry != null)
                {
                    // Liste der registrierten Komponenten entsorgen
                    _componentRegistry.Clear();
                    _componentRegistry = null;
                }
            }
        }
    }
}
