using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.SessionMgmt;
using Zyan.Communication.Notification;
using System.Linq.Expressions;
using Zyan.InterLinq.Expressions;

namespace Zyan.Communication
{
	/// <summary>
	/// Host for publishing components with Zyan.
	/// </summary>
	public class ZyanComponentHost : IComponentCatalog, IDisposable
	{
		#region Constructors

		/// <summary>
		/// Konstruktor.
		/// </summary>
		/// <param name="name">Name des Komponentenhosts</param>
		/// <param name="tcpPort">TCP-Anschlussnummer</param>
		public ZyanComponentHost(string name, int tcpPort)
			: this(name, new TcpBinaryServerProtocolSetup(tcpPort), new InProcSessionManager(), new ComponentCatalog(true))
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
			: this(name, protocolSetup, new InProcSessionManager(), new ComponentCatalog(true))
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
		public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup, ISessionManager sessionManager)
			: this(name, protocolSetup, sessionManager, new ComponentCatalog(true))
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
				throw new ArgumentException(LanguageResource.ArgumentException_ComponentHostNameMissing,"name");

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
			_sessionManager.ClientSessionTerminated += (s, e) => OnClientSessionTerminated(e);
			_catalog = catalog;
			
			// Verwaltung für Serialisierungshandling erzeugen
			_serializationHandling = new SerializationHandlerRepository();
			
			// Komponentenaufrufer erzeugen
			_dispatcher = new ZyanDispatcher(this);

			// Authentifizierungsanbieter übernehmen und verdrahten
			_authProvider = protocolSetup.AuthenticationProvider;
			this.Authenticate = _authProvider.Authenticate;

			// Komponenten Host der Host-Auflistung zufügen
			_hosts.Add(this);

			// Register standard serialization handlers
			RegisterStandardSerializationHandlers();

