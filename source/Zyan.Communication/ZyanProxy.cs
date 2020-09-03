using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using Zyan.Communication.Delegates;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Toolbox.Diagnostics;
using Zyan.InterLinq;

namespace Zyan.Communication
{
	/// <summary>
	/// Proxy to access a remote Zyan component.
	/// </summary>
	public class ZyanProxy : RealProxy
	{
		private Type _interfaceType = null;
		private IZyanDispatcher _remoteDispatcher = null;
		private List<DelegateCorrelationInfo> _delegateCorrelationSet = null;
		private bool _implicitTransactionTransfer = false;
		private Guid _sessionID;
		private string _componentHostName = string.Empty;
		private bool _autoLoginOnExpiredSession = false;
		private ZyanConnection _connection = null;
		private ActivationType _activationType = ActivationType.SingleCall;
		private string _uniqueName = string.Empty;
		private SynchronizationContext _synchronizationContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanProxy"/> class.
		/// </summary>
		/// <param name="uniqueName">Unique component name.</param>
		/// <param name="type">Component interface type.</param>
		/// <param name="connection"><see cref="ZyanConnection"/> instance.</param>
		/// <param name="implicitTransactionTransfer">Specifies whether transactions should be passed implicitly.</param>
		/// <param name="keepSynchronizationContext">Specifies whether callbacks and event handlers should use the original synchronization context.</param>
		/// <param name="sessionID">Session ID.</param>
		/// <param name="componentHostName">Name of the remote component host.</param>
		/// <param name="autoLoginOnExpiredSession">Specifies whether Zyan should login automatically with cached credentials after the session is expired.</param>
		/// <param name="activationType">Component activation type</param>
		public ZyanProxy(string uniqueName, Type type, ZyanConnection connection, bool implicitTransactionTransfer, bool keepSynchronizationContext, Guid sessionID, string componentHostName, bool autoLoginOnExpiredSession, ActivationType activationType)
			: base(type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (connection == null)
				throw new ArgumentNullException("connection");

			if (string.IsNullOrEmpty(uniqueName))
				_uniqueName = type.FullName;
			else
				_uniqueName = uniqueName;

			_sessionID = sessionID;
			_connection = connection;
			_componentHostName = componentHostName;
			_interfaceType = type;
			_activationType = activationType;
			_remoteDispatcher = _connection.RemoteDispatcher;
			_implicitTransactionTransfer = implicitTransactionTransfer;
			_autoLoginOnExpiredSession = autoLoginOnExpiredSession;
			_delegateCorrelationSet = new List<DelegateCorrelationInfo>();

			// capture synchronization context for callback execution
			if (keepSynchronizationContext)
			{
				_synchronizationContext = SynchronizationContext.Current;
			}
		}

		/// <summary>
		/// Gets the name of the remote Component Host.
		/// </summary>
		public string ComponentHostName
		{
			get { return _componentHostName; }
		}

		/// <summary>
		/// Invokes the method that is specified in the provided IMessage on the remote object that is represented by the current instance.
		/// </summary>
		/// <param name="message">Remoting <see cref="IMessage"/> that contains information about the method call.</param>
		/// <returns>The message returned by the invoked method, containing the return value and any out or ref parameters.</returns>
		public override IMessage Invoke(IMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			// intercept remote method call and/or invoke the remote method
			return InterceptAndInvoke((IMethodCallMessage)message, _connection.CallInterceptionEnabled);
		}

