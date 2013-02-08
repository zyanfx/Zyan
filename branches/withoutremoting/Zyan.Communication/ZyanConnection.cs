using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
//using System.Runtime.Remoting;
//using System.Runtime.Remoting.Channels;
//using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Transactions;
using Zyan.Communication.Protocols;
using Zyan.Communication.Protocols.Tcp.DuplexChannel;
using Zyan.Communication.Protocols.Wrapper;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Toolbox.Diagnostics;
using Zyan.InterLinq.Expressions;
using Zyan.Communication.Transport;
using Zyan.Communication.Delegates;

namespace Zyan.Communication
{
	/// <summary>
	/// Maintains a connection to a Zyan component host.
	/// </summary>
	public class ZyanConnection : IDisposable
	{
		#region Configuration

		static ZyanConnection()
		{
			AllowUrlRandomization = true;
		}

		/// <summary>
		/// Enables or disables URL randomization to work around Remoting Identity caching.
		/// </summary>
		public static bool AllowUrlRandomization { get; set; }

		// URL of server
		private string _serverUrl = string.Empty;

		// Name of the remote component host
		private string _componentHostName = string.Empty;

		// Protocol and communication settings
		private IClientProtocolSetup _protocolSetup = null;

		// Transport adapter
		private IClientTransportAdapter _transportAdapter = null;

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
		{
		}

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>
		public ZyanConnection(string serverUrl)
			: this(serverUrl, ClientProtocolSetup.GetClientProtocol(serverUrl), null, false, true)
		{
		}

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>
		/// <param name="autoLoginOnExpiredSession">Specifies whether the proxy should relogin automatically when the session expired</param>
		public ZyanConnection(string serverUrl, bool autoLoginOnExpiredSession)
			: this(serverUrl, ClientProtocolSetup.GetClientProtocol(serverUrl), null, autoLoginOnExpiredSession, !autoLoginOnExpiredSession)
		{
		}

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>
		/// <param name="protocolSetup">Protocol an communication settings</param>
		public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup)
			: this(serverUrl, protocolSetup, null, false, true)
		{
		}

