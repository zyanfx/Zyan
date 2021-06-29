﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Transactions;
using Castle.DynamicProxy;
using CoreRemoting;
using CoreRemoting.Channels.Websocket;
using CoreRemoting.Serialization.Binary;
using Zyan.Communication.Delegates;
using Zyan.Communication.Discovery;
using Zyan.Communication.Discovery.Metadata;
using Zyan.Communication.Protocols;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Toolbox.Diagnostics;
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
		private IRemotingClient _remotingClient = null;

		// List of created proxies
		private List<WeakReference> _proxies;

		private ProxyGenerator _proxyGenerator;
		
		/// <summary>
		/// Gets the alive proxies.
		/// </summary>
		private IEnumerable<ZyanProxy> AliveProxies
		{
			get
			{
				if (_proxies == null)
				{
					return new ZyanProxy[0];
				}

				lock (_proxies)
				{
					return _proxies.Where(p => p.IsAlive).Select(p => p.Target as ZyanProxy).Where(p => p != null).ToArray();
				}
			}
		}

		// Local event subscriptions tracker
		private SubscriptionTracker _localSubscriptionTracker = new SubscriptionTracker();

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
		public string ComponentHostName => _componentHostName;

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="setup">Object with configuration settings for the connection</param>
		public ZyanConnection(ZyanConnectionSetup setup)
			: this(setup.ServerUrl, setup.ProtocolSetup, setup.Credentials, setup.AutoLoginOnExpiredSession, setup.KeepSessionAlive)
		{
		}

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>
		/// <param name="autoLoginOnExpiredSession">Specifies whether the proxy should relogin automatically when the session expired</param>
		public ZyanConnection(string serverUrl, bool autoLoginOnExpiredSession = false)
			: this(serverUrl, ClientProtocolSetup.GetClientProtocol(serverUrl), null, autoLoginOnExpiredSession, !autoLoginOnExpiredSession)
		{
		}

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>
		/// <param name="protocolSetup">Protocol an communication settings</param>
		/// <param name="autoLoginOnExpiredSession">Specifies whether the proxy should relogin automatically when the session expired</param>
		public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup, bool autoLoginOnExpiredSession = false)
			: this(serverUrl, protocolSetup, null, autoLoginOnExpiredSession, true)
		{
		}

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp").</param>
		/// <param name="credentials">Login credentials.</param>
		/// <param name="autoLoginOnExpiredSession">Specifies whether the proxy should relogin automatically when the session expired.</param>
		/// <param name="keepSessionAlive">Specifies whether the session should be automaticly kept alive.</param>
		public ZyanConnection(string serverUrl, Hashtable credentials, bool autoLoginOnExpiredSession, bool keepSessionAlive)
			: this(serverUrl, ClientProtocolSetup.GetClientProtocol(serverUrl), credentials, autoLoginOnExpiredSession, true)
		{
		}

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp").</param>
		/// <param name="protocolSetup">Protocol and communication settings.</param>
		/// <param name="credentials">Login credentials.</param>
		/// <param name="autoLoginOnExpiredSession">Specifies whether the proxy should relogin automatically when the session expired.</param>
		/// <param name="keepSessionAlive">Specifies whether the session should be automaticly kept alive.</param>
		public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup, AuthCredentials credentials, bool autoLoginOnExpiredSession, bool keepSessionAlive)
		{
			if (string.IsNullOrEmpty(serverUrl))
				throw new ArgumentException(LanguageResource.ArgumentException_ServerUrlMissing, nameof(serverUrl));

			// if (protocolSetup == null)
			// {
			// 	// try to select the protocol automatically
			// 	protocolSetup = ClientProtocolSetup.GetClientProtocol(serverUrl);
			// 	if (protocolSetup == null)
			// 		throw new ArgumentNullException(nameof(protocolSetup));
			// }

			// if (!protocolSetup.IsUrlValid(serverUrl))
			// 	throw new ArgumentException(LanguageResource.ArgumentException_ServerUrlIsInvalid, nameof(serverUrl));

			_proxyGenerator = new ProxyGenerator();
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
			//TODO: Migrate CallInterception 
			//_callInterceptors = new CallInterceptorCollection();
			RegisterStandardSerializationHandlers();
			string[] addressParts = _serverUrl.Split('/');
			_componentHostName = addressParts[addressParts.Length - 1];

			var	clientConfig = _protocolSetup.BuildClientConfig();
			clientConfig.Serializer = new BinarySerializerAdapter();

			_remotingClient = new RemotingClient(clientConfig);
			_remotingClient.Connect();
			
			var connectionNotification = _remotingClient as IConnectionNotification;
			if (connectionNotification != null)
			{
				connectionNotification.ConnectionEstablished += Channel_ConnectionEstablished;
			}

			string channelName = _protocolSetup.ChannelName;

			if (credentials == null)
				credentials = new AuthCredentials();

			try
			{
				credentials.Authenticate(_sessionID, RemoteDispatcher);

				_registeredComponents = new List<ComponentInfo>(RemoteDispatcher.GetRegisteredComponents());
				_sessionAgeLimit = RemoteDispatcher.SessionAgeLimit;
			}
			catch (Exception ex)
			{
				if (_remotingClient != null)
				{
					_remotingClient.Dispose();
					_remotingClient = null;
				}

				_remoteDispatcher = null;
				throw ex.PreserveStackTrace();
			}

			var reconnectEvents = new Action(ReconnectRemoteEventsCore).ExecuteByOneThreadAtMost();
			var debounceInterval = ZyanSettings.ReconnectRemoteEventsDebounceInterval.TotalMilliseconds;
			ReconnectRemoteEvents = reconnectEvents.CancellableDebounce((int)debounceInterval);

			StartKeepSessionAliveTimer();
			lock (_connections)
			{
				_connections.Add(this);
			}
		}

		#endregion

		#region Session Management

		// Unique Session key
		private Guid _sessionID = Guid.Empty;

		// Switch for relogin after expired session
		private bool _autoLoginOnExpiredSession = false;

		// Login credentials
		private AuthCredentials _autoLoginCredentials = null;

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
			{
			}
		}

		/// <summary>
		/// Gets the session ID.
		/// </summary>
		public Guid SessionID { get { return _sessionID; } }

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
		/// <param name="implicitTransactionTransfer">Specify whether transactions (System.Transactions) should be transferred to remote component automatically.</param>
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
		/// <param name="implicitTransactionTransfer">Specify whether transactions (System.Transactions) should be transferred to remote component automatically.</param>
		/// <returns>Proxy</returns>
		public T CreateProxy<T>(string uniqueName, bool implicitTransactionTransfer)
		{
			return CreateProxy<T>(uniqueName, implicitTransactionTransfer, false);
		}

		/// <summary>
		/// Creates a local proxy object of a specified remote component.
		/// </summary>
		/// <typeparam name="T">Remote component interface type</typeparam>
		/// <param name="uniqueName">Unique component name</param>
		/// <param name="implicitTransactionTransfer">Specify whether transactions (System.Transactions) should be transferred to remote component automatically.</param>
		/// <param name="keepCallbackSynchronizationContext">Specify whether callbacks and events should use the original synchronization context.</param>
		/// <returns>Proxy</returns>
		public T CreateProxy<T>(string uniqueName, bool implicitTransactionTransfer, bool keepCallbackSynchronizationContext)
		{
			Type interfaceType = typeof(T);

			if (string.IsNullOrEmpty(uniqueName))
				uniqueName = interfaceType.FullName;

			if (!interfaceType.IsInterface)
				throw new ApplicationException(string.Format(LanguageResource.ApplicationException_SpecifiedTypeIsNotAnInterface, interfaceType.FullName));

			ComponentInfo info;
			lock (_registeredComponents)
			{
				info = (from entry in _registeredComponents
						where entry.UniqueName.Equals(uniqueName)
						select entry).FirstOrDefault();
			}

			if (info == null)
				throw new ApplicationException(string.Format(LanguageResource.ApplicationException_NoServerComponentIsRegisteredForTheGivenInterface, interfaceType.FullName, _serverUrl));

			var proxy = new ZyanProxy(info.UniqueName, interfaceType, this, implicitTransactionTransfer, keepCallbackSynchronizationContext, _sessionID, _componentHostName, _autoLoginOnExpiredSession, info.ActivationType);
			
			lock (_proxies)
			{
				var proxyReference = new WeakReference(proxy);
				_proxies.Add(proxyReference);
			}

			return 
				(T)_proxyGenerator.CreateInterfaceProxyWithoutTarget(
					interfaceToProxy: interfaceType,
					interceptor: (IInterceptor)proxy);
		}

		// Proxy of remote dispatcher
		private IZyanDispatcher _remoteDispatcher = null;

		/// <summary>
		/// Gets a proxy to access the remote dispatcher.
		/// </summary>
		protected internal IZyanDispatcher RemoteDispatcher
		{
			get { return _remoteDispatcher ?? (_remoteDispatcher = _remotingClient.CreateProxy<IZyanDispatcher>()); }
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
			BeforeInvoke?.Invoke(this, e);
		}

		/// <summary>
		/// Fires the AfterInvoke event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected internal virtual void OnAfterInvoke(AfterInvokeEventArgs e)
		{
			AfterInvoke?.Invoke(this, e);
		}

		/// <summary>
		/// Fires the InvokeCanceled event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected internal virtual void OnInvokeCanceled(InvokeCanceledEventArgs e)
		{
			InvokeCanceled?.Invoke(this, e);
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

		// // List of registered call interceptors.
		// private CallInterceptorCollection _callInterceptors = null;
		//
		// /// <summary>
		// /// Returns a collection of registred call interceptors.
		// /// </summary>
		// public CallInterceptorCollection CallInterceptors
		// {
		// 	get { return _callInterceptors; }
		// }

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
			Error?.Invoke(this, e);
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
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and — optionally — managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

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
					foreach (var zyanProxy in AliveProxies)
					{
						zyanProxy.RemoveAllRemoteEventHandlers();
					}
					RemoteDispatcher.Logoff(_sessionID);
				}
				catch (RemotingException)
				{ }
				catch (SocketException)
				{ }
				catch (WebException)
				{ }
				catch (Exception ex)
				{
					Trace.WriteLine("Unexpected exception of type {0} caught while disposing ZyanConnection: {1}", ex.GetType(), ex.Message);
				}
				finally
				{
					lock (_connections)
					{
						_connections.Remove(this);
					}
				}
				if (_remotingClient != null)
				{
					//TODO: Migrate connection notification.
					// unsubscribe from connection notifications
					//_remotingClient.ConnectionEstablished -= Channel_ConnectionEstablished;

					if (_remotingClient != null)
					{
						_remotingClient.Dispose();
						_remotingClient = null;
					}
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
				//TODO: Migrate call interception
				// if (_callInterceptors != null)
				// {
				// 	_callInterceptors.Clear();
				// 	_callInterceptors = null;
				// }
				if (_autoLoginCredentials != null)
				{
					_autoLoginCredentials = null;
				}
				GC.WaitForPendingFinalizers();
			}
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
		/// Occurs when disconnection is detected.
		/// </summary>
		public event EventHandler<DisconnectedEventArgs> Disconnected;

		/// <summary>
		/// Fires the Disconnected event.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		protected virtual void OnDisconnected(DisconnectedEventArgs e)
		{
			Disconnected?.Invoke(this, e);
		}

		/// <summary>
		/// Occurs when connection is restored.
		/// </summary>
		public event EventHandler Reconnected;

		/// <summary>
		/// Fires the Reconnected event.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		protected virtual void OnReconnected(EventArgs e)
		{
			Reconnected?.Invoke(this, e);
		}

		/// <summary>
		/// Event: Fired when a new logon is needed, after a detected diconnection.
		/// </summary>
		public event EventHandler<NewLogonNeededEventArgs> NewLogonNeeded;

		/// <summary>
		/// Fires the NewLogonNeeded event.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		/// <returns>True, if the event is handled, otherwise, false.</returns>
		protected virtual bool OnNewLogonNeeded(NewLogonNeededEventArgs e)
		{
			var newLogonNeeded = NewLogonNeeded;
			if (newLogonNeeded != null)
			{
				newLogonNeeded(this, e);
				return true;
			}

			return false;
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
				_pollingInterval = value;
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

		private bool _sendingHeartbeat;

		/// <summary>
		/// Will be called from polling timer on every interval.
		/// </summary>
		/// <param name="state">State (not used)</param>
		internal void SendHeartbeat(object state)
		{
			if (_sendingHeartbeat)
			{
				// if polling timer interval is less than
				// channel timeout, skip sending a heartbeat
				return;
			}

			try
			{
				_sendingHeartbeat = true;
				try
				{
					PrepareCallContext(false);
					RemoteDispatcher.ReceiveClientHeartbeat(_sessionID);
				}
				finally
				{
					_sendingHeartbeat = false;
				}
			}
			catch (Exception ex)
			{
				PollingEnabled = false;
				bool problemSolved = false;

				DisconnectedEventArgs e = new DisconnectedEventArgs()
				{
					Exception = ex,
					RetryCount = 0,
					Retry = false
				};

				OnDisconnected(e);

				while (e.Retry)
				{
					e.Retry = false;

					Thread.Sleep(Convert.ToInt32(_pollingInterval.TotalMilliseconds));

					try
					{
						problemSolved = InternalReconnect();
					}
					catch (Exception retryEx)
					{
						e.Exception = retryEx;
						e.RetryCount++;
						OnDisconnected(e);
					}
				}

				if (problemSolved)
				{
					PollingEnabled = true;
					OnReconnected(EventArgs.Empty);
				}
				else
				{
					// connection wasn't restored, make sure
					// that the polling timer is stopped
					PollingEnabled = false;
				}
			}
		}

		/// <summary>
		/// Reestablish connection to server.
		/// </summary>
		/// <remarks>
		/// This method checks if the session is valid. If not, a new logon in perfomed automatically.
		/// Handle the NewLogonNeeded event to provide credentials.
		/// </remarks>
		/// <returns>True, if reconnecting was successfull, otherwis false </returns>
		public bool Reconnect()
		{
			try
			{
				return InternalReconnect();
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Reestablish connection to server.
		/// </summary>
		/// <remarks>
		/// This method checks if the session is valid. If not, a new logon in perfomed automatically.
		/// Handle the NewLogonNeeded event to provide credentials.
		/// </remarks>
		/// <returns>True, if reconnecting was successfull, otherwis false </returns>
		internal bool InternalReconnect()
		{
			// When the session isn´t valid, the server process must have been restarted
			if (!RemoteDispatcher.ExistSession(_sessionID))
			{
				AuthCredentials credentials = null;
				var performNewLogon = true;

				// If cached auto login credentials are present
				if (_autoLoginOnExpiredSession)
					credentials = _autoLoginCredentials;
				else
				{
					var newLogonNeededEventArgs = new NewLogonNeededEventArgs();
					if (OnNewLogonNeeded(newLogonNeededEventArgs))
					{
						performNewLogon = !newLogonNeededEventArgs.Cancel;
						credentials = newLogonNeededEventArgs.Credentials;
					}
					else
						performNewLogon = false;
				}

				if (performNewLogon)
				{
					if (credentials == null)
					{
						credentials = new AuthCredentials();
					}

					credentials.Authenticate(_sessionID, RemoteDispatcher);
					ReconnectRemoteEvents();

					RemoteDispatcher.ReceiveClientHeartbeat(_sessionID);
					return true;
				}
			}
			else
			{
				RemoteDispatcher.ReceiveClientHeartbeat(_sessionID);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Refreshes the list of server-side components.
		/// </summary>
		public void RefreshRegisteredComponents()
		{
			_registeredComponents = new List<ComponentInfo>(RemoteDispatcher.GetRegisteredComponents());
		}

		/// <summary>
		/// Gets the remote events lock object.
		/// </summary>
		internal object RemoteEventsLock { get; } = new object();

		/// <summary>
		/// Reconnects to all remote events or delegates after a server restart.
		/// </summary>
		private Debouncer.CancellableAction ReconnectRemoteEvents { get; }

		/// <summary>
		/// Reconnects to all remote events or delegates of any known proxy for this connection, after a server restart.
		/// </summary>
		/// <remarks>
		/// Does not check, if the event handler registrations are truly lost.
		/// </remarks>
		private void ReconnectRemoteEventsCore()
		{
			if (_isDisposed)
			{
				return;
			}

			try
			{
				lock (RemoteEventsLock)
				{
					var subscriptions = AliveProxies.Select(p => p.GetActiveSubscriptions()).Where(s => !s.DelegateCorrelationSet.IsNullOrEmpty()).ToArray();
					var correlationSets = subscriptions.SelectMany(s => s.DelegateCorrelationSet).ToArray();
					_localSubscriptionTracker.Reset(correlationSets);

					PrepareCallContext(false);
					RemoteDispatcher.ReconnectEventHandlers(subscriptions);
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Error while restoring client subscriptions: {0}", ex);
			}
		}

		/// <summary>
		/// Adds the specified correlation set to the subscription tracker.
		/// </summary>
		internal void TrackRemoteSubscriptions(IEnumerable<DelegateCorrelationInfo> delegateCorrelationSet)
		{
			_localSubscriptionTracker.Add(delegateCorrelationSet);
		}

		/// <summary>
		/// Removes the specified correlation set to the subscription tracker.
		/// </summary>
		internal void UntrackRemoteSubscriptions(IEnumerable<DelegateCorrelationInfo> delegateCorrelationSet)
		{
			_localSubscriptionTracker.Remove(delegateCorrelationSet);
		}

		/// <summary>
		/// Checks the remote subscription counter and reconnects remote events if needed.
		/// </summary>
		internal void CheckRemoteSubscriptionCounter()
		{
			var key = SubscriptionTracker.CallContextSlotName;
			var callContextData = CallContext.GetData("__ZyanContextData_" + _componentHostName) as LogicalCallContextData;
			if (callContextData != null && callContextData.Store != null && callContextData.Store.ContainsKey(key))
			{
				// if the server was restarted, is has less subscriptions than the client
				var remoteChecksum = callContextData.Store[key].ToString();
				if (remoteChecksum != _localSubscriptionTracker.Checksum)
				{
					// restore subscriptions asynchronously
					ReconnectRemoteEvents();
				}
				else
				{
					// checksums match on both ends, so pending
					// ReconnectRemoteEvents call, if any, is canceled
					ReconnectRemoteEvents(false);
				}
			}
		}

		private void Channel_ConnectionEstablished(object sender, EventArgs e)
		{
			// restore subscriptions if necessary
			if (_localSubscriptionTracker.Count > 0)
			{
				ReconnectRemoteEvents();
			}
		}

		/// <summary>
		/// Gets true, if the session on server is valid, otherwise false.
		/// </summary>
		public bool IsSessionValid
		{
			get
			{
				try
				{
					return RemoteDispatcher.ExistSession(_sessionID);
				}
				catch
				{
					return false;
				}
			}
		}

		#endregion

		#region Service discovery

		/// <summary>
		/// Discovers the available <see cref="ZyanComponentHost"/> instances in the local network.
		/// </summary>
		/// <param name="namePattern">The <see cref="ZyanComponentHost"/> name pattern.</param>
		/// <param name="responseHandler">The response handler is called every time a server is discovered.</param>
		public static void DiscoverHosts(string namePattern, Action<DiscoveryResponse> responseHandler)
		{
			DiscoverHosts(namePattern, null, DiscoveryServer.DefaultDiscoveryPort, responseHandler);
		}

		/// <summary>
		/// Discovers the available <see cref="ZyanComponentHost"/> instances in the local network.
		/// </summary>
		/// <param name="namePattern">The <see cref="ZyanComponentHost"/> name pattern.</param>
		/// <param name="version">Desired server version.</param>
		/// <param name="port">Discovery service port.</param>
		/// <param name="responseHandler">The response handler is called every time a server is discovered.</param>
		public static void DiscoverHosts(string namePattern, string version, int port, Action<DiscoveryResponse> responseHandler)
		{
			if (string.IsNullOrEmpty(namePattern))
			{
				throw new ArgumentNullException("namePattern", "ZyanComponentHost name pattern is required for discovery.");
			}

			if (responseHandler == null)
			{
				throw new ArgumentNullException("responseHandler", "Response handler is required for discovery.");
			}

			// start service discovery
			var request = new DiscoveryRequest(namePattern, version);
			var dc = new DiscoveryClient(request, port);
			dc.Discovered += (s, e) =>
			{
				var response = e.Metadata as DiscoveryResponse;
				if (response != null)
				{
					responseHandler(response);
				}
			};

			// note: the discovery will be stopped automatically when timed out
			dc.StartDiscovery();
		}

		#endregion
	}
}
