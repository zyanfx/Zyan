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
using System.Linq.Expressions;
using Zyan.InterLinq.Expressions;

namespace Zyan.Communication
{
	/// <summary>
	/// Maintains a connection to a Zyan component host.
	/// </summary>
	public class ZyanConnection : IDisposable
	{
		#region Configuration

		// URL of server
		private string _serverUrl = string.Empty;

		// Name of the remote component host
		private string _componentHostName = string.Empty;

		// Protocol and communication settings
		private IClientProtocolSetup _protocolSetup = null;

		// Remoting-Channel
		private IChannel _remotingChannel = null;

		// List of created proxies
		private List<WeakReference> _proxies; 

		/// <summary>
		/// Gets the URL of the remote server.
		/// </summary>
		public string ServerUrl
		{
			get { return _serverUrl; }
		}

		/// <summary>
		/// Gets the name of the remote component host.
		/// </summary>
		public string ComponentHostName
		{
			get { return _componentHostName; }
		}

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="setup">Objekt mit Konfigurationseinstellungen für die Verbindung</param>
		public ZyanConnection(ZyanConnectionSetup setup)
			: this(setup.ServerUrl, setup.ProtocolSetup, setup.Credentials, setup.AutoLoginOnExpiredSession, setup.KeepSessionAlive)
		{ }

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>                
		public ZyanConnection(string serverUrl)
			: this(serverUrl, new TcpBinaryClientProtocolSetup(), null, false, true)
		{ }

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>                
		/// <param name="autoLoginOnExpiredSession">Specifies whether the proxy should relogin automatically when the session expired</param>
		public ZyanConnection(string serverUrl, bool autoLoginOnExpiredSession)
			: this(serverUrl, new TcpBinaryClientProtocolSetup(), null, autoLoginOnExpiredSession, !autoLoginOnExpiredSession)
		{ }

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>                
		/// <param name="protocolSetup">Protocol an communication settings</param>
		public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup)
			: this(serverUrl, protocolSetup, null, false, true)
		{ }

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>                
		/// <param name="protocolSetup">Protocol an communication settings</param>
		/// <param name="autoLoginOnExpiredSession">Specifies whether the proxy should relogin automatically when the session expired</param>
		public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup, bool autoLoginOnExpiredSession)
			: this(serverUrl, protocolSetup, null, autoLoginOnExpiredSession, true)
		{ }

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>        
		/// <param name="protocolSetup">Protocol an communication settings</param>
		/// <param name="credentials">Login credentials</param>
		/// <param name="autoLoginOnExpiredSession">Specifies whether the proxy should relogin automatically when the session expired</param>
		/// <param name="keepSessionAlive">Specifies whether the session should be automaticly kept alive</param>
		public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup, Hashtable credentials, bool autoLoginOnExpiredSession, bool keepSessionAlive)
		{
			if (string.IsNullOrEmpty(serverUrl))
				throw new ArgumentException(LanguageResource.ArgumentException_ServerUrlMissing, "serverUrl");

			if (protocolSetup == null)
				throw new ArgumentNullException("protocolSetup");

			_proxies = new List<WeakReference>();

			_protocolSetup = protocolSetup;
			_sessionID = Guid.NewGuid();
			_serverUrl = serverUrl;
			_autoLoginOnExpiredSession = autoLoginOnExpiredSession;
			_keepSessionAlive = keepSessionAlive;

			if (_autoLoginOnExpiredSession)
				_autoLoginCredentials = credentials;

			_serializationHandling = new SerializationHandlerRepository();
			CallInterceptionEnabled = false;
			_callInterceptors = new CallInterceptorCollection();
			RegisterStandardSerializationHandlers();
			string[] addressParts = _serverUrl.Split('/');
			_componentHostName = addressParts[addressParts.Length - 1];

			_remotingChannel = _protocolSetup.CreateChannel();

			if (_remotingChannel != null)
				ChannelServices.RegisterChannel(_remotingChannel, false);

			_subscriptions = new Dictionary<Guid, NotificationReceiver>();
			
			if (credentials != null && credentials.Count == 0)
				credentials = null;

			RemoteDispatcher.Logon(_sessionID, credentials);

			_registeredComponents = new List<ComponentInfo>(RemoteDispatcher.GetRegisteredComponents());
			_sessionAgeLimit = RemoteDispatcher.SessionAgeLimit;

			StartKeepSessionAliveTimer();

			_connections.Add(this);
		}

		#endregion

		#region Session Management

		// Unique Session key
		private Guid _sessionID = Guid.Empty;

		// Switch for relogin after expired session
		private bool _autoLoginOnExpiredSession = false;

		// Login credentials
		private Hashtable _autoLoginCredentials = null;

		// Switch for keeping session alive
		private bool _keepSessionAlive = true;

		// Timer to provide interval for keeping session alive
		private Timer _keepSessionAliveTimer = null;

		// Maximum session lifetime (in minutes)
		private int _sessionAgeLimit = 0;

		/// <summary>
		/// Prepares the .NET Remoting call context before a remote call.
		/// </summary>
		internal void PrepareCallContext(bool implicitTransactionTransfer)
		{
			LogicalCallContextData data = new LogicalCallContextData();
			data.Store.Add("sessionid", _sessionID);

			if (implicitTransactionTransfer && Transaction.Current != null)
			{
				Transaction transaction = Transaction.Current;

				if (transaction.TransactionInformation.Status == TransactionStatus.InDoubt ||
					transaction.TransactionInformation.Status == TransactionStatus.Active)
				{
					data.Store.Add("transaction", transaction);
				}
			}
			CallContext.SetData("__ZyanContextData_" + _componentHostName, data);
		}

		/// <summary>
		/// Starts the session keep alive timer.
		/// <remarks>
		/// If the timer runs already, it will be restarted with current settings.
		/// </remarks>
		/// </summary>
		private void StartKeepSessionAliveTimer()
		{
			if (_keepSessionAliveTimer != null)
				_keepSessionAliveTimer.Dispose();

			if (_keepSessionAlive)
			{
				int interval = (_sessionAgeLimit / 2) * 60000;
				_keepSessionAliveTimer = new Timer(new TimerCallback(KeepSessionAlive), null, interval, interval);
			}
		}

		/// <summary>
		/// Will be called from session keep alive timer on ervery interval.
		/// </summary>
		/// <param name="state">State (not used)</param>
		private void KeepSessionAlive(object state)
		{
			try
			{
				PrepareCallContext(false);

				int serverSessionAgeLimit = RemoteDispatcher.RenewSession();

				if (_sessionAgeLimit != serverSessionAgeLimit)
				{
					_sessionAgeLimit = serverSessionAgeLimit;
					StartKeepSessionAliveTimer();
				}
			}
			catch
			{ }
		}

		#endregion

		#region Accessing remote components

		// List of registered remote components (provided by remote components host)
		private List<ComponentInfo> _registeredComponents = null;

		/// <summary>
		/// Creates a local proxy object of a specified remote component.
		/// </summary>
		/// <typeparam name="T">Remote component interface type</typeparam>        
		/// <returns>Proxy</returns>
		public T CreateProxy<T>()
		{
			return CreateProxy<T>(string.Empty, false);
		}

		/// <summary>
		/// Creates a local proxy object of a specified remote component.
		/// </summary>
		/// <typeparam name="T">Remote component interface type</typeparam>        
		/// <param name="uniqueName">Unique component name</param>
		/// <returns>Proxy</returns>
		public T CreateProxy<T>(string uniqueName)
		{
			return CreateProxy<T>(uniqueName, false);
		}

		/// <summary>
		/// Creates a local proxy object of a specified remote component.
		/// </summary>
		/// <typeparam name="T">Remote component interface type</typeparam>
		/// <param name="implicitTransactionTransfer">Specify whether transactions (System.Transactions) should be transferred to remote component automaticly</param>
		/// <returns>Proxy</returns>
		public T CreateProxy<T>(bool implicitTransactionTransfer)
		{
			return CreateProxy<T>(string.Empty, implicitTransactionTransfer);
		}

		/// <summary>
		/// Creates a local proxy object of a specified remote component.
		/// </summary>
		/// <typeparam name="T">Remote component interface type</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="implicitTransactionTransfer">Specify whether transactions (System.Transactions) should be transferred to remote component automaticly</param>
		/// <returns>Proxy</returns>
		public T CreateProxy<T>(string uniqueName, bool implicitTransactionTransfer)
		{
			Type interfaceType = typeof(T);

			if (string.IsNullOrEmpty(uniqueName))
				uniqueName = interfaceType.FullName;

			if (!interfaceType.IsInterface)
				throw new ApplicationException(string.Format("Der angegebene Typ '{0}' ist keine Schnittstelle! Für die Erzeugung einer entfernten Komponenteninstanz, wird deren öffentliche Schnittstelle benötigt!", interfaceType.FullName));

			ComponentInfo info = (from entry in _registeredComponents
								  where entry.UniqueName.Equals(uniqueName)
								  select entry).FirstOrDefault();

			if (info == null)
				throw new ApplicationException(string.Format("Für Schnittstelle '{0}' ist auf dem Server '{1}' keine Komponente registriert.", interfaceType.FullName, _serverUrl));

			ZyanProxy proxy = new ZyanProxy(info.UniqueName, typeof(T), this, implicitTransactionTransfer, _sessionID, _componentHostName, _autoLoginOnExpiredSession, _autoLoginCredentials, info.ActivationType);
			
			WeakReference proxyReference = new WeakReference(proxy);
			_proxies.Add(proxyReference);
			
			return (T)proxy.GetTransparentProxy();
		}

		// Proxy of remote dispatcher
		private IZyanDispatcher _remoteDispatcher = null;

		/// <summary>
		/// Gets a proxy to access the remote dispatcher.
		/// </summary>
		protected internal IZyanDispatcher RemoteDispatcher
		{
			get
			{
				if (_remoteDispatcher == null)
					_remoteDispatcher = (IZyanDispatcher)Activator.GetObject(typeof(IZyanDispatcher), _serverUrl);
				
				return _remoteDispatcher;
			}
		}

		#endregion

		#region User defined serialization

		// Repository of seialization handlers
		private SerializationHandlerRepository _serializationHandling = null;

		/// <summary>
		/// Returns the repository of serialization handlers.
		/// </summary>
		public SerializationHandlerRepository SerializationHandling
		{
			get { return _serializationHandling; }
		}

		/// <summary>
		/// Registeres standard serialization handlers.
		/// </summary>
		private void RegisterStandardSerializationHandlers()
		{
			// TODO: use MEF to discover and register standard serialization handlers:
			// [Export(ISerializationHandler), ExportMetadata("SerializedType", typeof(Expression))]
			SerializationHandling.RegisterSerializationHandler(typeof(Expression), new ExpressionSerializationHandler());
		}

		#endregion

		#region Call tracking

		/// <summary>
		/// Event: Before a remote call is invoked.
		/// </summary>
		public event EventHandler<BeforeInvokeEventArgs> BeforeInvoke;

		/// <summary>
		/// Event: After a remote call is invoked.
		/// </summary>
		public event EventHandler<AfterInvokeEventArgs> AfterInvoke;

		/// <summary>
		/// Event: When a remote call is canceled.
		/// </summary>
		public event EventHandler<InvokeCanceledEventArgs> InvokeCanceled;

		/// <summary>
		/// Fires the BeforeInvoke event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected internal virtual void OnBeforeInvoke(BeforeInvokeEventArgs e)
		{
			// Wenn für BeforeInvoke Ereignisprozeduren registriert sind ...
			if (BeforeInvoke != null)
				// Ereignis feuern
				BeforeInvoke(this, e);
		}

		/// <summary>
		/// Fires the AfterInvoke event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected internal virtual void OnAfterInvoke(AfterInvokeEventArgs e)
		{
			// Wenn für AfterInvoke Ereignisprozeduren registriert sind ...
			if (AfterInvoke != null)
				// Ereignis feuern
				AfterInvoke(this, e);
		}

		/// <summary>
		/// Fires the InvokeCanceled event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected internal virtual void OnInvokeCanceled(InvokeCanceledEventArgs e)
		{
			// Wenn für AfterInvoke Ereignisprozeduren registriert sind ...
			if (InvokeCanceled != null)
				// Ereignis feuern
				InvokeCanceled(this, e);
		}

		#endregion

		#region Notification (old NotificationService feature) 

		// Repository for event subscriptions (NotficationService)
		private volatile Dictionary<Guid, NotificationReceiver> _subscriptions = null;

		// Object for thread synchronization of event subscriptions
		private object _subscriptionsLockObject = new object();

		/// <summary>
		/// Subscribes for receiving notifications of a specified event.
		/// </summary>
		/// <param name="eventName">Event name</param>
		/// <param name="handler">Client side event handler</param>
		/// <returns>Unique subscription ID</returns>
		[Obsolete("The NotificationService feature may not be supported in future Zyan versions. Please use remote delegates to create your notification system.",false)]
		public Guid SubscribeEvent(string eventName, EventHandler<NotificationEventArgs> handler)
		{
			NotificationReceiver receiver = new NotificationReceiver(eventName, handler);
			RemoteDispatcher.Subscribe(eventName, receiver.FireNotifyEvent);

			Guid subscriptionID = Guid.NewGuid();

			lock (_subscriptionsLockObject)
			{
				_subscriptions.Add(subscriptionID, receiver);
			}
			return subscriptionID;
		}

		/// <summary>
		/// Unsubscribe for receiving notifications of a specified event.
		/// </summary>
		/// <param name="subscriptionID">Unique subscription ID</param>
		[Obsolete("The NotificationService feature may not be supported in future Zyan versions. Please use remote delegates to create your notification system.")]
		public void UnsubscribeEvent(Guid subscriptionID)
		{
			lock (_subscriptionsLockObject)
			{
				if (_subscriptions.ContainsKey(subscriptionID))
				{
					NotificationReceiver receiver = _subscriptions[subscriptionID];
					RemoteDispatcher.Unsubscribe(receiver.EventName, receiver.FireNotifyEvent);

					_subscriptions.Remove(subscriptionID);

					receiver.Dispose();
				}
			}
		}

		#endregion

		#region Intercept calls

		/// <summary>
		/// Gets whether registered call interceptors should be processed.
		/// </summary>
		public bool CallInterceptionEnabled
		{
			get;
			set;
		}

		// List of registered call interceptors.
		private CallInterceptorCollection _callInterceptors = null;

		/// <summary>
		/// Returns a collection of registred call interceptors.
		/// </summary>
		public CallInterceptorCollection CallInterceptors
		{
			get { return _callInterceptors; }
		}

		#endregion

		#region Centralized error handling

		/// <summary>
		/// Event: Occures when a error is detected.
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

		#region Dispose implementation

		// Switch to mark object as disposed.
		private bool _isDisposed = false;

		/// <summary>
		/// Release managed resources.
		/// </summary>
		public void Dispose()
		{
			if (!_isDisposed)
			{
				_isDisposed = true;

				if (_pollingTimer != null)
				{
					_pollingTimer.Dispose();
					_pollingTimer = null;
				}
				_pollingEnabled = false;
				
				if (_keepSessionAliveTimer != null)
				{
					_keepSessionAliveTimer.Dispose();
					_keepSessionAliveTimer = null;
				}
				try
				{
					if (_proxies != null)
					{
						foreach (var proxyReference in _proxies)
						{
							if (proxyReference.IsAlive)
							{
								var proxy = proxyReference.Target as ZyanProxy;
								proxy.RemoveAllRemoteEventHandlers();								
							}
						}
					}
					RemoteDispatcher.Logoff(_sessionID);
				}
				catch (RemotingException)
				{ }
				catch (SocketException)
				{ }
				catch (WebException)
				{ }
				finally
				{
					_connections.Remove(this);
				}
				if (_remotingChannel != null)
				{
					if (ChannelServices.GetChannel(_remotingChannel.ChannelName)!=null)
						ChannelServices.UnregisterChannel(_remotingChannel);

					_remotingChannel = null;
				}
				_remoteDispatcher = null;
				_serverUrl = string.Empty;
				_sessionID = Guid.Empty;
				_protocolSetup = null;
				_serializationHandling = null;
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
		/// Called from CLR when the object is finalized.
		/// </summary>
		~ZyanConnection()
		{
			Dispose();
		}

		#endregion

		#region Accessing connections

		// List of connections
		private static List<ZyanConnection> _connections = new List<ZyanConnection>();

		/// <summary>
		/// Gets a list of all known Zyan connections in the current Application Domain.
		/// </summary>
		public static List<ZyanConnection> Connections
		{
			get { return _connections.ToList<ZyanConnection>(); }
		}

		#endregion

		#region Detect unexpected disconnection (Polling)

		// Polling timer (Triggers check for unexpected disconnection when elapsed)
		private Timer _pollingTimer = null;

		// Interval of polling timer
		private TimeSpan _pollingInterval = TimeSpan.FromMinutes(1);

		// Switch to enable polling
		private bool _pollingEnabled = false;

		/// <summary>
		/// Event: Fired when disconnected.
		/// </summary>
		public event EventHandler<DisconnectedEventArgs> Disconnected;

		/// <summary>
		/// Fires the Disconnected event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected virtual void OnDisconnected(DisconnectedEventArgs e)
		{
			if (Disconnected!=null)
				Disconnected(this,e);
		}

		/// <summary>
		/// Gets whether polling is enabled.
		/// </summary>
		public bool PollingEnabled
		{
			get { return _pollingEnabled; }
			set 
			{
				if (value != _pollingEnabled)
				{
					_pollingEnabled = value;
					StartPollingTimer();
				}
			}
		}

		/// <summary>
		/// Gets or sets the polling interval.
		/// <remarks>
		/// Default is 1 minute.
		/// </remarks>
		/// </summary>
		public TimeSpan PollingInterval
		{
			get { return _pollingInterval; }
			set
			{ 
				_pollingInterval=value;
				StartPollingTimer();
			}
		}

		/// <summary>
		/// Starts the polling timer.
		/// <remarks>
		/// If the timer runs already, it will be restarted with current settings.
		/// </remarks>
		/// </summary>
		private void StartPollingTimer()
		{
			if (_pollingTimer != null)
				_pollingTimer.Dispose();

			if (_pollingEnabled)
			{
				int interval = Convert.ToInt32(_pollingInterval.TotalMilliseconds);
				_pollingTimer = new Timer(new TimerCallback(SendHeartbeat), null, interval, interval);
			}
		}

		/// <summary>
		/// Will be called from polling timer on ervery interval.
		/// </summary>
		/// <param name="state">State (not used)</param>
		private void SendHeartbeat(object state)
		{
			try
			{
				RemoteDispatcher.ReceiveClientHeartbeat(_sessionID);	
			}
			catch (Exception ex)
			{
				PollingEnabled = false;
				bool problemSolved = false;

				DisconnectedEventArgs e = new DisconnectedEventArgs() 
				{ 
					Exception=ex,
					RetryCount=0,
					Retry=false
				};

				OnDisconnected(e);

				while (e.Retry)
				{					
					e.Retry = false;

					Thread.Sleep(Convert.ToInt32(_pollingInterval.TotalMilliseconds));

					try
					{
						RemoteDispatcher.ReceiveClientHeartbeat(_sessionID);
						problemSolved = true;						
					}
					catch (Exception retryEx)
					{
						e.Exception = retryEx;
						e.RetryCount++;
						OnDisconnected(e);
					}
				}
				if (problemSolved)
					PollingEnabled = true;
				else
					Dispose();
			}
		}

		#endregion
	}
}
