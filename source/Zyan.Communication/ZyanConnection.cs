using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Transactions;
using Zyan.Communication.Notification;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;

namespace Zyan.Communication
{
    /// <summary>
    /// Maintains a connection to a Zyan component host.
    /// </summary>
    public class ZyanConnection : IDisposable
    {
        #region Konfiguration

        // URL zum Server-Prozess
        private string _serverUrl = string.Empty;

        // Name des entfernten Komponentenhosts
        private string _componentHostName = string.Empty;

        // Protokoll-Einstellungen
        private IClientProtocolSetup _protocolSetup = null;

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

        #endregion

        #region Konstruktoren

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="setup">Objekt mit Konfigurationseinstellungen für die Verbindung</param>
        public ZyanConnection(ZyanConnectionSetup setup)
            : this(setup.ServerUrl, setup.ProtocolSetup,setup.Credentials,setup.AutoLoginOnExpiredSession,setup.KeepSessionAlive)
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="serverUrl">Server-URL (z.B. "tcp://server1:46123/ebcserver")</param>                
        public ZyanConnection(string serverUrl)
            : this(serverUrl, new TcpBinaryClientProtocolSetup(), null, false, true)
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="serverUrl">Server-URL (z.B. "tcp://server1:46123/ebcserver")</param>                
        /// <param name="autoLoginOnExpiredSession">Gibt an, ob sich der Proxy automatisch neu anmelden soll, wenn die Sitzung abgelaufen ist</param>
        public ZyanConnection(string serverUrl, bool autoLoginOnExpiredSession)
            : this(serverUrl, new TcpBinaryClientProtocolSetup(), null, autoLoginOnExpiredSession, !autoLoginOnExpiredSession)
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="serverUrl">Server-URL (z.B. "tcp://server1:46123/ebcserver")</param>                
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>
        public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup)
            : this(serverUrl, protocolSetup, null, false, true)
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="serverUrl">Server-URL (z.B. "tcp://server1:46123/ebcserver")</param>                
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>
        /// <param name="autoLoginOnExpiredSession">Gibt an, ob sich der Proxy automatisch neu anmelden soll, wenn die Sitzung abgelaufen ist</param>
        public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup, bool autoLoginOnExpiredSession)
            : this(serverUrl, protocolSetup, null, autoLoginOnExpiredSession, true)
        { }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="serverUrl">Server-URL (z.B. "tcp://server1:46123/ebcserver")</param>        
        /// <param name="protocolSetup">Protokoll-Einstellungen</param>
        /// <param name="credentials">Anmeldeinformationen</param>
        /// <param name="autoLoginOnExpiredSession">Gibt an, ob sich der Proxy automatisch neu anmelden soll, wenn die Sitzung abgelaufen ist</param>
        /// <param name="keepSessionAlive">Gib an, ob die Sitzung automatisch verlängert werden soll</param>
        public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup, Hashtable credentials, bool autoLoginOnExpiredSession, bool keepSessionAlive)
        {
            // Wenn kein Server-URL angegeben wurde ...
            if (string.IsNullOrEmpty(serverUrl))
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_ServerUrlMissing, "serverUrl");

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

            // Einstellung für automatisches Anmelden bei abgelaufener Sitzung übernehmen
            _autoLoginOnExpiredSession = autoLoginOnExpiredSession;

            // Einstellung für automatische Sitzungsverlängung übernehmen
            _keepSessionAlive = keepSessionAlive;
            
            // Wenn automatisches Anmelden aktiv ist ...
            if (_autoLoginOnExpiredSession)
                // Anmeldedaten speichern
                _autoLoginCredentials = credentials;

            // Verwaltung für Serialisierungshandling erzeugen
            _serializationHandling = new SerializationHandlerRepository();

            // Standardmäßig keine Aufrufabfangvorrichtungen verarbeiten
            CallInterceptionEnabled = false;

            // Auflistung für Aufrufabfangvorrichtungen erzeugen
            _callInterceptors = new CallInterceptorCollection();

            // Server-URL in Bestandteile zerlegen
            string[] addressParts = _serverUrl.Split('/');

            // Name des Komponentenhots speichern
            _componentHostName = addressParts[addressParts.Length - 1];

            // TCP-Kommunikationskanal öffnen
            IChannel channel = _protocolSetup.CreateChannel();

            // Wenn der Kanal erzeugt wurde ...
            if (channel != null)
                // Kanal registrieren
                ChannelServices.RegisterChannel(channel, false);

            // Wörterbuch für Benachrichtigungs-Registrierungen erzeugen
            _subscriptions = new Dictionary<Guid, NotificationReceiver>();

            // Wenn leere Anmeldeinformationen angegben wurden ...
            if (credentials != null && credentials.Count == 0)
                // Auflistung löschen
                credentials = null;

            // Am Server anmelden
            RemoteComponentFactory.Logon(_sessionID, credentials);

            // Registrierte Komponenten vom Server abrufen
            _registeredComponents = new List<ComponentInfo>(RemoteComponentFactory.GetRegisteredComponents());

            // Sitzungslimit abrufen
            _sessionAgeLimit = RemoteComponentFactory.SessionAgeLimit;

            // Zeitgeber starten (Wenn die automatische Sitzungsverlängerung aktiv ist)
            StartKeepSessionAliveTimer();

            // Verbindung der Auflistung zufügen
            _connections.Add(this);
        }

        #endregion
        
        #region Sitzungsverwaltung

        // Sitzungsschlüssel
        private Guid _sessionID = Guid.Empty;

        // Schalter für automatisches Anmelden, bei abgelaufender Sitzung
        private bool _autoLoginOnExpiredSession = false;

        // Anmeldeinformationen für automatisches Anmelden
        private Hashtable _autoLoginCredentials = null;

        // Schalter für automatische Sitzungsverlängerung
        private bool _keepSessionAlive = true;

        // Zeitgeber
        private Timer _keepSessionAliveTimer = null;

        // Maximale Sitzungslebensdauer in Minuten
        private int _sessionAgeLimit = 0;

        /// <summary>
        /// Bereitet den Aufrufkontext für die Übertragung vor.
        /// </summary>
        internal void PrepareCallContext(bool implicitTransactionTransfer)
        {
            // Transferobjekt für Kontextdaten erzeugen, die implizit übertragen werden sollen 
            LogicalCallContextData data = new LogicalCallContextData();

            // Sitzungsschlüssel im Transferobjekt ablegen
            data.Store.Add("sessionid", _sessionID);

            // Wenn eine Umgebungstransaktion aktiv ist die implizite Transaktionsübertragung eingeschaltet ist ...
            if (implicitTransactionTransfer && Transaction.Current != null)
            {
                // Umgebungstransaktion abrufen
                Transaction transaction = Transaction.Current;

                // Wenn die Transaktion noch aktiv ist ...
                if (transaction.TransactionInformation.Status == TransactionStatus.InDoubt ||
                    transaction.TransactionInformation.Status == TransactionStatus.Active)
                {
                    // Transaktion im Transferobjekt ablegen                        
                    data.Store.Add("transaction", transaction);
                }
            }
            // Transferobjekt in den Aufrufkontext einhängen
            CallContext.SetData("__ZyanContextData_" + _componentHostName, data);
        }

        /// <summary>
        /// Startet den Zeitgeber für automatische Sitzungsverlängerung.
        /// <remarks>
        /// Wenn der Zeitgeber läuft, wird er neu gestartet.
        /// </remarks>
        /// </summary>
        private void StartKeepSessionAliveTimer()
        { 
            // Wenn der Zeitgeber für automatische Sitzungsverlängerung existiert ...
            if (_keepSessionAliveTimer != null)
                // Zeitgeber entsorgen
                _keepSessionAliveTimer.Dispose();

            // Wenn die Sitzung automatisch verlängert werden soll ...
            if (_keepSessionAlive)
            {
                // Intervall in Millisekunden berechnen
                int interval = (_sessionAgeLimit / 2) * 60000;
                // Zeitgeber erzeugen
                _keepSessionAliveTimer = new Timer(new TimerCallback(KeepSessionAlive), null, interval, interval);
            }
        }

        /// <summary>
        /// Wird vom Zeitgeber aufgerufen, wenn die Sitzung verlängert werden soll. 
        /// </summary>
        /// <param name="state">Statusobjekt</param>
        private void KeepSessionAlive(object state)
        {
            try
            {
                // Aufrufkontext vorbereiten
                PrepareCallContext(false);

                // Sitzung erneuern
                int serverSessionAgeLimit = RemoteComponentFactory.RenewSession();

                // Wenn der Wert für die Sitzungslebensdauer auf dem Server in der Zwischenzeit geändert wurde ...
                if (_sessionAgeLimit != serverSessionAgeLimit)
                {
                    // Neuen Wert für Sitzungslebendauer speichern
                    _sessionAgeLimit = serverSessionAgeLimit;

                    // Zeitgeber neu starten
                    StartKeepSessionAliveTimer();
                }
            }
            catch
            { }
        }

        #endregion

        #region Komponentenzugriff

        // Liste mit allen registrierten Komponenten des verbundenen Servers
        private List<ComponentInfo> _registeredComponents = null;

        /// <summary>
        /// Erzeugt im Server-Prozess eine neue Instanz einer bestimmten Komponente und gibt einen Proxy dafür zurück.
        /// </summary>
        /// <typeparam name="T">Typ der öffentlichen Schnittstelle der zu konsumierenden Komponente</typeparam>        
        /// <returns>Proxy</returns>
        public T CreateProxy<T>()
        {
            // Andere Überladung aufrufen
            return CreateProxy<T>(string.Empty, false);
        }

        /// <summary>
        /// Erzeugt im Server-Prozess eine neue Instanz einer bestimmten Komponente und gibt einen Proxy dafür zurück.
        /// </summary>
        /// <typeparam name="T">Typ der öffentlichen Schnittstelle der zu konsumierenden Komponente</typeparam>        
        /// <param name="uniqueName">Eindeutiger Name der Komponente</param>
        /// <returns>Proxy</returns>
        public T CreateProxy<T>(string uniqueName)
        {
            // Andere Überladung aufrufen
            return CreateProxy<T>(uniqueName, false);
        }

        /// <summary>
        /// Erzeugt im Server-Prozess eine neue Instanz einer bestimmten Komponente und gibt einen Proxy dafür zurück.
        /// </summary>
        /// <typeparam name="T">Typ der öffentlichen Schnittstelle der zu konsumierenden Komponente</typeparam>
        /// <param name="implicitTransactionTransfer">Implizite Transaktionsübertragung</param>
        /// <returns>Proxy</returns>
        public T CreateProxy<T>(bool implicitTransactionTransfer)
        {
            return CreateProxy<T>(string.Empty, implicitTransactionTransfer);
        }

        /// <summary>
        /// Erzeugt im Server-Prozess eine neue Instanz einer bestimmten Komponente und gibt einen Proxy dafür zurück.
        /// </summary>
        /// <typeparam name="T">Typ der öffentlichen Schnittstelle der zu konsumierenden Komponente</typeparam>
        /// <param name="uniqueName">Eindeutiger Name der Komponente</param>
        /// <param name="implicitTransactionTransfer">Implizite Transaktionsübertragung</param>
        /// <returns>Proxy</returns>
        public T CreateProxy<T>(string uniqueName, bool implicitTransactionTransfer)
        {
            // Typeninformationen lesen
            Type interfaceType = typeof(T);

            // Wenn kein eindeutiger Name angegeben ist ...
            if (string.IsNullOrEmpty(uniqueName))
                // Name der Schnittstelle verwenden
                uniqueName = interfaceType.FullName;

            // Wenn keine Schnittstelle angegeben wurde ...
            if (!interfaceType.IsInterface)
                // Ausnahme werfen
                throw new ApplicationException(string.Format("Der angegebene Typ '{0}' ist keine Schnittstelle! Für die Erzeugung einer entfernten Komponenteninstanz, wird deren öffentliche Schnittstelle benötigt!", interfaceType.FullName));
            
            // Komponenteninformation abrufen
            ComponentInfo info = (from entry in _registeredComponents
                                  where entry.UniqueName.Equals(uniqueName)
                                  select entry).FirstOrDefault();

            // Wenn für die Schnittstelle auf dem verbundenen Server keine Komponente registriert ist ...
            if (info==null)
                // Ausnahme werfne
                throw new ApplicationException(string.Format("Für Schnittstelle '{0}' ist auf dem Server '{1}' keine Komponente registriert.", interfaceType.FullName, _serverUrl));

            // Proxy erzeugen
            ZyanProxy proxy = new ZyanProxy(info.UniqueName, typeof(T), this, implicitTransactionTransfer, _sessionID, _componentHostName, _autoLoginOnExpiredSession, _autoLoginCredentials, info.ActivationType);

            // Proxy transparent machen und zurückgeben
            return (T)proxy.GetTransparentProxy();
        }

        // Proxy für den Zugriff auf die entfernte Komponentenfabrik des Komponentenhosts
        private IZyanDispatcher _remoteComponentFactory = null;

        /// <summary>
        /// Gibt einen Proxy für den Zugriff auf die entfernte Komponentenfabrik des Komponentenhosts zurück.
        /// </summary>
        protected internal IZyanDispatcher RemoteComponentFactory
        {
            get
            {
                // Wenn noch keine Verbindung zur entfernten Komponentenfabrik existiert ...
                if (_remoteComponentFactory == null)
                {
                    // Verbindung zur entfernten Komponentenfabrik herstellen
                    _remoteComponentFactory = (IZyanDispatcher)Activator.GetObject(typeof(IZyanDispatcher), _serverUrl);
                }
                // Fabrik-Proxy zurückgeben
                return _remoteComponentFactory;
            }
        }

        #endregion

        #region Benutzerdefinierte Serialisierung

        // Serialisierungshandling.
        private SerializationHandlerRepository _serializationHandling = null;

        /// <summary>
        /// Gibt die Verwaltung für benutzerdefinierte Serialisierungsbehandlung zurück.
        /// </summary>
        public SerializationHandlerRepository SerializationHandling
        {
            get { return _serializationHandling; }
        }

        #endregion

        #region Aufrufverfolgung

        /// <summary>
        /// Ereignis: Bevor ein Komponentenaufruf durchgeführt wird.
        /// </summary>
        public event EventHandler<BeforeInvokeEventArgs> BeforeInvoke;

        /// <summary>
        /// Ereignis: Nachdem ein Komponentenaufruf durchgeführt wurde.
        /// </summary>
        public event EventHandler<AfterInvokeEventArgs> AfterInvoke;

        /// <summary>
        /// Ereignis: Wenn ein Komponentenaufruf abgebrochen wurde.
        /// </summary>
        public event EventHandler<InvokeCanceledEventArgs> InvokeCanceled;

        /// <summary>
        /// Feuert das BeforeInvoke-Ereignis.
        /// </summary>
        /// <param name="e">Ereignisargumente</param>
        protected internal virtual void OnBeforeInvoke(BeforeInvokeEventArgs e)
        {
            // Wenn für BeforeInvoke Ereignisprozeduren registriert sind ...
            if (BeforeInvoke != null)
                // Ereignis feuern
                BeforeInvoke(this, e);
        }

        /// <summary>
        /// Feuert das AfterInvoke-Ereignis.
        /// </summary>
        /// <param name="e">Ereignisargumente</param>
        protected internal virtual void OnAfterInvoke(AfterInvokeEventArgs e)
        {
            // Wenn für AfterInvoke Ereignisprozeduren registriert sind ...
            if (AfterInvoke != null)
                // Ereignis feuern
                AfterInvoke(this, e);
        }

        /// <summary>
        /// Feuert das InvokeCanceled-Ereignis.
        /// </summary>
        /// <param name="e">Ereignisargumente</param>
        protected internal virtual void OnInvokeCanceled(InvokeCanceledEventArgs e)
        {
            // Wenn für AfterInvoke Ereignisprozeduren registriert sind ...
            if (InvokeCanceled != null)
                // Ereignis feuern
                InvokeCanceled(this, e);
        }

        #endregion

        #region Benachrichtigungen

        // Wörterbuch für Benachrichtigungs-Registrierungen
        private volatile Dictionary<Guid, NotificationReceiver> _subscriptions = null;

        // Sperrobjekt für Threadsynchronisierung
        private object _subscriptionsLockObject = new object();

        /// <summary>
        /// Registriert einen Client für den Empfang von Benachrichtigungen bei einem bestimmten Ereignis.
        /// </summary>
        /// <param name="eventName">Ereignisname</param>
        /// <param name="handler">Delegat auf Client-Ereignisprozedur</param>
        public Guid SubscribeEvent(string eventName, EventHandler<NotificationEventArgs> handler)
        {
            // Empfangsvorrichtung für Benachrichtigung erzeugen
            NotificationReceiver receiver = new NotificationReceiver(eventName, handler);

            // Für Benachrichtigung beim entfernten Komponentenhost registrieren
            RemoteComponentFactory.Subscribe(eventName, receiver.FireNotifyEvent);

            // Registrerungsschlüssel erzeugen
            Guid subscriptionID = Guid.NewGuid();

            lock (_subscriptionsLockObject)
            {
                // Empfangsvorrichtung in Wörterbuch speichern
                _subscriptions.Add(subscriptionID, receiver);
            }
            // Registrerungsschlüssel zurückgeben
            return subscriptionID;
        }

        /// <summary>
        /// Hebt eine Registrierung für den Empfang von Benachrichtigungen eines bestimmten Ereignisses auf.
        /// </summary>
        /// <param name="subscriptionID">Registrerungsschlüssel</param>
        public void UnsubscribeEvent(Guid subscriptionID)
        {
            lock (_subscriptionsLockObject)
            {
                // Wenn die angegebene Registrerungsschlüssel bekannt ist ...
                if (_subscriptions.ContainsKey(subscriptionID))
                {
                    // Empfangsvorrichtung abrufen
                    NotificationReceiver receiver = _subscriptions[subscriptionID];

                    // Für Benachrichtigung beim entfernten Komponentenhost registrieren
                    RemoteComponentFactory.Unsubscribe(receiver.EventName, receiver.FireNotifyEvent);

                    // Empfängervorrichtung aus Wörterbuch löschen
                    _subscriptions.Remove(subscriptionID);

                    // Empfängervorrichtung entsorgen
                    receiver.Dispose();
                }
            }
        }

        #endregion

        #region Aufrufe abfangen

        /// <summary>
        /// Gibt zurück, ob registrierte Aufrufabfangvorrichtungen verarbeitet werden, oder legt diest fest.
        /// </summary>
        public bool CallInterceptionEnabled
        {
            get;
            set;
        }

        // Auflistung für Aufrufabfangvorrichtungen
        private CallInterceptorCollection _callInterceptors = null;

        /// <summary>
        /// Gibt eine Auflistung der registrierten Aufrufabfangvorrichtungen zurück.
        /// </summary>
        public CallInterceptorCollection CallInterceptors
        {
            get { return _callInterceptors; }
        }

        #endregion

        #region Fehlerbehandlung

        /// <summary>
        /// Occures when a error is detected.
        /// </summary>
        public event EventHandler<ZyanErrorEventArgs> Error;

        /// <summary>
        /// Fires the Error event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected internal void OnError(ZyanErrorEventArgs e)
        {
            if (Error != null)
                Error(this, e);
        }

        /// <summary>
        /// Gets if errors should be handled or not.
        /// </summary>
        internal bool ErrorHandlingEnabled
        {
            get { return Error != null; }
        }

        #endregion

        #region Dispose-Implementierung

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

                // Wenn der Zeitgeber noch existiert ...
                if (_keepSessionAliveTimer != null)
                {
                    // Zeitgeber entsorgen
                    _keepSessionAliveTimer.Dispose();
                    _keepSessionAliveTimer = null;
                }                
                try
                {
                    // Vom Server abmelden
                    RemoteComponentFactory.Logoff(_sessionID);
                }
                catch (RemotingException)
                { }
                catch (SocketException)
                { }
                catch (WebException)
                { }
                finally
                {
                    // Verbindung aus der Auflistung entfernen
                    _connections.Remove(this);
                }
                // Variablen freigeben                
                _remoteComponentFactory = null;
                _serverUrl = string.Empty;
                _sessionID = Guid.Empty;
                _protocolSetup = null;
                _serializationHandling=null;
                _componentHostName = string.Empty;
                
                if (_registeredComponents != null)
                {
                    _registeredComponents.Clear();
                    _registeredComponents = null;
                }
                if (_callInterceptors != null)
                {
                    _callInterceptors.Clear();
                    _callInterceptors = null;
                }
                if (_autoLoginCredentials != null)
                {
                    _autoLoginCredentials.Clear();
                    _autoLoginCredentials = null;
                }
                GC.WaitForPendingFinalizers();
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

        #endregion

        #region Statischer Zugriff auf alle Verbindungen

        // Auflistung der bekannten Verbindungen
        private static List<ZyanConnection> _connections = new List<ZyanConnection>();

        /// <summary>
        /// Gibt eine Auflistung aller bekanten Verbindungen Hosts zurück.
        /// </summary>
        public static List<ZyanConnection> Connections
        {
            get { return _connections.ToList<ZyanConnection>(); }
        }

        #endregion
    }
}
