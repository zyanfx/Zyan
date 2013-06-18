using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using Zyan.Communication.Notification;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;
using Zyan.Communication.SessionMgmt;
using Zyan.InterLinq.Expressions;
#if !XAMARIN
using DefaultServerProtocolSetup = Zyan.Communication.Protocols.Tcp.TcpBinaryServerProtocolSetup;
#else
using DefaultServerProtocolSetup = Zyan.Communication.Protocols.Tcp.TcpDuplexServerProtocolSetup;
#endif

namespace Zyan.Communication
{
	/// <summary>
	/// Host for publishing components with Zyan.
	/// </summary>
	public class ZyanComponentHost : IComponentCatalog, IDisposable
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanComponentHost" /> class.
		/// </summary>
		/// <param name="name">The name of the component host.</param>
		/// <param name="tcpPort">The TCP port.</param>
		public ZyanComponentHost(string name, int tcpPort)
			: this(name, new DefaultServerProtocolSetup(tcpPort), new InProcSessionManager(), new ComponentCatalog(true))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanComponentHost" /> class.
		/// </summary>
		/// <param name="name">The name of the component host.</param>
		/// <param name="tcpPort">The TCP port.</param>
		/// <param name="catalog">The component catalog.</param>
		public ZyanComponentHost(string name, int tcpPort, ComponentCatalog catalog)
			: this(name, new DefaultServerProtocolSetup(tcpPort), new InProcSessionManager(), catalog)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanComponentHost" /> class.
		/// </summary>
		/// <param name="name">The name of the component host.</param>
		/// <param name="protocolSetup">The protocol setup.</param>
		public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup)
			: this(name, protocolSetup, new InProcSessionManager(), new ComponentCatalog(true))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanComponentHost" /> class.
		/// </summary>
		/// <param name="name">The name of the component host.</param>
		/// <param name="protocolSetup">The protocol setup.</param>
		/// <param name="catalog">The component catalog.</param>
		public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup, ComponentCatalog catalog)
			: this(name, protocolSetup, new InProcSessionManager(), catalog)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanComponentHost" /> class.
		/// </summary>
		/// <param name="name">The name of the component host.</param>
		/// <param name="protocolSetup">The protocol setup.</param>
		/// <param name="sessionManager">The session manager.</param>
		public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup, ISessionManager sessionManager)
			: this(name, protocolSetup, sessionManager, new ComponentCatalog(true))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanComponentHost" /> class.
		/// </summary>
		/// <param name="name">The name of the component host.</param>
		/// <param name="protocolSetup">The protocol setup.</param>
		/// <param name="sessionManager">The session manager.</param>
		/// <param name="catalog">The component catalog.</param>
		public ZyanComponentHost(string name, IServerProtocolSetup protocolSetup, ISessionManager sessionManager, ComponentCatalog catalog)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException(LanguageResource.ArgumentException_ComponentHostNameMissing, "name");

			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			if (sessionManager == null)
				throw new ArgumentNullException("sessionManager");

			if (catalog == null)
				throw new ArgumentNullException("catalog");

			_name = name;
			_protocolSetup = protocolSetup;
			_sessionManager = sessionManager;
			_sessionManager.ClientSessionTerminated += (s, e) => OnClientSessionTerminated(e);
			_catalog = catalog;
			_serializationHandling = new SerializationHandlerRepository();
			_dispatcher = new ZyanDispatcher(this);

			// Set up authentication request delegate
			_authProvider = protocolSetup.AuthenticationProvider;
			this.Authenticate = _authProvider.Authenticate;

			// Register standard serialization handlers
			RegisterStandardSerializationHandlers();

			_hosts.Add(this);
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

		/// <summary>
		/// Gets or sets a value indicating whether legacy blocking events raising mode is enabled.
		/// </summary>
		[Obsolete("Use ZyanSettings.LegacyBlockingEvents static property instead.")]
		public static bool LegacyBlockingEvents { get; set; }

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
		/// Gets the name of the component host.
		/// </summary>
		public string Name
		{
			get { return _name; }
		}

		/// <summary>
		/// Starts listening to the client requests.
		/// </summary>
		private void StartListening()
		{
			var channel = _protocolSetup.CreateChannel();
			if (channel == null)
			{ 
				throw new ApplicationException(LanguageResource.ApplicationException_NoChannel);
			}

			// register the channel and publish the dispatcher
			_channelName = channel.ChannelName;
			ChannelServices.RegisterChannel(channel, false);
			RemotingServices.Marshal(_dispatcher, _name);
		}

		/// <summary>
		/// Stop listening to the client requests.
		/// </summary>
		private void StopListening()
		{
			// detach the dispatcher and close the communication channel
			RemotingServices.Disconnect(_dispatcher);
			CloseChannel();
		}

		/// <summary>
		/// Closes the channel if it is open.
		/// </summary>
		private void CloseChannel()
		{
			// unregister remoting channel
			var channel = ChannelServices.GetChannel(_channelName);
			if (channel != null)
				ChannelServices.UnregisterChannel(channel);

			// dispose channel if it's disposable
			var disposableChannel = channel as IDisposable;
			if (disposableChannel != null)
				disposableChannel.Dispose();
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
		/// Occurs when a client subscribes to a component's event.
		/// </summary>
		public event EventHandler<SubscriptionEventArgs> SubscriptionAdded;

		/// <summary>
		/// Occurs when a client unsubscribes to a component's event.
		/// </summary>
		public event EventHandler<SubscriptionEventArgs> SubscriptionRemoved;

		/// <summary>
		/// Occurs when a component cancels the subscription due to exceptions thrown by the client's event handler.
		/// </summary>
		public event EventHandler<SubscriptionEventArgs> SubscriptionCanceled;

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
			var beforeInvoke = BeforeInvoke;
			if (beforeInvoke != null)
			{
				beforeInvoke(this, e);
			}
		}

		/// <summary>
		/// Fires the AfterInvoke event.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		protected internal virtual void OnAfterInvoke(AfterInvokeEventArgs e)
		{
			var afterInvoke = AfterInvoke;
			if (afterInvoke != null)
			{
				afterInvoke(this, e);
			}
		}

		/// <summary>
		/// Fires the InvokeCanceled event.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		protected internal virtual void OnInvokeCanceled(InvokeCanceledEventArgs e)
		{
			var invokeCanceled = InvokeCanceled;
			if (invokeCanceled != null)
			{
				InvokeCanceled(this, e);
			}
		}

		/// <summary>
		/// Fires the InvokeRejected event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected internal virtual void OnInvokeRejected(InvokeCanceledEventArgs e)
		{
			var invokeRejected = InvokeRejected;
			if (invokeRejected != null)
			{
				invokeRejected(this, e);
			}
		}

		/// <summary>
		/// Raises the <see cref="E:SubscriptionAdded" /> event.
		/// </summary>
		/// <param name="e">The <see cref="SubscriptionEventArgs"/> instance containing the event data.</param>
		protected internal virtual void OnSubscriptionAdded(SubscriptionEventArgs e)
		{
			var subscriptionAdded = SubscriptionAdded;
			if (subscriptionAdded != null)
			{
				subscriptionAdded(this, e);
			}
		}

		/// <summary>
		/// Raises the <see cref="E:SubscriptionRemoved" /> event.
		/// </summary>
		/// <param name="e">The <see cref="SubscriptionEventArgs"/> instance containing the event data.</param>
		protected internal virtual void OnSubscriptionRemoved(SubscriptionEventArgs e)
		{
			var subscriptionRemoved = SubscriptionRemoved;
			if (subscriptionRemoved != null)
			{
				subscriptionRemoved(this, e);
			}
		}

		/// <summary>
		/// Raises the <see cref="E:SubscriptionCanceled" /> event.
		/// </summary>
		/// <param name="e">The <see cref="SubscriptionEventArgs"/> instance containing the event data.</param>
		protected internal virtual void OnSubscriptionCanceled(SubscriptionEventArgs e)
		{
			var subscriptionCanceled = SubscriptionCanceled;
			if (subscriptionCanceled != null)
			{
				subscriptionCanceled(this, e);
			}
		}

		#endregion

		#region Notifications

		// Notification service
		private volatile NotificationService _notificationService = null;

		// Lock object for the notification service instance creation
		private object _notificationServiceLockObject = new object();

		/// <summary>
		/// Gets a value indicating whether the notification service is running.
		/// </summary>
		[Obsolete("The NotificationService feature may not be supported in future Zyan versions. Please use remote delegates to create your notification system.", false)]
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
		/// Starts the notification service.
		/// </summary>
		[Obsolete("The NotificationService feature may not be supported in future Zyan versions. Please use remote delegates to create your notification system.", false)]
		public void StartNotificationService()
		{
			lock (_notificationServiceLockObject)
			{
				if (_notificationService == null)
				{
					_notificationService = new NotificationService();
				}
			}
		}

		/// <summary>
		/// Stops the notification service.
		/// </summary>
		[Obsolete("The NotificationService feature may not be supported in future Zyan versions. Please use remote delegates to create your notification system.", false)]
		public void StopNotificationService()
		{
			lock (_notificationServiceLockObject)
			{
				if (_notificationService != null)
				{
					_notificationService = null;
				}
			}
		}

		/// <summary>
		/// Gets the notification service.
		/// </summary>
		[Obsolete("The NotificationService feature may not be supported in future Zyan versions. Please use remote delegates to create your notification system.", false)]
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

