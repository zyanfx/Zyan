using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using Castle.DynamicProxy;
using CoreRemoting.ClassicRemotingApi;
using Zyan.Communication.Delegates;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Toolbox.Diagnostics;
using Zyan.InterLinq;

namespace Zyan.Communication
{
	/// <summary>
	/// Proxy to access a remote Zyan component.
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class ZyanProxy : IInterceptor
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
		public ZyanProxy(
			string uniqueName, 
			Type type, 
			ZyanConnection connection, 
			bool implicitTransactionTransfer, 
			bool keepSynchronizationContext, 
			Guid sessionID, 
			string componentHostName, 
			bool autoLoginOnExpiredSession, 
			ActivationType activationType)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

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
		/// Intercepts a call on this proxy object and invokes the member on the remote service.
		/// </summary>
		/// <param name="invocation">Intercepted invocation details</param>
		public void Intercept(IInvocation invocation)
		{
			if (invocation == null)
				throw new ArgumentNullException(nameof(invocation));
			
			InterceptAndInvoke(invocation, _connection.CallInterceptionEnabled);
		}

		/// <summary>
		/// Intercepts the method that is specified in the provided IMessage and/or invokes it on the remote object.
		/// </summary>
		/// <param name="invocation">Intercepted invocation details</param>
		/// <param name="allowInterception">Specifies whether call interception is allowed.</param>
		private void InterceptAndInvoke(IInvocation invocation, bool allowInterception)
		{
			try
			{
				var methodInfo = invocation.Method;

				//TODO: Migrate call interception
				// if (HandleCallInterception(invocation, allowInterception))
				// 	return;
				
				if (HandleLocalInvocation(invocation))
					return;
				
				if (HandleEventSubscription(invocation))
					return;
				
				if (HandleEventUnsubscription(invocation))
					return;
				
				if (HandleLinqQuery(invocation))
					return;

				if (HandleRemoteInvocation(invocation))
					return;
			}
			catch (Exception ex)
			{
				if (_connection.ErrorHandlingEnabled)
				{
					ZyanErrorEventArgs e = new ZyanErrorEventArgs()
					{
						Exception = ex,
						ServerComponentType = _interfaceType,
						RemoteMemberName = invocation.Method.Name
					};

					_connection.OnError(e);

					switch (e.Action)
					{
						case ZyanErrorAction.ThrowException:
							throw;
						case ZyanErrorAction.Retry:
							InterceptAndInvoke(invocation, allowInterception);
							break;
						case ZyanErrorAction.Ignore:
							invocation.ReturnValue = null;
							break;
					}
				}

				throw;
			}
		}

		//TODO: Migrate call interception.
		// /// <summary>
		// /// Handles method call interception.
		// /// </summary>
		// /// <param name="invocation">Intercepted invocation details</param>
		// /// <param name="allowInterception">Specifies whether call interception is allowed.</param>
		// /// <returns>True, if handled, otherwise false</returns>
		// private bool HandleCallInterception(IInvocation invocation, bool allowInterception)
		// {
		// 	if (!allowInterception || CallInterceptor.IsPaused)
		// 		return null;
		//
		// 	var interceptor = _connection.CallInterceptors.FindMatchingInterceptor(_interfaceType, _uniqueName, methodCallMessage);
		// 	if (interceptor != null && interceptor.OnInterception != null)
		// 	{
		// 		var interceptionData = new CallInterceptionData(_uniqueName, methodCallMessage.Args, HandleRemoteInvocation, methodCallMessage);
		// 		interceptor.OnInterception(interceptionData);
		//
		// 		if (interceptionData.Intercepted)
		// 			return new ReturnMessage(interceptionData.ReturnValue, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
		// 	}
		//
		// 	// Remote call is not intercepted or interceptor doesn't exist
		// 	return null;
		// }

		/// <summary>
		/// Handles remote method invocation.
		/// </summary>
		/// <param name="invocation">Intercepted invocation details</param>
		/// <returns>True, if handled, otherwise false</returns>
		private bool HandleRemoteInvocation(IInvocation invocation)
		{
			_connection.PrepareCallContext(_implicitTransactionTransfer);
			var handled = InvokeRemoteMethod(invocation);
			_connection.CheckRemoteSubscriptionCounter();
			return handled;
		}
		