		/// <summary>
		/// Creates a new instance of the ZyanConnection class.
		/// </summary>
		/// <param name="serverUrl">URL of remote server (e.G. "tcp://server1:46123/myapp")</param>
		/// <param name="protocolSetup">Protocol an communication settings</param>
		/// <param name="autoLoginOnExpiredSession">Specifies whether the proxy should relogin automatically when the session expired</param>
		public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup, bool autoLoginOnExpiredSession)
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
			: this(serverUrl, ClientProtocolSetup.GetClientProtocol(serverUrl), null, autoLoginOnExpiredSession, true)
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
		public ZyanConnection(string serverUrl, IClientProtocolSetup protocolSetup, Hashtable credentials, bool autoLoginOnExpiredSession, bool keepSessionAlive)
		{
			if (string.IsNullOrEmpty(serverUrl))
				throw new ArgumentException(LanguageResource.ArgumentException_ServerUrlMissing, "serverUrl");

			if (protocolSetup == null)
			{
				// try to select the protocol automatically
				protocolSetup = ClientProtocolSetup.GetClientProtocol(serverUrl);
				if (protocolSetup == null)
					throw new ArgumentNullException("protocolSetup");
			}

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

			_transportAdapter = _protocolSetup.CreateTransportAdapter();

            //TODO: Implement URL randomization without .NET Remoting dependency
            //if (AllowUrlRandomization)
            //{
            //    _transportChannel = ChannelWrapper.WrapChannel(_transportChannel);
            //}

			if (_transportAdapter != null)
			{
				var registeredChannel = ClientTransportAdapterManager.Instance.GetTransportAdapter(_transportAdapter.UniqueName);

				if (registeredChannel == null)
                    ClientTransportAdapterManager.Instance.Register(_transportAdapter);
			}
			else
				throw new ApplicationException(LanguageResource.ApplicationException_NoChannelCreated);

			string channelName = _transportAdapter.UniqueName;

			if (credentials != null && credentials.Count == 0)
				credentials = null;

            //try
            //{   
                //TODO: Generate logon request message and send it through transport channel
                //RemoteDispatcher.Logon(_sessionID, credentials);                
                _sessionAgeLimit = SendLogonMessage(_sessionID, credentials);
                                
                //TODO: Request registered components through transport channel
				//_registeredComponents = new List<ComponentInfo>(RemoteDispatcher.GetRegisteredComponents());
                _registeredComponents = SendGetRegisteredComponentsMessage();

                //TODO: Extract session age limit from logon response message.
                //_sessionAgeLimit = RemoteDispatcher.SessionAgeLimit;                
            //}
            //catch (Exception ex)
            //{
            //    // unregister remoting channel
            //    var registeredChannel = ClientTransportAdapterManager.Instance.GetTransportAdapter(channelName);
            //    if (registeredChannel != null)
            //        ClientTransportAdapterManager.Instance.Unregister(registeredChannel);

            //    // dispose channel if it's disposable
            //    var disposableChannel = registeredChannel as IDisposable;
            //    if (disposableChannel != null)
            //        disposableChannel.Dispose();

            //    throw ex.PreserveStackTrace();
            //}
			StartKeepSessionAliveTimer();

			_connections.Add(this);
		}

        internal void SendAddEventHandlerMessage(string interfaceType,DelegateCorrelationInfo correlationInfo, string uniqueName)
        {
            //_connection.RemoteDispatcher.AddEventHandler(_interfaceType.FullName, correlationInfo, _uniqueName);
            _transportAdapter.SendRequest(new RequestMessage()
            {
                RequestType = RequestType.SystemOperation,
                MethodName = "AddEventHandler",
                Address = _serverUrl,
                ParameterValues=new object[] { interfaceType, correlationInfo, uniqueName },
                CallContext = PrepareCallContext(false)
            });
        }

        internal void SendRemoveEventHandlerMessage(string interfaceType, DelegateCorrelationInfo correlationInfo, string uniqueName)
        {
            //_connection.RemoteDispatcher.AddEventHandler(_interfaceType.FullName, correlationInfo, _uniqueName);
            _transportAdapter.SendRequest(new RequestMessage()
            {
                RequestType = RequestType.SystemOperation,
                MethodName = "RemoveEventHandler",
                Address = _serverUrl,
                ParameterValues = new object[] { interfaceType, correlationInfo, uniqueName },
                CallContext = PrepareCallContext(false)
            });
        }

        internal List<ComponentInfo> SendGetRegisteredComponentsMessage()
        {
            var registeredComponentsResponseMessage = _transportAdapter.SendRequest(new RequestMessage()
            {
                RequestType = RequestType.SystemOperation,
                MethodName = "GetRegisteredComponents",
                Address = _serverUrl
            });
            return ((ComponentInfo[])registeredComponentsResponseMessage.ReturnValue).ToList();
        }

        internal object SendRemoteMethodCallMessage(Guid trackingID, string uniqueName, List<DelegateCorrelationInfo> correlationSet,string methodName, Type[] genericArgs, Type[] paramTypes, object[] paramValues, LogicalCallContextData callContextData)
        { 
            var rpcResponseMessage = _transportAdapter.SendRequest(new RequestMessage()
            {
                RequestType=RequestType.RemoteMethodCall,
                TrackingID=trackingID,
                Address = _serverUrl,
                DelegateCorrelationSet = correlationSet,
                GenericArguments = genericArgs,
                InterfaceName = uniqueName,
                MethodName = methodName,
                ParameterTypes = paramTypes,
                ParameterValues = paramValues,
                CallContext = callContextData
            });
            return rpcResponseMessage.ReturnValue;
        }

        private int SendLogonMessage(Guid sessionID, Hashtable credentials)
        {
            var logonResponseMessage = _transportAdapter.SendRequest(new RequestMessage()
            {
                RequestType = RequestType.SystemOperation,
                MethodName = "Logon",
                Address = _serverUrl,
                ParameterValues = new object[] { sessionID, credentials }
            });
            _sessionAgeLimit = (int)logonResponseMessage.ReturnValue;

            return _sessionAgeLimit;
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
        internal LogicalCallContextData PrepareCallContext(bool implicitTransactionTransfer)
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
            return data;
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
                var response = _transportAdapter.SendRequest(new RequestMessage() 
                { 
                    RequestType=RequestType.SystemOperation,
                    MethodName="RenewSession",
                    Address=_serverUrl,
                    CallContext=PrepareCallContext(false)
                });
                int serverSessionAgeLimit = (int)response.ReturnValue;

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

			var proxy = new ZyanProxy(info.UniqueName, typeof(T), this, implicitTransactionTransfer, _sessionID, _componentHostName, _autoLoginOnExpiredSession, info.ActivationType);
			lock (_proxies)
			{
				var proxyReference = new WeakReference(proxy);
				_proxies.Add(proxyReference);
			}

			return (T)proxy.GetTransparentProxy();
		}

        //// Proxy of remote dispatcher
        //private IZyanDispatcher _remoteDispatcher = null;

        ///// <summary>
        ///// Gets a proxy to access the remote dispatcher.
        ///// </summary>
        //protected internal IZyanDispatcher RemoteDispatcher
        //{
        //    get
        //    {
        //        if (_remoteDispatcher == null)
        //        {
        //            var serverUrl = _serverUrl;
        //            if (AllowUrlRandomization)
        //                serverUrl = ChannelWrapper.RandomizeUrl(_serverUrl);

        //            _remoteDispatcher = (IZyanDispatcher)Activator.GetObject(typeof(IZyanDispatcher), serverUrl);
        //        }

        //        return _remoteDispatcher;
        //    }
        //}

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
			if (BeforeInvoke != null)
				BeforeInvoke(this, e);
		}

		/// <summary>
		/// Fires the AfterInvoke event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected internal virtual void OnAfterInvoke(AfterInvokeEventArgs e)
		{
			if (AfterInvoke != null)
				AfterInvoke(this, e);
		}

		/// <summary>
		/// Fires the InvokeCanceled event.
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected internal virtual void OnInvokeCanceled(InvokeCanceledEventArgs e)
		{
			if (InvokeCanceled != null)
				InvokeCanceled(this, e);
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
					//RemoteDispatcher.Logoff(_sessionID);
                    _transportAdapter.SendRequest(new RequestMessage() 
                    { 
                        RequestType=RequestType.SystemOperation,
                        MethodName="Logoff",
                        Address=_serverUrl,
                        ParameterValues=new object[] { _sessionID }
                    });
				}
                //TODO: Get rid of .NET Remoting dependency.
                //catch (RemotingException)
                //{ }
				catch (SocketException)
				{ }
				catch (WebException)
				{ }
				catch (MessageException)
				{ }
				catch (Exception ex)
				{
					Trace.WriteLine("Unexpected exception of type {0} caught while disposing ZyanConnection: {1}", ex.GetType(), ex.Message);
				}
				finally
				{
					_connections.Remove(this);
				}
				if (_transportAdapter != null)
				{
					// unregister remoting channel
                    var registeredChannel = ClientTransportAdapterManager.Instance.GetTransportAdapter(_transportAdapter.UniqueName);
					if (registeredChannel != null && registeredChannel == _transportAdapter)
                        ClientTransportAdapterManager.Instance.Unregister(_transportAdapter);

					// dispose remoting channel, if it's disposable
					var disposableChannel = _transportAdapter as IDisposable;
					if (disposableChannel != null)
						disposableChannel.Dispose();

					_transportAdapter = null;
				}				
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
		/// Occurs when disconnection is detected.
		/// </summary>
		public event EventHandler<DisconnectedEventArgs> Disconnected;

		/// <summary>
		/// Fires the Disconnected event.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		protected virtual void OnDisconnected(DisconnectedEventArgs e)
		{
			if (Disconnected != null)
				Disconnected(this, e);
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
			if (Reconnected != null)
				Reconnected(this, e);
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
			if (NewLogonNeeded != null)
			{
				NewLogonNeeded(this, e);
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
                    _transportAdapter.SendRequest(new RequestMessage() 
                    { 
                        RequestType=RequestType.SystemOperation,
                        MethodName = "ReceiveClientHeartbeat",
                        Address=_serverUrl,
                        ParameterValues=new object[] { _sessionID },
                        CallContext = PrepareCallContext(false)
                    });                    
                    //RemoteDispatcher.ReceiveClientHeartbeat(_sessionID);
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

        private bool SendExistSessionMessage(Guid sessionID)
        {
            var existSessionResponse = _transportAdapter.SendRequest(new RequestMessage()
            {
                RequestType = RequestType.SystemOperation,
                MethodName = "ExistSession",
                Address = _serverUrl,
                ParameterValues = new object[] { sessionID }
            });
            return (bool)existSessionResponse.ReturnValue;
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
			//if (!RemoteDispatcher.ExistSession(_sessionID))
            if (!SendExistSessionMessage(_sessionID))
			{
				Hashtable credentials = null;
				bool performNewLogon = true;

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
					//RemoteDispatcher.Logon(_sessionID, credentials);
                    SendLogonMessage(_sessionID, credentials);
					ReconnectRemoteEvents();

					//RemoteDispatcher.ReceiveClientHeartbeat(_sessionID);
                    SendReceiveClientHeartbeatMessage(_sessionID);
                    
					return true;
				}
			}
			else
			{
				//RemoteDispatcher.ReceiveClientHeartbeat(_sessionID);
                SendReceiveClientHeartbeatMessage(_sessionID);
				return true;
			}
			return false;
		}

        private void SendReceiveClientHeartbeatMessage(Guid sessionID)
        {
            _transportAdapter.SendRequest(new RequestMessage()
            {
                RequestType = RequestType.SystemOperation,
                Address = _serverUrl,
                MethodName = "ReceiveClientHeartbeat",
                ParameterValues = new object[] { sessionID }
            });
        }

		/// <summary>
		/// Reconnects to all remote events or delegates of any know proxy for this connection, after a server restart.
		/// <remarks>
		/// Caution! This method does not check, if the event handler registrations are truly lost (caused by a server restart).
		/// </remarks>
		/// </summary>
		private void ReconnectRemoteEvents()
		{
			foreach (var proxyRef in _proxies)
			{
				if (proxyRef.IsAlive)
				{
					var proxy = proxyRef.Target as ZyanProxy;
					proxy.ReconnectRemoteEvents();
				}
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
					//return RemoteDispatcher.ExistSession(_sessionID);
                    return SendExistSessionMessage(_sessionID);
				}
				catch
				{
					return false;
				}
			}
		}

		#endregion
	}
}