			// Beginnen auf Client-Anfragen zu horchen
			StartListening();
		}

		#endregion
		
		#region Authentication

		private IAuthenticationProvider _authProvider = null;

		/// <summary>
		/// Request for authentication.
		/// </summary>
		public Func<AuthRequestMessage, AuthResponseMessage> Authenticate;

		#endregion

		#region Session Management

		private ISessionManager _sessionManager = null;
		
		/// <summary>
		/// Returns the session manager used by this host.
		/// </summary>
		public ISessionManager SessionManager
		{
			get { return _sessionManager; }
		}

		#endregion

		#region Component Hosting

		private ComponentCatalog _catalog = null;
		private ZyanDispatcher _dispatcher = null;
		private static List<ZyanComponentHost> _hosts = new List<ZyanComponentHost>();

		/// <summary>
		/// Gets a list of all known hosts.
		/// </summary>
		public static List<ZyanComponentHost> Hosts
		{
			get { return _hosts.ToList<ZyanComponentHost>(); }
		}

		/// <summary>
		/// Get or sets the component catalog for this host instance.
		/// </summary>
		public ComponentCatalog ComponentCatalog
		{
			get { return _catalog; }
			set
			{
				if (value == null)
					throw new ArgumentNullException();

				_catalog = value;
			}
		}

		/// <summary>
		/// Returns a name-value-list of all component registrations.
		/// <remarks>
		/// If the list doesn´t exist yet, it will be created automaticly.
		/// </remarks>
		/// </summary>
		internal Dictionary<string, ComponentRegistration> ComponentRegistry
		{
			get
			{
				return _catalog.ComponentRegistry;
			}
		}

		/// <summary>
		/// Returns an instance of a specified registered component.
		/// </summary>
		/// <param name="registration">Component registration</param>
		/// <returns>Component instance</returns>
		public object GetComponentInstance(ComponentRegistration registration)
		{
			return _catalog.GetComponentInstance(registration);
		}

		/// <summary>
		/// Gets registration data for a specified component by its interface name.
		/// </summary>
		/// <param name="interfaceName">Name of the component´s interface</param>
		/// <returns>Component registration</returns>
		ComponentRegistration IComponentCatalog.GetRegistration(string interfaceName)
		{
			return _catalog.GetRegistration(interfaceName);
		}

		/// <summary>
		/// Deletes a component registration.
		/// </summary>
		/// <param name="uniqueName">Unique component name</param>
		public void UnregisterComponent(string uniqueName)
		{
			_catalog.UnregisterComponent(uniqueName);
		}

		/// <summary>
		/// Returns a list with information about all registered components.
		/// </summary>
		/// <returns>List with component information</returns>
		public List<ComponentInfo> GetRegisteredComponents()
		{
			return _catalog.GetRegisteredComponents();
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public void RegisterComponent<I, T>(string uniqueName, ActivationType activationType, Action<object> cleanUpHandler)
		{
			_catalog.RegisterComponent<I, T>(uniqueName, activationType, cleanUpHandler);
		}

		/// <summary>
		/// Registers a component in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="factoryMethod">Delegate of factory method for external instance creation</param>
		/// <param name="activationType">Activation type (SingleCall/Singleton)</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public void RegisterComponent<I>(string uniqueName, Func<object> factoryMethod, ActivationType activationType, Action<object> cleanUpHandler)
		{
			_catalog.RegisterComponent<I>(uniqueName, factoryMethod, activationType, cleanUpHandler);
		}

		/// <summary>
		/// Registeres a component instance in the component catalog.
		/// </summary>
		/// <typeparam name="I">Interface type of the component</typeparam>
		/// <typeparam name="T">Implementation type of the component</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="instance">Component instance</param>
		/// <param name="cleanUpHandler">Delegate for external clean up method</param>
		public void RegisterComponent<I, T>(string uniqueName, T instance, Action<object> cleanUpHandler)
		{
			_catalog.RegisterComponent<I, T>(uniqueName, instance, cleanUpHandler);
		}

		#endregion

		#region Network Communication

		// Protocol and communication settings
		private IServerProtocolSetup _protocolSetup = null;

		// Name of the component host (will be included in server URL)
		private string _name = string.Empty;

		// Name of the transport channel
		private string _channelName = string.Empty;

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
			IChannel channel = _protocolSetup.CreateChannel();

			// Wenn der Kanal erzeugt wurde ...
			if (channel != null)
			{
				// Kanalnamen merken
				_channelName = channel.ChannelName;

				// Kanal registrieren
				ChannelServices.RegisterChannel(channel, false);

				// Komponentenhost für entfernte Zugriffe veröffentlichen
				RemotingServices.Marshal(_dispatcher, _name);
			}
			else
				throw new ApplicationException(LanguageResource.ApplicationException_NoChannel);
		}

		/// <summary>
		/// Beendet das Horchen auf Client-Anfragen.
		/// </summary>
		private void StopListening()
		{
			// Veröffentlichung des Komponentenhosts für entfernte Zugriffe löschen
			RemotingServices.Disconnect(_dispatcher);

			// Kommunikationskanal schließen
			CloseChannel();
		}

		/// <summary>
		/// Schließt den Kanal, falls dieser geöffent ist.
		/// </summary>
		private void CloseChannel()
		{
			// Kanal suchen
			IChannel channel = ChannelServices.GetChannel(_channelName);

			// Wenn der Kanal gefunden wurde ...
			if (channel != null)
				// Kanalregistrierung aufheben
				ChannelServices.UnregisterChannel(channel);
		}

		#endregion

		#region Policy Injection

		/// <summary>
		/// Occurs before the component call is initiated.
		/// </summary>
		public event EventHandler<BeforeInvokeEventArgs> BeforeInvoke;

		/// <summary>
		/// Occurs after the component call is completed.
		/// </summary>
		public event EventHandler<AfterInvokeEventArgs> AfterInvoke;

		/// <summary>
		/// Occurs when the component call is canceled due to exception.
		/// </summary>
		public event EventHandler<InvokeCanceledEventArgs> InvokeCanceled;

		/// <summary>
		/// Occurs when the component call is rejected due to security reasons.
		/// </summary>
		public event EventHandler<InvokeCanceledEventArgs> InvokeRejected;

		/// <summary>
		/// Checks whether the BeforeInvoke event has subscriptions.
		/// </summary>
		/// <returns>True, if subsciptions exist, otherwise, false.</returns>
		protected internal bool HasBeforeInvokeSubscriptions()
		{
			return (BeforeInvoke != null);
		}

		/// <summary>
		/// Checks whether the AfterInvoke event has subscriptions.
		/// </summary>
		/// <returns>True, if subsciptions exist, otherwise, false.</returns>
		protected internal bool HasAfterInvokeSubscriptions()
		{
			return (AfterInvoke != null);
		}

		/// <summary>
		/// Checks whether the InvokeCanceled event has subscriptions.
		/// </summary>
		/// <returns>True, if subsciptions exist, otherwise, false.</returns>
		protected internal bool HasInvokeCanceledSubscriptions()
		{
			return (InvokeCanceled != null);
		}

		/// <summary>
		/// Checks whether the InvokeRejected event has subscriptions.
		/// </summary>
		/// <returns>True, if subsciptions exist, otherwise, false.</returns>
		protected internal bool HasInvokeRejectedSubscriptions()
		{
			return (InvokeRejected != null);
		}

		/// <summary>
		/// Fires the BeforeInvoke event.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		protected internal virtual void OnBeforeInvoke(BeforeInvokeEventArgs e)
		{
			if (BeforeInvoke != null)
				BeforeInvoke(this, e);
		}

		/// <summary>
		/// Fires the AfterInvoke event.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		protected internal virtual void OnAfterInvoke(AfterInvokeEventArgs e)
		{
			if (AfterInvoke != null)
				AfterInvoke(this, e);
		}

		/// <summary>
		/// Fires the InvokeCanceled event.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		protected internal virtual void OnInvokeCanceled(InvokeCanceledEventArgs e)
		{
			if (InvokeCanceled != null)
				InvokeCanceled(this, e);
		}

		/// <summary>
		/// Fires the InvokeRejected event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected internal virtual void OnInvokeRejected(InvokeCanceledEventArgs e)
		{
			if (InvokeRejected != null)
				InvokeRejected(this, e);
		}

		#endregion

		#region Notifications

		// Benachrichtigungsdienst
		private volatile NotificationService _notificationService = null;

		// Sperrobjekt für Instanzerstellung des Benachrichtigungsdienstes
		private object _notificationServiceLockObject = new object();

		/// <summary>
		/// Gibt zurück, ob der Benachrichtigungsdienst läuft, oder nicht.
		/// </summary>
		public bool IsNotificationServiceRunning
		{
			get
			{
				lock (_notificationServiceLockObject)
				{
					return _notificationService != null;
				}
			}
		}

		/// <summary>
		/// Startet den Benachrichtigungsdienst.
		/// </summary>
		[Obsolete("The NotificationService feature may not be supported in future Zyan versions. Please use remote delegates to create your notification system.", false)]
		public void StartNotificationService()
		{
			lock (_notificationServiceLockObject)
			{
				// Wenn der Dienst nicht bereits läuft ...
				if (_notificationService == null)
				{
					// Instanz erzeugen
					_notificationService = new NotificationService();
				}
			}
		}
		
		/// <summary>
		/// Beendet den Benachrichtigungsdienst.
		/// </summary>
		public void StopNotificationService()
		{
			lock (_notificationServiceLockObject)
			{
				// Wenn der Dienst läuft ...
				if (_notificationService != null)
				{
					// Instanz löschen
					_notificationService = null;
				}
			}
		}

		/// <summary>
		/// Gibt den Benachrichtigungsdienst zurück.
		/// </summary>		
		public NotificationService NotificationService
		{
			get
			{
				lock (_notificationServiceLockObject)
				{
					return _notificationService;
				}
			}
		}

		/// <summary>
		/// Veröffentlicht ein Ereignis einer Serverkomponente.
		/// </summary>
		/// <param name="eventName">Ereignisname</param>
		/// <returns>Delegat für Benachrichtigungsversand an registrierte Clients</returns>
		[Obsolete("The NotificationService feature may not be supported in future Zyan versions. Please use remote delegates to create your notification system.", false)]
		public EventHandler<NotificationEventArgs> PublishEvent(string eventName)
		{
			// Wenn kein Benachrichtigungsdienst läuft ...
			if (!IsNotificationServiceRunning)
				// Ausnahme werfen
				throw new ApplicationException(LanguageResource.ApplicationException_NotificationServiceNotRunning);

			// Sendevorrichtung erstellen
			NotificationSender sender = new NotificationSender(NotificationService, eventName);

			// Delegat auf Methode zum Benachrichtigungsversand erzeugen
			EventHandler<NotificationEventArgs> sendHandler = new EventHandler<NotificationEventArgs>(sender.HandleServerEvent);

			// Delegat zurückgeben
			return sendHandler;
		}

		#endregion

		#region User defined Serialization Handling

		// Serialisierungshandling.
		private SerializationHandlerRepository _serializationHandling = null;

		/// <summary>
		/// Gibt die Verwaltung für benutzerdefinierte Serialisierungsbehandlung zurück.
		/// </summary>
		public SerializationHandlerRepository SerializationHandling
		{
			get { return _serializationHandling; }
		}

		private void RegisterStandardSerializationHandlers()
		{
			// TODO: use MEF to discover and register standard serialization handlers:
			// [Export(ISerializationHandler), ExportMetadata("SerializedType", typeof(Expression))]
			SerializationHandling.RegisterSerializationHandler(typeof(Expression), new ExpressionSerializationHandler());
		}

		#endregion

		#region IDisposable implementation

		private bool _isDisposed = false;

		/// <summary>
		/// Releases all managed resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(false);
		}

		/// <summary>
		/// Releases all managed resources.
		/// </summary>
		/// <param name="calledFromFinalizer">Specifies if this method is called from finalizer or not</param>
		private void Dispose(bool calledFromFinalizer)
		{
			if (!_isDisposed)
			{
				_isDisposed = true;

				_hosts.Remove(this);

				StopListening();

				if (_dispatcher != null)
					_dispatcher = null;

				if (_sessionManager != null)
				{
					_sessionManager.Dispose();
					_sessionManager = null;
				}
				if (this.Authenticate != null)
					this.Authenticate = null;

				if (_authProvider != null)
					_authProvider = null;

				if (_catalog != null)
				{
					if (_catalog.DisposeWithHost)
						_catalog.Dispose();

					_catalog = null;
				}

				if (!calledFromFinalizer)
					GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Is called from runtime when this object is finalized.
		/// </summary>
		~ZyanComponentHost()
		{
			Dispose(true);
		}

		#endregion

		#region Login Events

		/// <summary>
		/// Occurs when new client is logged on.
		/// </summary>
		public event EventHandler<LoginEventArgs> ClientLoggedOn;

		/// <summary>
		/// Occurs when the client is logged off.
		/// </summary>
		public event EventHandler<LoginEventArgs> ClientLoggedOff;

		/// <summary>
		/// Occurs when the client session is terminated abnormally.
		/// </summary>
		public event EventHandler<LoginEventArgs> ClientSessionTerminated;

		/// <summary>
		/// Fires "ClientLoggedOn" event.
		/// </summary>
		/// <param name="e">Arguments</param>
		protected internal void OnClientLoggedOn(LoginEventArgs e)
		{
			if (ClientLoggedOn!=null)
				ClientLoggedOn(this,e);
		}

		/// <summary>
		/// Fires "ClientLoggedOff" event.
		/// </summary>
		/// <param name="e">Arguments</param>
		protected internal void OnClientLoggedOff(LoginEventArgs e)
		{
			if (ClientLoggedOff != null)
				ClientLoggedOff(this, e);
		}

		/// <summary>
		/// Fires "ClientSessionTerminated" event.
		/// </summary>
		/// <param name="e">Arguments</param>
		protected internal void OnClientSessionTerminated(LoginEventArgs e)
		{
			if (ClientSessionTerminated != null)
				ClientSessionTerminated(this, e);
		}

		#endregion

		#region Trace Polling Events

		// Switch to enable polling event trace
		private bool _pollingEventTacingEnabled = false;

		/// <summary>
		/// Event: Occours when a heartbeat signal is received from a client.
		/// </summary>
		public event EventHandler<ClientHeartbeatEventArgs> ClientHeartbeatReceived;

		/// <summary>
		/// Fires the ClientHeartbeatReceived event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected virtual void OnClientHeartbeatReceived(ClientHeartbeatEventArgs e)
		{
			if (SessionManager.ExistSession(e.SessionID))
			{
				var session = SessionManager.GetSessionBySessionID(e.SessionID);
				session.Timestamp = DateTime.Now;

				if (ClientHeartbeatReceived != null)
					ClientHeartbeatReceived(this, e);
			}
		}

		/// <summary>
		/// Gets or sets whether polling event tracing is enabled.
		/// </summary>
		public virtual bool PollingEventTracingEnabled
		{
			get { return _pollingEventTacingEnabled; }
			set
			{
				if (_pollingEventTacingEnabled != value)
				{
					_pollingEventTacingEnabled = value;

					if (_pollingEventTacingEnabled)
						_dispatcher.ClientHeartbeatReceived += _dispatcher_ClientHeartbeatReceived;
					else
						_dispatcher.ClientHeartbeatReceived -= _dispatcher_ClientHeartbeatReceived;
				}
			}
		}

		/// <summary>
		/// Called, when dispatcher receives a heartbeat signal from a client.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">Event arguments</param>
		private void _dispatcher_ClientHeartbeatReceived(object sender, ClientHeartbeatEventArgs e)
		{
			OnClientHeartbeatReceived(e);
		}

		#endregion
	}
}