#pragma warning disable 612

		/// <summary>
		/// Publishes the event of the server component.
		/// </summary>
		/// <param name="eventName">Name of the event.</param>
		/// <exception cref="System.ApplicationException"></exception>
		[Obsolete("The NotificationService feature may not be supported in future Zyan versions. Please use remote delegates to create your notification system.", false)]
		public EventHandler<NotificationEventArgs> PublishEvent(string eventName)
		{
			if (!IsNotificationServiceRunning)
				throw new ApplicationException(LanguageResource.ApplicationException_NotificationServiceNotRunning);

			var sender = new NotificationSender(NotificationService, eventName);
			return new EventHandler<NotificationEventArgs>(sender.HandleServerEvent);
		}

#pragma warning restore 612

		#endregion

		#region User defined Serialization Handling

		// Repository of the serialization handlers.
		private SerializationHandlerRepository _serializationHandling = null;

		/// <summary>
		/// Gets the serialization handling repository.
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
		/// Occurs when this instance is disposed.
		/// </summary>
		public event EventHandler Disposing;

		/// <summary>
		/// Fires the Disposing event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected virtual void OnDisposing(EventArgs e)
		{
			if (Disposing != null)
				Disposing(this, e);
		}

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
				OnDisposing(new EventArgs());

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
			if (ClientLoggedOn != null)
				ClientLoggedOn(this, e);
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