		/// <summary>
		/// Intercepts the method that is specified in the provided IMessage and/or invokes it on the remote object.
		/// </summary>
		/// <remarks>This method is called by <see cref="CallInterceptionData"/>.MakeRemoteCall() method.</remarks>
		/// <param name="methodCallMessage">Remoting <see cref="IMethodCallMessage"/> that contains information about the method call.</param>
		/// <param name="allowInterception">Specifies whether call interception is allowed.</param>
		/// <returns>The message returned by the invoked method, containing the return value and any out or ref parameters.</returns>
		private IMessage InterceptAndInvoke(IMethodCallMessage methodCallMessage, bool allowInterception)
		{
			try
			{
				var methodInfo = (MethodInfo)methodCallMessage.MethodBase;

				return
					HandleCallInterception(methodCallMessage, allowInterception) ??
					HandleLocalInvocation(methodCallMessage, methodInfo) ??
					HandleEventSubscription(methodCallMessage, methodInfo) ??
					HandleEventUnsubscription(methodCallMessage, methodInfo) ??
					HandleLinqQuery(methodCallMessage, methodInfo) ??
					HandleRemoteInvocation(methodCallMessage, methodInfo);
			}
			catch (Exception ex)
			{
				if (_connection.ErrorHandlingEnabled)
				{
					ZyanErrorEventArgs e = new ZyanErrorEventArgs()
					{
						Exception = ex,
						RemotingMessage = methodCallMessage,
						ServerComponentType = _interfaceType,
						RemoteMemberName = methodCallMessage.MethodName
					};

					_connection.OnError(e);

					switch (e.Action)
					{
						case ZyanErrorAction.ThrowException:
							throw;
						case ZyanErrorAction.Retry:
							return Invoke(methodCallMessage);
						case ZyanErrorAction.Ignore:
							return new ReturnMessage(null, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
					}
				}

				throw;
			}
		}

		/// <summary>
		/// Handles method call interception.
		/// </summary>
		/// <param name="methodCallMessage">Remoting <see cref="IMethodCallMessage"/> that contains information about the method call.</param>
		/// <param name="allowInterception">Specifies whether call interception is allowed.</param>
		/// <returns><see cref="ReturnMessage"/>, if the call is intercepted, otherwise, false.</returns>
		private ReturnMessage HandleCallInterception(IMethodCallMessage methodCallMessage, bool allowInterception)
		{
			if (!allowInterception || CallInterceptor.IsPaused)
				return null;

			var interceptor = _connection.CallInterceptors.FindMatchingInterceptor(_interfaceType, _uniqueName, methodCallMessage);
			if (interceptor != null && interceptor.OnInterception != null)
			{
				var interceptionData = new CallInterceptionData(_uniqueName, methodCallMessage.Args, HandleRemoteInvocation, methodCallMessage);
				interceptor.OnInterception(interceptionData);

				if (interceptionData.Intercepted)
					return new ReturnMessage(interceptionData.ReturnValue, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
			}

			// Remote call is not intercepted or interceptor doesn't exist
			return null;
		}

		/// <summary>
		/// Handles remote method invocation.
		/// </summary>
		/// <param name="methodCallMessage"><see cref="IMethodCallMessage"/> to process.</param>
		/// <param name="methodInfo"><see cref="MethodInfo"/> for the method being called.</param>
		/// <returns><see cref="ReturnMessage"/>, if the call is processed successfully, otherwise, false.</returns>
		private ReturnMessage HandleRemoteInvocation(IMethodCallMessage methodCallMessage, MethodInfo methodInfo)
		{
			_connection.PrepareCallContext(_implicitTransactionTransfer);
			var returnMessage = InvokeRemoteMethod(methodCallMessage);
			_connection.CheckRemoteSubscriptionCounter();
			return returnMessage;
		}

		/// <summary>
		/// Handles certain invocations locally for methods declared by System.Object class.
		/// </summary>
		/// <param name="methodCallMessage"><see cref="IMethodCallMessage"/> to process.</param>
		/// <param name="methodInfo"><see cref="MethodInfo"/> for the method being called.</param>
		/// <returns><see cref="ReturnMessage"/>, if the call is processed successfully, otherwise, false.</returns>
		private ReturnMessage HandleLocalInvocation(IMethodCallMessage methodCallMessage, MethodInfo methodInfo)
		{
			// only methods of type object are handled locally
			if (methodInfo.DeclaringType != typeof(object))
			{
				return null;
			}

			Func<object, ReturnMessage> GetResult =
				result => new ReturnMessage(result, null, 0, null, methodCallMessage);

			switch (methodInfo.Name)
			{
				case "GetType":
					return GetResult(_interfaceType);

				case "GetHashCode":
					var hashCode = 0xBadFace;
					hashCode ^= _connection.ServerUrl.GetHashCode();
					hashCode ^= _interfaceType.FullName.GetHashCode();
					return GetResult(hashCode);

				case "Equals":
					var falseResult = GetResult(false);

					// is other object also a transparent proxy?
					var other = methodCallMessage.Args[0];
					if (!RemotingServices.IsTransparentProxy(other))
						return falseResult;

					// is other object proxied by ZyanProxy?
					var proxy = RemotingServices.GetRealProxy(other) as ZyanProxy;
					if (proxy == null)
						return falseResult;

					// are properties the same?
					if (proxy._sessionID != _sessionID ||
						proxy._connection.ServerUrl != _connection.ServerUrl ||
						proxy._interfaceType != _interfaceType ||
						proxy._uniqueName != _uniqueName)
						return falseResult;

					return GetResult(true);

				case "ToString":
					var result = _connection.ServerUrl + "/" + _interfaceType.FullName;
					return GetResult(result);

				default:
					return null;
			}
		}

		private void OptionalAsync(bool sync, Action action)
		{
			if (sync)
			{
				action();
				return;
			}

			ThreadPool.QueueUserWorkItem(x =>
			{
				try
				{
					action();
				}
				catch (Exception ex) // SocketException, etc.
				{
					// ZyanConnection will detect and retry to subscribe/unsubscribe when connection is re-established
					Trace.WriteLine("Subscription failed due to an exception: {0}", ex);
				}
			});
		}

		/// <summary>
		/// Handles subscription to events.
		/// </summary>
		/// <param name="methodCallMessage"><see cref="IMethodCallMessage"/> to process.</param>
		/// <param name="methodInfo"><see cref="MethodInfo"/> for the method being called.</param>
		/// <returns><see cref="ReturnMessage"/>, if the call is processed successfully, otherwise, false.</returns>
		private ReturnMessage HandleEventSubscription(IMethodCallMessage methodCallMessage, MethodInfo methodInfo)
		{
			// Check for delegate parameters in properties and events
			if (methodInfo.ReturnType.Equals(typeof(void)) &&
				(methodCallMessage.MethodName.StartsWith("set_") || methodCallMessage.MethodName.StartsWith("add_")) &&
				methodCallMessage.InArgCount == 1 &&
				methodCallMessage.ArgCount == 1 &&
				methodCallMessage.Args[0] != null &&
				typeof(Delegate).IsAssignableFrom(methodCallMessage.Args[0].GetType()))
			{
				// Get client delegate
				var receiveMethodDelegate = methodCallMessage.GetArg(0) as Delegate;
				var eventFilter = default(IEventFilter);

				// Get event filter, if it is attached
				ExtractEventHandlerDetails(ref receiveMethodDelegate, ref eventFilter);

				// Trim "set_" or "add_" prefix
				string propertyName = methodCallMessage.MethodName.Substring(4);

				// Create delegate interceptor and correlation info
				var wiring = new DelegateInterceptor()
				{
					ClientDelegate = receiveMethodDelegate,
					SynchronizationContext = _synchronizationContext
				};

				var correlationInfo = new DelegateCorrelationInfo()
				{
					IsEvent = methodCallMessage.MethodName.StartsWith("add_"),
					DelegateMemberName = propertyName,
					ClientDelegateInterceptor = wiring,
					EventFilter = eventFilter
				};

				OptionalAsync(ZyanSettings.LegacyBlockingSubscriptions, () =>
					AddRemoteEventHandlers(new List<DelegateCorrelationInfo> { correlationInfo }));

				// Save delegate correlation info
				lock (_delegateCorrelationSet)
				{
					_delegateCorrelationSet.Add(correlationInfo);
				}

				return new ReturnMessage(null, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
			}

			// This method doesn't represent event subscription
			return null;
		}

		/// <summary>
		/// Handles unsubscription.
		/// </summary>
		/// <param name="methodCallMessage"><see cref="IMethodCallMessage"/> to process.</param>
		/// <param name="methodInfo"><see cref="MethodInfo"/> for the method being called.</param>
		/// <returns><see cref="ReturnMessage"/>, if the call is processed successfully, otherwise, false.</returns>
		private ReturnMessage HandleEventUnsubscription(IMethodCallMessage methodCallMessage, MethodInfo methodInfo)
		{
			if (methodInfo.ReturnType.Equals(typeof(void)) &&
				methodCallMessage.MethodName.StartsWith("remove_") &&
				methodCallMessage.InArgCount == 1 &&
				methodCallMessage.ArgCount == 1 &&
				methodCallMessage.Args[0] != null &&
				typeof(Delegate).IsAssignableFrom(methodCallMessage.Args[0].GetType()))
			{
				string propertyName = methodCallMessage.MethodName.Substring(7);
				var inputMessage = methodCallMessage.GetArg(0) as Delegate;
				var eventFilter = default(IEventFilter);

				// Detach event filter, if it is attached
				ExtractEventHandlerDetails(ref inputMessage, ref eventFilter);

				if (_delegateCorrelationSet.Count > 0)
				{
					// copy delegates
					IEnumerable<DelegateCorrelationInfo> correlationSet;
					lock (_delegateCorrelationSet)
					{
						correlationSet = _delegateCorrelationSet.ToArray();
					}

					var found = (
						from correlationInfo in correlationSet
						where correlationInfo.DelegateMemberName.Equals(propertyName) && correlationInfo.ClientDelegateInterceptor.ClientDelegate.Equals(inputMessage)
						select correlationInfo).FirstOrDefault();

					if (found != null)
					{
						OptionalAsync(ZyanSettings.LegacyBlockingSubscriptions, () =>
							RemoveRemoteEventHandlers(new List<DelegateCorrelationInfo> { found }));

						// Remove delegate correlation info
						lock (_delegateCorrelationSet)
						{
							_delegateCorrelationSet.Remove(found);
							found.Dispose();
						}
					}
				}

				return new ReturnMessage(null, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
			}

			// This method doesn't represent event unsubscription
			return null;
		}

		/// <summary>
		/// Handles LINQ queries.
		/// </summary>
		/// <param name="methodCallMessage"><see cref="IMethodCallMessage"/> to process.</param>
		/// <param name="methodInfo"><see cref="MethodInfo"/> for the method being called.</param>
		/// <returns><see cref="ReturnMessage"/>, if the call is processed successfully, otherwise, false.</returns>
		private ReturnMessage HandleLinqQuery(IMethodCallMessage methodCallMessage, MethodInfo methodInfo)
		{
			if (methodInfo.GetParameters().Length == 0 &&
				methodInfo.GetGenericArguments().Length == 1 &&
				(typeof(IEnumerable).IsAssignableFrom(methodInfo.ReturnType) || typeof(IQueryable).IsAssignableFrom(methodInfo.ReturnType)))
			{
				var elementType = methodInfo.GetGenericArguments().First();
				var serverHandlerName = ZyanMethodQueryHandler.GetMethodQueryHandlerName(_uniqueName, methodInfo);
				var clientHandler = new ZyanClientQueryHandler(_connection, serverHandlerName);
				var returnValue = clientHandler.Get(elementType);
				return new ReturnMessage(returnValue, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
			}

			// This method call doesn't represent a LINQ query
			return null;
		}

		/// <summary>
		/// Adds remote handlers to server events or delegate properties.
		/// </summary>
		/// <param name="correlationSet">Correlation data.</param>
		private void AddRemoteEventHandlers(List<DelegateCorrelationInfo> correlationSet)
		{
			if (correlationSet == null)
				throw new ArgumentNullException("correlationSet");

			var count = correlationSet.Count;
			if (count > 0)
			{
				_connection.TrackRemoteSubscriptions(correlationSet);
				lock (_connection.RemoteEventsLock)
				{
					_connection.PrepareCallContext(false);
					_connection.RemoteDispatcher.AddEventHandlers(_interfaceType.FullName, correlationSet, _uniqueName);
				}
			}
		}

		/// <summary>
		/// Removes remote handlers from server events or delegate properties.
		/// </summary>
		/// <param name="correlationSet">Correlation data.</param>
		private void RemoveRemoteEventHandlers(List<DelegateCorrelationInfo> correlationSet)
		{
			if (correlationSet == null)
				throw new ArgumentNullException("correlationSet");

			var count = correlationSet.Count;
			if (count > 0)
			{
				lock (_connection.RemoteEventsLock)
				{
					_connection.UntrackRemoteSubscriptions(correlationSet);
					_connection.PrepareCallContext(false);
					_connection.RemoteDispatcher.RemoveEventHandlers(_interfaceType.FullName, correlationSet, _uniqueName);
				}
			}
		}

		/// <summary>
		/// Gets the delegate correlations between server events and client handlers.
		/// </summary>
		internal List<DelegateCorrelationInfo> DelegateCorrelationSet
		{
			get
			{
				lock (_delegateCorrelationSet)
				{
					return _delegateCorrelationSet.ToList();
				}
			}
		}

		/// <summary>
		/// Returns all active subscriptions to the remote events.
		/// </summary>
		internal ComponentDelegateCorrelationSet GetActiveSubscriptions()
		{
			lock (_delegateCorrelationSet)
			{
				// copy delegates
				var correlationSet = _delegateCorrelationSet.ToArray();
				return new ComponentDelegateCorrelationSet
				{
					InterfaceName = _interfaceType.FullName,
					DelegateCorrelationSet = correlationSet,
					UniqueName = _uniqueName,
				};
			}
		}

		/// <summary>
		/// Extracts the event handler details such as event filter.
		/// </summary>
		/// <param name="eventHandler">The event handler delegate.</param>
		/// <param name="eventFilter">The event filter.</param>
		private void ExtractEventHandlerDetails(ref Delegate eventHandler, ref IEventFilter eventFilter)
		{
			if (eventHandler == null || eventHandler.Method == null)
			{
				return;
			}

			// handle attached event filters, if any
			if (eventHandler.Target is IFilteredEventHandler)
			{
				var filtered = eventHandler.Target as IFilteredEventHandler;
				eventHandler = filtered.EventHandler;
				eventFilter = filtered.EventFilter;
			}

			// handle special case: session-bound events
			var parameters = eventHandler.Method.GetParameters();
			if (parameters.Length == 2 && typeof(SessionEventArgs).IsAssignableFrom(parameters[1].ParameterType))
			{
				// if SessionEventFilter is already attached, do not create a new filter
				if (eventFilter == null || !eventFilter.Contains<SessionEventFilter>())
				{
					var sessionFilter = new SessionEventFilter(_sessionID);
					eventFilter = sessionFilter.Combine(eventFilter);
				}
			}
		}

		/// <summary>
		/// Invokes a remote method.
		/// </summary>
		/// <param name="methodCallMessage">Remoting message.</param>
		/// <returns>Remoting response message</returns>
		private ReturnMessage InvokeRemoteMethod(IMethodCallMessage methodCallMessage)
		{
			Guid trackingID = Guid.NewGuid();

			try
			{
				object returnValue = null;

				List<DelegateCorrelationInfo> correlationSet = null;

				if (_activationType == ActivationType.SingleCall)
					correlationSet = _delegateCorrelationSet;

				BeforeInvokeEventArgs cancelArgs = new BeforeInvokeEventArgs()
				{
					TrackingID = trackingID,
					InterfaceName = _interfaceType.FullName,
					DelegateCorrelationSet = correlationSet,
					MethodName = methodCallMessage.MethodName,
					Arguments = methodCallMessage.Args,
					Cancel = false
				};
				_connection.OnBeforeInvoke(cancelArgs);

				if (cancelArgs.Cancel)
				{
					if (cancelArgs.CancelException == null)
						cancelArgs.CancelException = new InvokeCanceledException();

					_connection.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = cancelArgs.CancelException });

					throw cancelArgs.CancelException.PreserveStackTrace();
				}

				// Prepare generic method arguments
				Type[] genericArgs = null;
				if (methodCallMessage.MethodBase.IsGenericMethod)
				{
					genericArgs = methodCallMessage.MethodBase.GetGenericArguments();
				}

				var paramDefs = methodCallMessage.MethodBase.GetParameters();
				var paramTypes = paramDefs.Select(p => p.ParameterType).ToArray();

				try
				{
					object[] checkedArgs = InterceptDelegateParameters(methodCallMessage);

					returnValue = _remoteDispatcher.Invoke(trackingID, _uniqueName, correlationSet, methodCallMessage.MethodName, genericArgs, paramTypes, checkedArgs);

					AfterInvokeEventArgs afterInvokeArgs = new AfterInvokeEventArgs()
					{
						TrackingID = trackingID,
						InterfaceName = _interfaceType.FullName,
						DelegateCorrelationSet = correlationSet,
						MethodName = methodCallMessage.MethodName,
						Arguments = methodCallMessage.Args,
						ReturnValue = returnValue
					};

					_connection.OnAfterInvoke(afterInvokeArgs);
				}
				catch (InvalidSessionException)
				{
					if (_autoLoginOnExpiredSession)
					{
						if (_connection.Reconnect())
							returnValue = _remoteDispatcher.Invoke(trackingID, _uniqueName, correlationSet, methodCallMessage.MethodName, genericArgs, paramTypes, methodCallMessage.Args);
						else
							throw;
					}
					else
					{
						throw;
					}
				}

				var container = returnValue as CustomSerializationContainer;
				if (container != null)
				{
					var serializationHandler = _connection.SerializationHandling[container.HandledType];
					if (serializationHandler == null)
						throw new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_SerializationHandlerNotFound, container.HandledType.FullName));

					returnValue = serializationHandler.Deserialize(container.DataType, container.Data);
				}

				return new ReturnMessage(returnValue, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
			}
			catch (Exception ex)
			{
				if (_connection.ErrorHandlingEnabled)
					throw;
				else
					return new ReturnMessage(ex, methodCallMessage);
			}
		}

		/// <summary>
		/// Replaces delegate parameters with call interceptors.
		/// </summary>
		/// <param name="message">Remoting message</param>
		/// <returns>Parameters</returns>
		private object[] InterceptDelegateParameters(IMethodCallMessage message)
		{
			object[] result = new object[message.ArgCount];

			ParameterInfo[] paramDefs = message.MethodBase.GetParameters();

			for (int i = 0; i < message.ArgCount; i++)
			{
				object arg = message.Args[i];

				if (arg != null && typeof(Delegate).IsAssignableFrom(arg.GetType()))
				{
					DelegateInterceptor interceptor = new DelegateInterceptor()
					{
						ClientDelegate = arg
					};
					result[i] = interceptor;
				}
				else
				{
					Type argType = paramDefs[i].ParameterType;

					Type handledType;
					ISerializationHandler handler;
					_connection.SerializationHandling.FindMatchingSerializationHandler(argType, out handledType, out handler);

					if (handler != null)
					{
						byte[] raw = handler.Serialize(arg);
						result[i] = new CustomSerializationContainer(handledType, argType, raw);
					}
					else
						// 1:1
						result[i] = arg;
				}
			}
			return result;
		}

		/// <summary>
		/// Removes all remote event handlers.
		/// </summary>
		internal void RemoveAllRemoteEventHandlers()
		{
			if (_delegateCorrelationSet == null)
				return;

			if (_delegateCorrelationSet.Count > 0)
			{
				// copy delegates
				List<DelegateCorrelationInfo> correlationSet;
				lock (_delegateCorrelationSet)
				{
					correlationSet = _delegateCorrelationSet.ToList();
				}

				RemoveRemoteEventHandlers(correlationSet);
				lock (_delegateCorrelationSet)
				{
					_delegateCorrelationSet.Clear();
				}
			}
		}
	}
}

