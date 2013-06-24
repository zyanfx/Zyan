using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.Transactions;
using Zyan.Communication.Delegates;
using Zyan.Communication.Notification;
using Zyan.Communication.Security;
using Zyan.Communication.SessionMgmt;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication
{
	/// <summary>
	/// Central dispatch component for RPC requests.
	/// </summary>
	public class ZyanDispatcher : MarshalByRefObject, IZyanDispatcher
	{
		#region Construction

		/// <summary>
		/// Creates a new instance of the ZyanDispatcher class.
		/// </summary>
		/// <param name="host">Component host</param>
		public ZyanDispatcher(ZyanComponentHost host)
		{
			if (host == null)
				throw new ArgumentNullException("host");

			_host = host;
		}

		#endregion

		#region Method invocation

		// Component Host this dispatcher is dispatching for
		private ZyanComponentHost _host = null;

		/// <summary>
		/// Creates wires between client component and server component.
		/// </summary>
		/// <param name="type">Implementation type of the server component.</param>
		/// <param name="eventStub"><see cref="EventStub"/> with cached subscriptions.</param>
		/// <param name="delegateCorrelationSet">Correlation set (say how to wire)</param>
		/// <param name="wiringList">Collection of built wires</param>
		private void CreateClientServerWires(Type type, EventStub eventStub, List<DelegateCorrelationInfo> delegateCorrelationSet, Dictionary<Guid, Delegate> wiringList)
		{
			if (delegateCorrelationSet == null)
				return;

			foreach (var correlationInfo in delegateCorrelationSet)
			{
				if (wiringList.ContainsKey(correlationInfo.CorrelationID))
					continue;

				var currentSession = ServerSession.CurrentSession;
				if (currentSession == null)
					throw new InvalidSessionException(string.Format(LanguageResource.InvalidSessionException_SessionIDInvalid, "(null)"));

				var dynamicWire = DynamicWireFactory.CreateDynamicWire(type, correlationInfo.DelegateMemberName, correlationInfo.IsEvent);
				dynamicWire.Interceptor = correlationInfo.ClientDelegateInterceptor;

				if (correlationInfo.IsEvent)
				{
					var dynamicEventWire = (DynamicEventWireBase)dynamicWire;
					dynamicEventWire.EventFilter = correlationInfo.EventFilter;

					// add session validation handler and unsubscription callback
					var sessionId = currentSession.SessionID;
					var sessionManager = _host.SessionManager;
					dynamicEventWire.ValidateSession = () => sessionManager.ExistSession(sessionId);
					dynamicEventWire.CancelSubscription = ex =>
					{
						eventStub.RemoveHandler(correlationInfo.DelegateMemberName, dynamicEventWire.InDelegate);
						wiringList.Remove(correlationInfo.CorrelationID);
						currentSession.DecrementRemoteSubscriptionCounter();
						_host.OnSubscriptionCanceled(new SubscriptionEventArgs
						{
							ComponentType = type,
							DelegateMemberName = correlationInfo.DelegateMemberName,
							CorrelationID = correlationInfo.CorrelationID,
							Exception = ex
						});
					};

					eventStub.AddHandler(correlationInfo.DelegateMemberName, dynamicEventWire.InDelegate);
					wiringList.Add(correlationInfo.CorrelationID, dynamicEventWire.InDelegate);
				}
				else
				{
					eventStub.AddHandler(correlationInfo.DelegateMemberName, dynamicWire.InDelegate);
					wiringList.Add(correlationInfo.CorrelationID, dynamicWire.InDelegate);
				}

				currentSession.IncrementRemoteSubscriptionCounter();
				_host.OnSubscriptionAdded(new SubscriptionEventArgs
				{
					ComponentType = type,
					DelegateMemberName = correlationInfo.DelegateMemberName,
					CorrelationID = correlationInfo.CorrelationID,
				});
			}
		}

		/// <summary>
		/// Removes wires between server and client components (as defined in correlation set).
		/// </summary>
		/// <param name="type">Type of the server component</param>
		/// <param name="eventStub"><see cref="EventStub"/> with cached subscriptions.</param>
		/// <param name="delegateCorrelationSet">Correlation set with wiring information</param>
		/// <param name="wiringList">List with known wirings</param>
		private void RemoveClientServerWires(Type type, EventStub eventStub, List<DelegateCorrelationInfo> delegateCorrelationSet, Dictionary<Guid, Delegate> wiringList)
		{
			if (delegateCorrelationSet == null)
				return;

			var currentSession = ServerSession.CurrentSession;
			foreach (var correlationInfo in delegateCorrelationSet)
			{
				if (wiringList.ContainsKey(correlationInfo.CorrelationID))
				{
					var dynamicWireDelegate = wiringList[correlationInfo.CorrelationID];
					eventStub.RemoveHandler(correlationInfo.DelegateMemberName, dynamicWireDelegate);
					wiringList.Remove(correlationInfo.CorrelationID);
					if (currentSession != null)
					{
						currentSession.DecrementRemoteSubscriptionCounter();
					}

					_host.OnSubscriptionRemoved(new SubscriptionEventArgs
					{
						ComponentType = type,
						DelegateMemberName = correlationInfo.DelegateMemberName,
						CorrelationID = correlationInfo.CorrelationID,
					});
				}
			}
		}

		/// <summary>
		/// Processes BeforeInvoke event subscriptions (if there any).
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void ProcessBeforeInvoke(InvocationDetails details)
		{
			if (_host.HasBeforeInvokeSubscriptions())
			{
				BeforeInvokeEventArgs cancelArgs = new BeforeInvokeEventArgs()
				{
					TrackingID = details.TrackingID,
					InterfaceName = details.InterfaceName,
					DelegateCorrelationSet = details.DelegateCorrelationSet,
					MethodName = details.MethodName,
					Arguments = details.Args,
					Cancel = false
				};
				_host.OnBeforeInvoke(cancelArgs);

				if (cancelArgs.Cancel)
				{
					if (cancelArgs.CancelException == null)
						cancelArgs.CancelException = new InvokeCanceledException();

					throw cancelArgs.CancelException.PreserveStackTrace();
				}

				details.InterfaceName = cancelArgs.InterfaceName;
				details.DelegateCorrelationSet = cancelArgs.DelegateCorrelationSet;
				details.MethodName = cancelArgs.MethodName;
				details.Args = cancelArgs.Arguments;
			}
		}

		/// <summary>
		/// Processes AfterInvoke event subscriptions (if there any).
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void ProcessAfterInvoke(InvocationDetails details)
		{
			if (_host.HasAfterInvokeSubscriptions())
			{
				AfterInvokeEventArgs afterInvokeArgs = new AfterInvokeEventArgs()
				{
					TrackingID = details.TrackingID,
					InterfaceName = details.InterfaceName,
					DelegateCorrelationSet = details.DelegateCorrelationSet,
					MethodName = details.MethodName,
					Arguments = details.Args,
					ReturnValue = details.ReturnValue
				};
				_host.OnAfterInvoke(afterInvokeArgs);
			}
		}

		/// <summary>
		/// Gets the IP Address of the calling client from CallContext.
		/// </summary>
		/// <returns></returns>
		private IPAddress GetCallingClientIPAddress()
		{
			return CallContext.GetData("Zyan_ClientAddress") as IPAddress;
		}

		/// <summary>
		/// Puts the IP Address of the calling client to the current Server Session.
		/// </summary>
		private void PutClientAddressToCurrentSession()
		{
			if (ServerSession.CurrentSession == null)
				return;

			IPAddress clientAddress = GetCallingClientIPAddress();

			if (clientAddress != null)
				ServerSession.CurrentSession.ClientAddress = clientAddress.ToString();
			else
				ServerSession.CurrentSession.ClientAddress = string.Empty;
		}

		/// <summary>
		/// Checks if the provided interface name belongs to a registered component.
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_CheckInterfaceName(InvocationDetails details)
		{
			// look up the component registration info
			if (!_host.ComponentRegistry.ContainsKey(details.InterfaceName))
				throw new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_CannotFindComponentForInterface, details.InterfaceName));
		}

		/// <summary>
		/// Loads data from the logical call context.
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_LoadCallContextData(InvocationDetails details)
		{
			// check for logical context data
			details.CallContextData = CallContext.GetData("__ZyanContextData_" + _host.Name) as LogicalCallContextData;
			if (details.CallContextData == null)
			{
				throw new SecurityException(LanguageResource.SecurityException_ContextInfoMissing);
			}
		}

		/// <summary>
		/// Sets the session for the current worker thread.
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_SetSession(InvocationDetails details)
		{
			// validate session
			var sessionID = details.CallContextData.Store.ContainsKey("sessionid") ? (Guid)details.CallContextData.Store["sessionid"] : Guid.Empty;
			if (!_host.SessionManager.ExistSession(sessionID))
			{
				throw new InvalidSessionException(string.Format(LanguageResource.InvalidSessionException_SessionIDInvalid, sessionID.ToString()));
			}

			// set current session
			details.Session = _host.SessionManager.GetSessionBySessionID(sessionID);
			details.Session.Timestamp = DateTime.Now;
			_host.SessionManager.SetCurrentSession(details.Session);
			PutClientAddressToCurrentSession();
		}

		/// <summary>
		/// Sets a transaction for the current worker thread, if provided.
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_SetTransaction(InvocationDetails details)
		{
			// transfer implicit transaction
			var transaction = details.CallContextData.Store.ContainsKey("transaction") ? (Transaction)details.CallContextData.Store["transaction"] : null;
			if (transaction != null)
				details.Scope = new TransactionScope(transaction);
		}

		/// <summary>
		/// Sets the remote subscription counter.
		/// </summary>
		/// <param name="details">The details.</param>
		private void Invoke_SetRemoteSubscriptionCounter(InvocationDetails details)
		{
			if (details.CallContextData.Store != null && ServerSession.CurrentSession != null)
			{
				details.CallContextData.Store["subscriptions"] = ServerSession.CurrentSession.RemoteSubscriptionCounter;
			}
		}

		/// <summary>
		/// Fires the InvokeCanceled event.
		/// </summary>
		/// <param name="details">Invocation details</param>
		/// <param name="ex">Exception</param>
		private void Invoke_FireInvokeCanceledEvent(InvocationDetails details, Exception ex)
		{
			details.ExceptionThrown = true;

			var args = new InvokeCanceledEventArgs
			{
				TrackingID = details.TrackingID,
				CancelException = ex
			};

			_host.OnInvokeCanceled(args);

			throw args.CancelException.PreserveStackTrace() ?? new InvokeCanceledException();
		}

		/// <summary>
		/// Fires the InvokeRejected event.
		/// </summary>
		/// <param name="details">Invocation details</param>
		/// <param name="ex">Exception</param>
		private void Invoke_FireInvokeRejectedEvent(InvocationDetails details, Exception ex)
		{
			details.ExceptionThrown = true;

			var args = new InvokeCanceledEventArgs
			{
				TrackingID = details.TrackingID,
				CancelException = ex
			};

			_host.OnInvokeRejected(args);

			throw args.CancelException.PreserveStackTrace() ?? new InvokeCanceledException();
		}

		/// <summary>
		/// Converts method arguments, if needed.
		/// <remarks>
		/// Conversion is needed when Types configured for custom serialization or arguments are delegates.
		/// </remarks>
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_ConvertMethodArguments(InvocationDetails details)
		{
			details.DelegateParamIndexes = new Dictionary<int, DelegateInterceptor>();
			for (int i = 0; i < details.ParamTypes.Length; i++)
			{
				var delegateParamInterceptor = details.Args[i] as DelegateInterceptor;
				if (delegateParamInterceptor != null)
				{
					details.DelegateParamIndexes.Add(i, delegateParamInterceptor);
					continue;
				}

				var container = details.Args[i] as CustomSerializationContainer;
				if (container != null)
				{
					var serializationHandler = _host.SerializationHandling[container.HandledType];
					if (serializationHandler == null)
					{
						var ex = new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_SerializationHandlerNotFound, container.HandledType.FullName));
						_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = details.TrackingID, CancelException = ex });
						throw ex;
					}

					details.Args[i] = serializationHandler.Deserialize(container.DataType, container.Data);
				}
			}
		}

		/// <summary>
		/// Resolves the component instance to be invoked.
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_ResolveComponentInstance(InvocationDetails details)
		{
			// Skip resolving registration, if already done
			if (details.Registration == null)
				details.Registration = _host.ComponentRegistry[details.InterfaceName];

			// get component instance
			details.Instance = _host.GetComponentInstance(details.Registration);
			details.Type = details.Instance.GetType();
		}

		/// <summary>
		/// Obtains metadata of the invoked method via reflection.
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_ObtainMethodMetadata(InvocationDetails details)
		{
			details.MethodInfo = details.Type.GetMethod(details.MethodName, details.GenericArguments, details.ParamTypes);
			if (details.MethodInfo == null)
			{
				var methodSignature = MessageHelpers.GetMethodSignature(details.Type, details.MethodName, details.ParamTypes);
				var exceptionMessage = String.Format(LanguageResource.MissingMethodException_MethodNotFound, methodSignature);
				throw new MissingMethodException(exceptionMessage);
			}
		}

		/// <summary>
		/// Intercepts delegate parameters.
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_InterceptDelegateParams(InvocationDetails details)
		{
			var serverMethodParamDefs = details.MethodInfo.GetParameters();
			foreach (int index in details.DelegateParamIndexes.Keys)
			{
				var delegateParamInterceptor = details.DelegateParamIndexes[index];
				var serverMethodParamDef = serverMethodParamDefs[index];

				var dynamicWire = DynamicWireFactory.CreateDynamicWire(details.Type, serverMethodParamDef.ParameterType);
				dynamicWire.Interceptor = delegateParamInterceptor;
				details.Args[index] = dynamicWire.InDelegate;
			}
		}

		/// <summary>
		/// Applies custom serialization on return value (if configured).
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_ApplyCustomSerializationOnReturnValue(InvocationDetails details)
		{
			if (details.ReturnValue != null)
			{
				Type returnValueType = details.ReturnValue.GetType();

				Type handledType;
				ISerializationHandler handler;
				_host.SerializationHandling.FindMatchingSerializationHandler(returnValueType, out handledType, out handler);

				if (handler != null)
				{
					byte[] raw = handler.Serialize(details.ReturnValue);
					details.ReturnValue = new CustomSerializationContainer(handledType, returnValueType, raw);
				}
			}
		}

		/// <summary>
		/// Completes the transaction scope, if no exception occured.
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_CompleteTransactionScope(InvocationDetails details)
		{
			if (details.Scope != null)
			{
				if (!details.ExceptionThrown)
					details.Scope.Complete();

				details.Scope.Dispose();
			}
		}

		/// <summary>
		/// Cleans up event handlers and component instance (if needed).
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_CleanUp(InvocationDetails details)
		{
			if (details.Instance != null && details.Registration != null && 
				details.Registration.ActivationType == ActivationType.SingleCall)
			{
				_host.ComponentCatalog.CleanUpComponentInstance(details.Registration, details.Instance);
			}
		}

		/// <summary>
		/// Processes remote method invocation.
		/// </summary>
		/// <param name="trackingID">Key for call tracking</param>
		/// <param name="interfaceName">Name of the component interface</param>
		/// <param name="delegateCorrelationSet">Correlation set for dynamic event and delegate wiring</param>
		/// <param name="methodName">Name of the invoked method</param>
		/// <param name="genericArguments">Generic arguments of the invoked method</param>
		/// <param name="paramTypes">Parameter types</param>
		/// <param name="args">Parameter values</param>
		/// <returns>Return value</returns>
		public object Invoke(Guid trackingID, string interfaceName, List<DelegateCorrelationInfo> delegateCorrelationSet, string methodName, Type[] genericArguments, Type[] paramTypes, params object[] args)
		{
			if (string.IsNullOrEmpty(interfaceName))
				throw new ArgumentException(LanguageResource.ArgumentException_InterfaceNameMissing, "interfaceName");

			if (string.IsNullOrEmpty(methodName))
				throw new ArgumentException(LanguageResource.ArgumentException_MethodNameMissing, "methodName");

			// Reset session variable (May point to wrong session, if threadpool thread is reused)
			_host.SessionManager.SetCurrentSession(null);

			var details = new InvocationDetails()
			{
				TrackingID = trackingID,
				InterfaceName = interfaceName,
				DelegateCorrelationSet = delegateCorrelationSet,
				MethodName = methodName,
				GenericArguments = genericArguments,
				ParamTypes = paramTypes,
				Args = args
			};

			var beforeInvokeOccured = false;

			try
			{
				Invoke_LoadCallContextData(details);
				Invoke_SetSession(details);
				Invoke_SetTransaction(details);

				beforeInvokeOccured = true;
				ProcessBeforeInvoke(details);

				Invoke_CheckInterfaceName(details);
				Invoke_ConvertMethodArguments(details);
				Invoke_ResolveComponentInstance(details);
				Invoke_ObtainMethodMetadata(details);
				Invoke_InterceptDelegateParams(details);

				details.ReturnValue = details.MethodInfo.Invoke(details.Instance, details.Args, details.MethodInfo.IsOneWay());

				Invoke_ApplyCustomSerializationOnReturnValue(details);
			}
			catch (Exception ex)
			{
				if (beforeInvokeOccured)
					Invoke_FireInvokeCanceledEvent(details, ex);
				else
					Invoke_FireInvokeRejectedEvent(details, ex);
			}
			finally
			{
				Invoke_CompleteTransactionScope(details);
				Invoke_SetRemoteSubscriptionCounter(details);
				Invoke_CleanUp(details);
			}

			ProcessAfterInvoke(details);

			return details.ReturnValue;
		}

		#endregion

		#region Event support

		/// <summary>
		/// Adds a handler to an event of a server component.
		/// </summary>
		/// <param name="interfaceName">Name of the server component interface</param>
		/// <param name="correlation">Correlation information</param>
		/// <param name="uniqueName">Unique name of the server component instance (May left empty, if component isn´t registered with a unique name)</param>
		public void AddEventHandler(string interfaceName, DelegateCorrelationInfo correlation, string uniqueName)
		{
			if (string.IsNullOrEmpty(interfaceName))
				throw new ArgumentException(LanguageResource.ArgumentException_InterfaceNameMissing, "interfaceName");

			if (string.IsNullOrEmpty(uniqueName))
				uniqueName = interfaceName;

			if (!_host.ComponentRegistry.ContainsKey(uniqueName))
				throw new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_CannotFindComponentForInterface, interfaceName));

			var details = new InvocationDetails()
			{
				InterfaceName = interfaceName,
				Registration = _host.ComponentRegistry[uniqueName]
			};

			Invoke_LoadCallContextData(details);
			Invoke_SetSession(details);
			Invoke_ResolveComponentInstance(details);

			var correlationSet = new List<DelegateCorrelationInfo>
			{
				correlation
			};

			CreateClientServerWires(details.Type, details.Registration.EventStub, correlationSet, details.Registration.EventWirings);
		}

		/// <summary>
		/// Removes a handler from an event of a server component.
		/// </summary>
		/// <param name="interfaceName">Name of the server component interface</param>
		/// <param name="correlation">Correlation information</param>
		/// <param name="uniqueName">Unique name of the server component instance (May left empty, if component isn´t registered with a unique name)</param>
		public void RemoveEventHandler(string interfaceName, DelegateCorrelationInfo correlation, string uniqueName)
		{
			if (string.IsNullOrEmpty(interfaceName))
				throw new ArgumentException(LanguageResource.ArgumentException_InterfaceNameMissing, "interfaceName");

			if (string.IsNullOrEmpty(uniqueName))
				uniqueName = interfaceName;

			if (!_host.ComponentRegistry.ContainsKey(uniqueName))
				throw new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_CannotFindComponentForInterface, interfaceName));

			var details = new InvocationDetails()
			{
				InterfaceName = interfaceName,
				Registration = _host.ComponentRegistry[uniqueName]
			};

			Invoke_LoadCallContextData(details);
			Invoke_SetSession(details);
			Invoke_ResolveComponentInstance(details);

			var correlationSet = new List<DelegateCorrelationInfo>
			{
				correlation
			};

			RemoveClientServerWires(details.Type, details.Registration.EventStub, correlationSet, details.Registration.EventWirings);
		}

		#endregion

		#region Metadata

		/// <summary>
		/// Returns an array with metadata about all registered components.
		/// </summary>
		/// <returns>Array with registered component metadata</returns>
		public ComponentInfo[] GetRegisteredComponents()
		{
			return _host.GetRegisteredComponents().ToArray();
		}

		#endregion

		#region Logon and Logoff

		/// <summary>
		/// Processes logon.
		/// </summary>
		/// <param name="sessionID">Unique session key (created on client side)</param>
		/// <param name="credentials">Logon credentials</param>
		public void Logon(Guid sessionID, Hashtable credentials)
		{
			if (sessionID == Guid.Empty)
				throw new ArgumentException(LanguageResource.ArgumentException_EmptySessionIDIsNotAllowed, "sessionID");

			if (!_host.SessionManager.ExistSession(sessionID))
			{
				// reset current session before authentication is complete
				_host.SessionManager.SetCurrentSession(null);

				AuthResponseMessage authResponse = _host.Authenticate(new AuthRequestMessage() { Credentials = credentials });
				if (!authResponse.Success)
				{
					var exception = authResponse.Exception ?? new SecurityException(authResponse.ErrorMessage);
					throw exception.PreserveStackTrace();
				}

				var session = _host.SessionManager.CreateServerSession(sessionID, DateTime.Now, authResponse.AuthenticatedIdentity);
				_host.SessionManager.StoreSession(session);
				_host.SessionManager.SetCurrentSession(session);
				PutClientAddressToCurrentSession();

				_host.OnClientLoggedOn(new LoginEventArgs(LoginEventType.Logon, session.Identity, session.ClientAddress, session.Timestamp));
			}
		}

		/// <summary>
		/// Returns true, if a specified Session ID is valid, otherwis false.
		/// </summary>
		/// <param name="sessionID">Session ID to check</param>
		/// <returns>Session check result</returns>
		public bool ExistSession(Guid sessionID)
		{
			if (sessionID == Guid.Empty)
				throw new ArgumentException(LanguageResource.ArgumentException_EmptySessionIDIsNotAllowed, "sessionID");

			return _host.SessionManager.ExistSession(sessionID);
		}

		/// <summary>
		/// Process logoff.
		/// </summary>
		/// <param name="sessionID">Unique session key</param>
		public void Logoff(Guid sessionID)
		{
			IIdentity identity = null;
			DateTime timestamp = DateTime.MinValue;

			var session = _host.SessionManager.GetSessionBySessionID(sessionID);
			if (session != null)
			{
				identity = session.Identity;
				timestamp = session.Timestamp;
			}
			_host.SessionManager.RemoveSession(sessionID);

			string clientIP = string.Empty;
			IPAddress clientAddress = GetCallingClientIPAddress();

			if (clientAddress != null)
				clientIP = clientAddress.ToString();

			try
			{
				if (identity != null)
					_host.OnClientLoggedOff(new LoginEventArgs(LoginEventType.Logoff, identity, clientIP, timestamp));
			}
			finally
			{
				// reset current session after the client is logged off
				_host.SessionManager.SetCurrentSession(null);
			}
		}

		#endregion

		#region Session management

		/// <summary>
		/// Gets the maximum sesseion age (in minutes).
		/// </summary>
		public int SessionAgeLimit
		{
			get { return _host.SessionManager.SessionAgeLimit; }
		}

		/// <summary>
		/// Extends the lifetime of the current session and returs the current session age limit.
		/// </summary>
		/// <returns>Session age limit (in minutes)</returns>
		public int RenewSession()
		{
			// validate context data
			var data = CallContext.GetData("__ZyanContextData_" + _host.Name) as LogicalCallContextData;
			if (data == null || !data.Store.ContainsKey("sessionid"))
			{
				throw new SecurityException(LanguageResource.SecurityException_ContextInfoMissing);
			}

			// validate session
			var sessionID = (Guid)data.Store["sessionid"];
			if (!_host.SessionManager.ExistSession(sessionID))
			{
				throw new InvalidSessionException(string.Format(LanguageResource.InvalidSessionException_SessionIDInvalid, sessionID.ToString()));
			}

			// renew session
			var session = _host.SessionManager.GetSessionBySessionID(sessionID);
			_host.SessionManager.SetCurrentSession(session);
			_host.SessionManager.RenewSession(session);
			return SessionAgeLimit;
		}

		#endregion

		#region Lifetime management

		/// <summary>
		/// Initializes the .NET Remoting limetime service of this object.
		/// </summary>
		/// <returns>Lease</returns>
		public override object InitializeLifetimeService()
		{
			// Unlimited lifetime
			return null;
		}

		#endregion

		#region Detect unexpected disconnection (Polling)

		/// <summary>
		/// Event: Occours when a heartbeat signal is received from a client.
		/// </summary>
		public event EventHandler<ClientHeartbeatEventArgs> ClientHeartbeatReceived;

		/// <summary>
		/// Fires the ClientHeartbeatReceived event.
		/// <remarks>
		/// Event is fired in a different thread to avoid blocking the client.
		/// </remarks>
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected virtual void OnClientHeartbeatReceived(ClientHeartbeatEventArgs e)
		{
			var eventHandler = ClientHeartbeatReceived;
			if (eventHandler != null)
			{
				// the current session is preserved because it's a part of the LogicalCallContext
				ThreadPool.QueueUserWorkItem(state => eventHandler(this, e));
			}
		}

		/// <summary>
		/// Called from client to send a heartbeat signal.
		/// </summary>
		/// <param name="sessionID">Client´s session key</param>
		public void ReceiveClientHeartbeat(Guid sessionID)
		{
			// validate server session
			var details = new InvocationDetails();
			Invoke_LoadCallContextData(details);
			Invoke_SetSession(details);

			// fire the heartbeat event
			OnClientHeartbeatReceived(new ClientHeartbeatEventArgs(DateTime.Now, sessionID));
		}

		#endregion
	}
}
