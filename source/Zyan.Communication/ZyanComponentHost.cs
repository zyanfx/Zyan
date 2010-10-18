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
using Zyan.Communication.Modularity;

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

        // Auflistung der bekannten Komponenten Hosts
        private static List<ZyanComponentHost> _hosts = new List<ZyanComponentHost>();

        /// <summary>
        /// Gibt eine Auflistung aller bekanten Komponenten Hosts zurück.
        /// </summary>
        public static List<ZyanComponentHost> Hosts
        {
            get { return _hosts.ToList<ZyanComponentHost>(); }
        }

        #region Konstruktor
      
        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="name">Name des Komponentenhosts</param>        
        /// <param name="tcpPort">TCP-Anschlussnummer</param>                
        public ZyanComponentHost(string name, int tcpPort)
            : this(name, new TcpBinaryServerProtocolSetup(tcpPort), new InProcSessionManager(), new ComponentCatalog())
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="name">Name des Komponentenhosts</param>        
        /// <param name="tcpPort">TCP-Anschlussnummer</param>                
        /// <param name="catalog">Komponenten-Katalog</param>
        public ZyanComponentHost(string name, int tcpPort, ComponentCatalog catalog)
            : this(name, new TcpBinaryServerProtocolSetup(tcpPort), new InProcSessionManager(), catalog)
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="name">Name des Komponentenhosts</param>        
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>        
        public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup)
            : this(name, protocolSetup, new InProcSessionManager(), new ComponentCatalog())
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="name">Name des Komponentenhosts</param>        
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>        
        /// <param name="catalog">Komponenten-Katalog</param>
        public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup, ComponentCatalog catalog)
            : this(name, protocolSetup, new InProcSessionManager(), catalog)
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="name">Name des Komponentenhosts</param>        
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>
        /// <param name="sessionManager">Sitzungsverwaltung</param>
        public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup, ISessionManager sessionManager) : this(name,protocolSetup,sessionManager,new ComponentCatalog())
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="name">Name des Komponentenhosts</param>        
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>        
        /// <param name="sessionManager">Sitzungsverwaltung</param>
        /// <param name="catalog">Komponenten-Katalog</param>
        public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup, ISessionManager sessionManager, ComponentCatalog catalog)
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

            // Wenn kein Komponenten-Katalog angegeben wurde ...
            if (catalog == null)
                // Ausnahme werfen
                throw new ArgumentNullException("catalog");

            // Werte übernehmen
            _name = name;
            _protocolSetup = protocolSetup;
            _sessionManager = sessionManager;
            _catalog = catalog;
            
            // Komponentenaufrufer erzeugen
            _invoker = new ComponentInvoker(this);

            // Authentifizierungsanbieter übernehmen und verdrahten
            _authProvider = protocolSetup.AuthenticationProvider;
            this.Authenticate = _authProvider.Authenticate;

            // Komponenten Host der Host-Auflistung zufügen
            _hosts.Add(this);

            // Beginnen auf Client-Anfragen zu horchen
            StartListening();
        }

        /// <summary>
        /// Destruktor.
        /// </summary>
        ~ZyanComponentHost()
        {
            // Ressourcen freigeben
            Dispose();
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

        // Komponenten-Katalog
        private ComponentCatalog _catalog = null;

        /// <summary>
        /// Gibt den zu veröffentlichenden Komponenten-Katalog zurück, oder legt ihn fest.
        /// </summary>
        public ComponentCatalog ComponentCatalog
        {
            get { return _catalog; }
            set
            { 
                // Wenn kein Wert angegeben wurde ...
                if (value == null)
                    // Ausnahme werfen
                    throw new ArgumentNullException();

                // Komponenten-Katalog festlegen
                _catalog = value;
            }
        }

        /// <summary>
        /// Gibt die Liste der Registrierten Komponenten zurück.
        /// <remarks>Falls die Liste noch nicht existiert, wird sie automatisch erstellt.</remarks>
        /// </summary>
        internal Dictionary<string, ComponentRegistration> ComponentRegistry
        { 
            get
            {        
                // Liste zurückgeben
                return _catalog.ComponentRegistry;
            }
        }

        /// <summary>
        /// Gibt eine Instanz einer registrierten Komponente zurück.
        /// </summary>
        /// <param name="registration">Komponentenregistrierung</param>
        /// <returns>Komponenten-Instanz</returns>
        internal object GetComponentInstance(ComponentRegistration registration)
        {
            // Aufruf an Komponentenkatalog weiterleiten
            return _catalog.GetComponentInstance(registration);
        }

        /// <summary>
        /// Hebt die Registrierung einer bestimmten Komponente auf.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        public void UnregisterComponent<I>()
        {
            // Aufruf an Komponentenkatalog weiterleiten
            _catalog.UnregisterComponent<I>();            
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>        
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        public void RegisterComponent<I, T>()
        {
            // Aufruf an Komponentenkatalog weiterleiten
            _catalog.RegisterComponent<I, T>();            
        }
        
        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <param name="factoryMethod">Delegat auf Fabrikmethode, die sich um die Erzeugung und Inizialisierung der Komponente kümmert</param>
        public void RegisterComponent<I>(Func<object> factoryMethod)
        {
            // Aufruf an Komponentenkatalog weiterleiten
            _catalog.RegisterComponent<I>(factoryMethod);            
        }

        /// <summary>
        /// Registriert eine bestimmte Komponenteninstanz.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        /// <param name="instance">Instanz</param>
        public void RegisterComponent<I, T>(T instance)
        {
            // Aufruf an Komponentenkatalog weiterleiten
            _catalog.RegisterComponent<I, T>(instance);            
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>        
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        /// <param name="moduleName">Modulname</param>
        public void RegisterComponent<I, T>(string moduleName)
        {
            // Aufruf an Komponentenkatalog weiterleiten
            _catalog.RegisterComponent<I, T>(moduleName);            
        }

        /// <summary>
        /// Registriert eine bestimmte Komponente.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <param name="moduleName">Modulname</param>
        /// <param name="factoryMethod">Delegat auf Fabrikmethode, die sich um die Erzeugung und Inizialisierung der Komponente kümmert</param>
        public void RegisterComponent<I>(string moduleName, Func<object> factoryMethod)
        {
            // Aufruf an Komponentenkatalog weiterleiten
            _catalog.RegisterComponent<I>(moduleName, factoryMethod);            
        }

        /// <summary>
        /// Registriert eine bestimmte Komponenteninstanz.
        /// </summary>
        /// <typeparam name="I">Schnittstellentyp der Komponente</typeparam>
        /// <typeparam name="T">Implementierungstyp der Komponente</typeparam>
        /// <param name="moduleName">Modulname</param>
        /// <param name="instance">Instanz</param>        
        public void RegisterComponent<I, T>(string moduleName, T instance)
        {
            // Aufruf an Komponentenkatalog weiterleiten
            _catalog.RegisterComponent<I, T>(moduleName,instance);            
        }

        /// <summary>
        /// Gibt eine Liste mit allen registrierten Komponenten zurück.
        /// </summary>
        /// <returns>Liste der registrierten Komponenten</returns>
        public List<string> GetRegisteredComponents()
        { 
            // Aufruf an Komponentenkatalog weiterleiten
            return _catalog.GetRegisteredComponents();
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

                // Host aus der Auflistung entfernen
                _hosts.Remove(this);

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

                // Wenn der Komponentenkatalog existiert ...
                if (_catalog != null)
                {
                    // Komponentenkatalog entsorgen                    
                    _catalog = null;
                }
            }
        }
    }
}