		/// <summary>
		/// Handles certain invocations locally for methods declared by System.Object class.
		/// </summary>
		/// <param name="invocation">Intercepted invocation details</param>
		/// <returns>True, if handled, otherwise false</returns>
		private bool HandleLocalInvocation(IInvocation invocation)
		{
			var methodInfo = invocation.Method;
			
			// only methods of type object are handled locally
			if (methodInfo.DeclaringType != typeof(object))
				return false;

			switch (methodInfo.Name)
			{
				case "GetType":
					invocation.ReturnValue = _interfaceType;
					return true;

				case "GetHashCode":
					var hashCode = 0xBadFace;
					hashCode ^= _connection.ServerUrl.GetHashCode();
					hashCode ^= _interfaceType.FullName.GetHashCode();
					invocation.ReturnValue = hashCode;
					return true;

				case "Equals":
					// is other object also a proxy?
					var other = invocation.Arguments[0];
					if (!RemotingServices.IsTransparentProxy(other))
					{
						invocation.ReturnValue = false;	
						return true;
					}

					// is other object proxied by ZyanProxy?
					var proxy = other as ZyanProxy;
					if (proxy == null)
					{
						invocation.ReturnValue = false;	
						return true;
					}

					// are properties the same?
					if (proxy._sessionID != _sessionID ||
						proxy._connection.ServerUrl != _connection.ServerUrl ||
						proxy._interfaceType != _interfaceType ||
						proxy._uniqueName != _uniqueName)
					{
						invocation.ReturnValue = false;	
						return true;
					}

					invocation.ReturnValue = true;	
					return true;

				case "ToString":
					var result = _connection.ServerUrl + "/" + _interfaceType.FullName;
					invocation.ReturnValue = false;	
					return true;

				default:
					return false;
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
		/// <param name="invocation">Intercepted invocation details</param>
		/// <returns>True, if handled, otherwise false</returns>
		private bool HandleEventSubscription(IInvocation invocation)
		{
			var methodInfo = invocation.Method;
			var paramters = methodInfo.GetParameters();
			var firstParameterValue = invocation.Arguments.Length > 0 ? invocation.Arguments[0] : null;
			var receiveMethodDelegate = firstParameterValue as Delegate;
			
			// Quit, if invoked member is not a delegate
			if (methodInfo.ReturnType != typeof(void) ||
			    (!methodInfo.Name.StartsWith("set_") && !methodInfo.Name.StartsWith("add_")) || 
			    paramters.Length != 1 ||
			    !paramters[0].IsIn || 
			    firstParameterValue == null ||
			    receiveMethodDelegate == null) 
				return false;
			
			// Get client delegate
			var eventFilter = default(IEventFilter);

			// Get event filter, if it is attached
			ExtractEventHandlerDetails(ref receiveMethodDelegate, ref eventFilter);

			// Trim "set_" or "add_" prefix
			string propertyName = methodInfo.Name.Substring(4);

			// Create delegate interceptor and correlation info
			var wiring = new DelegateInterceptor()
			{
				ClientDelegate = receiveMethodDelegate,
				SynchronizationContext = _synchronizationContext
			};

			var correlationInfo = new DelegateCorrelationInfo()
			{
				IsEvent = methodInfo.Name.StartsWith("add_"),
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

			return true;
			
			// This method doesn't represent event subscription
		}

		/// <summary>
		/// Handles unsubscription.
		/// </summary>
		/// <param name="invocation">Intercepted invocation details</param>
		/// <returns>True, if handled, otherwise false</returns>
		private bool HandleEventUnsubscription(IInvocation invocation)
		{
			var methodInfo = invocation.Method;
			var paramters = methodInfo.GetParameters();
			var firstParameterValue = invocation.Arguments.Length > 0 ? invocation.Arguments[0] : null;
			var eventHandler = firstParameterValue as Delegate;
			
			// Quit, if invoked member is not a delegate
			if (methodInfo.ReturnType != typeof(void) ||
			    !methodInfo.Name.StartsWith("remove_") || 
			    paramters.Length != 1 ||
			    !paramters[0].IsIn || 
			    firstParameterValue == null ||
			    eventHandler == null) 
				return false;
			
			string propertyName = methodInfo.Name.Substring(7);
			var eventFilter = default(IEventFilter);

			// Detach event filter, if it is attached
			ExtractEventHandlerDetails(ref eventHandler, ref eventFilter);

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
					where correlationInfo.DelegateMemberName.Equals(propertyName) && 
					      correlationInfo.ClientDelegateInterceptor.ClientDelegate.Equals(eventHandler)
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

			return true;
		}

		/// <summary>
		/// Handles LINQ queries.
		/// </summary>
		/// <param name="invocation">Intercepted invocation details</param>
		/// <returns>True, if handled, otherwise false</returns>
		private bool HandleLinqQuery(IInvocation invocation)
		{
			var methodInfo = invocation.Method;
			
			if (methodInfo.GetParameters().Length == 0 &&
				methodInfo.GetGenericArguments().Length == 1 &&
				(typeof(IEnumerable).IsAssignableFrom(methodInfo.ReturnType) || typeof(IQueryable).IsAssignableFrom(methodInfo.ReturnType)))
			{
				var elementType = methodInfo.GetGenericArguments().First();
				var serverHandlerName = ZyanMethodQueryHandler.GetMethodQueryHandlerName(_uniqueName, methodInfo);
				var clientHandler = new ZyanClientQueryHandler(_connection, serverHandlerName);
				invocation.ReturnValue = clientHandler.Get(elementType);
				return true;
			}

			return false;
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
		/// <param name="invocation">Intercepted invocation details</param>
		/// <returns>True, if handled, otherwise false</returns>
		private bool InvokeRemoteMethod(IInvocation invocation)
		{
			var trackingID = Guid.NewGuid();
			var methodInfo = invocation.Method;
			
			object returnValue = null;

			List<DelegateCorrelationInfo> correlationSet = null;

			if (_activationType == ActivationType.SingleCall)
				correlationSet = _delegateCorrelationSet;

			BeforeInvokeEventArgs cancelArgs = new BeforeInvokeEventArgs()
			{
				TrackingID = trackingID,
				InterfaceName = _interfaceType.FullName,
				DelegateCorrelationSet = correlationSet,
				MethodName = methodInfo.Name,
				Arguments = invocation.Arguments,
				Cancel = false
			};
			_connection.OnBeforeInvoke(cancelArgs);

			if (cancelArgs.Cancel)
			{
				cancelArgs.CancelException ??= new InvokeCanceledException();

				_connection.OnInvokeCanceled(
					new InvokeCanceledEventArgs()
					{
						TrackingID = trackingID, 
						CancelException = cancelArgs.CancelException
					});

				throw cancelArgs.CancelException.PreserveStackTrace();
			}

			// Prepare generic method arguments
			string[] genericArgs = null;
			if (methodInfo.IsGenericMethod)
			{
				genericArgs =
					methodInfo.GetGenericArguments()
						.Select(t => t.FullName + "," + t.Assembly.GetName().Name)
						.ToArray();
			}

			var paramDefs = methodInfo.GetParameters();
			var paramTypes = 
				paramDefs.Select(p => 
					p.ParameterType.FullName + "," + p.ParameterType.Assembly.GetName().Name).ToArray();

			try
			{
				object[] checkedArgs = InterceptDelegateParameters(invocation);

				returnValue = 
					_remoteDispatcher.Invoke(
						trackingID, 
						_uniqueName, 
						correlationSet, 
						methodInfo.Name, 
						genericArgs, 
						paramTypes, 
						checkedArgs);

				AfterInvokeEventArgs afterInvokeArgs = new AfterInvokeEventArgs()
				{
					TrackingID = trackingID,
					InterfaceName = _interfaceType.FullName,
					DelegateCorrelationSet = correlationSet,
					MethodName = methodInfo.Name,
					Arguments = invocation.Arguments,
					ReturnValue = returnValue
				};

				_connection.OnAfterInvoke(afterInvokeArgs);
			}
			catch (InvalidSessionException)
			{
				if (_autoLoginOnExpiredSession)
				{
					if (_connection.Reconnect())
						returnValue = 
							_remoteDispatcher.Invoke(
								trackingID, 
								_uniqueName, 
								correlationSet, 
								methodInfo.Name, 
								genericArgs, 
								paramTypes, 
								invocation.Arguments);
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

			invocation.ReturnValue = returnValue;
			
			return true;
		}

		/// <summary>
		/// Replaces delegate parameters with call interceptors.
		/// </summary>
		/// <param name="invocation">Invocation details</param>
		/// <returns>Parameters</returns>
		private object[] InterceptDelegateParameters(IInvocation invocation)
		{
			object[] result = new object[invocation.Arguments.Length];

			ParameterInfo[] paramDefs = invocation.Method.GetParameters();

			for (int i = 0; i < invocation.Arguments.Length; i++)
			{
				object arg = invocation.Arguments[i];

				if (arg is Delegate)
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

					_connection.SerializationHandling
						.FindMatchingSerializationHandler(argType, out var handledType, out var handler);

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

