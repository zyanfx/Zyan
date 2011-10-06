using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
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
		/// <param name="type">Implementation type of the server component</param>
		/// <param name="instance">Instance of the server component</param>
		/// <param name="delegateCorrelationSet">Correlation set (say how to wire)</param>
		/// <param name="wiringList">Collection of built wires</param>
		private void CreateClientServerWires(Type type, object instance, List<DelegateCorrelationInfo> delegateCorrelationSet, Dictionary<Guid, Delegate> wiringList)
		{
			if (delegateCorrelationSet == null)
				return;

			foreach (var correlationInfo in delegateCorrelationSet)
			{
				if (wiringList.ContainsKey(correlationInfo.CorrelationID))
					continue;

				if (ServerSession.CurrentSession == null)
					throw new InvalidSessionException(string.Format(LanguageResource.InvalidSessionException_SessionIDInvalid, "(null)"));

				var dynamicWire = DynamicWireFactory.CreateDynamicWire(type, correlationInfo.DelegateMemberName, correlationInfo.IsEvent);
				dynamicWire.Interceptor = correlationInfo.ClientDelegateInterceptor;

				if (correlationInfo.IsEvent)
				{
					var eventInfo = type.GetEvent(correlationInfo.DelegateMemberName);
					var dynamicEventWire = (DynamicEventWireBase)dynamicWire;

					dynamicEventWire.ServerEventInfo = eventInfo;
					dynamicEventWire.Component = instance;

					// add session validation handler
					var sessionId = ServerSession.CurrentSession.SessionID;
					var sessionManager = _host.SessionManager;
					dynamicEventWire.ValidateSession = () => sessionManager.ExistSession(sessionId);

					eventInfo.AddEventHandler(instance, dynamicEventWire.InDelegate);
					wiringList.Add(correlationInfo.CorrelationID, dynamicEventWire.InDelegate);
				}
				else
				{
					var outputPinMetaData = type.GetProperty(correlationInfo.DelegateMemberName);
					outputPinMetaData.SetValue(instance, dynamicWire.InDelegate, null);
					wiringList.Add(correlationInfo.CorrelationID, dynamicWire.InDelegate);
				}
			}
		}

		/// <summary>
		/// Removes wires between server and client components (as defined in correlation set).
		/// </summary>
		/// <param name="type">Type of the server component</param>
		/// <param name="instance">Instance of the server component</param>
		/// <param name="delegateCorrelationSet">Correlation set with wiring information</param>
		/// <param name="wiringList">List with known wirings</param>
		private void RemoveClientServerWires(Type type, object instance, List<DelegateCorrelationInfo> delegateCorrelationSet, Dictionary<Guid, Delegate> wiringList)
		{
			if (delegateCorrelationSet == null)
				return;

			foreach (DelegateCorrelationInfo correlationInfo in delegateCorrelationSet)
			{
				if (correlationInfo.IsEvent)
				{
					if (wiringList.ContainsKey(correlationInfo.CorrelationID))
					{
						EventInfo eventInfo = type.GetEvent(correlationInfo.DelegateMemberName);
						Delegate dynamicWireDelegate = wiringList[correlationInfo.CorrelationID];

						eventInfo.RemoveEventHandler(instance, dynamicWireDelegate);
					}
				}
				else
				{
					PropertyInfo delegatePropInfo = type.GetProperty(correlationInfo.DelegateMemberName);
					delegatePropInfo.SetValue(instance, null, null);
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
					DelegateCorrelationSet =  details.DelegateCorrelationSet,
					MethodName = details.MethodName,
					Arguments = details.Args,
					Cancel = false
				};
				_host.OnBeforeInvoke(cancelArgs);

				if (cancelArgs.Cancel)
				{
					if (cancelArgs.CancelException == null)
						cancelArgs.CancelException = new InvokeCanceledException();

					_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = details.TrackingID, CancelException = cancelArgs.CancelException });

					throw cancelArgs.CancelException;
				}
				else
				{
					details.InterfaceName = cancelArgs.InterfaceName;
					details.DelegateCorrelationSet = cancelArgs.DelegateCorrelationSet;
					details.MethodName = cancelArgs.MethodName;
					details.Args = cancelArgs.Arguments;
				}
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
					DelegateCorrelationSet =  details.DelegateCorrelationSet,
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
			return CallContext.GetData("Zyan_ClientAddress") as IPAddress; ;
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
			ServerSession.CurrentSession = details.Session;
			PutClientAddressToCurrentSession();
		}

		/// <summary>
		/// Sets a transaction for the current worker thread, if provied.
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
		/// Fires the InvokeCanceled event.
		/// </summary>
		/// <param name="details">Invocation details</param>
		/// <param name="ex">Exception</param>
		private void Invoke_FireInvokeCanceledEvent(InvocationDetails details, Exception ex)
		{
			details.ExceptionThrown = true;

			_host.OnInvokeCanceled(new InvokeCanceledEventArgs
			{
				TrackingID = details.TrackingID,
				CancelException = ex
			});

			throw ex;
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
			// get component instance
			details.Instance = _host.GetComponentInstance(details.Registration);
			details.Type = details.Instance.GetType();
		}

		/// <summary>
		/// Wires up event handlers.
		/// </summary>
		/// <param name="details">Invocation details</param>
		private void Invoke_WireUpEventHandlers(InvocationDetails details)
		{
			details.WiringList = null;
			if (details.Registration.ActivationType == ActivationType.SingleCall)
			{
				details.WiringList = new Dictionary<Guid, Delegate>();
				CreateClientServerWires(details.Type, details.Instance, details.DelegateCorrelationSet, details.WiringList);
			}
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
			if (details.Registration.ActivationType == ActivationType.SingleCall)
			{
				if (details.WiringList!=null)
					RemoveClientServerWires(details.Type, details.Instance, details.DelegateCorrelationSet, details.WiringList);

				if (details.Instance!=null)
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

			ProcessBeforeInvoke(details);

			try
			{
				Invoke_CheckInterfaceName(details);
				Invoke_LoadCallContextData(details);
				Invoke_SetSession(details);
				Invoke_SetTransaction(details);
				Invoke_ConvertMethodArguments(details);

				details.Registration = _host.ComponentRegistry[details.InterfaceName];

				Invoke_ResolveComponentInstance(details);
				Invoke_WireUpEventHandlers(details);
				Invoke_ObtainMethodMetadata(details);
				Invoke_InterceptDelegateParams(details);

				details.ReturnValue = details.MethodInfo.Invoke(details.Instance, details.Args, details.MethodInfo.IsOneWay());

				Invoke_ApplyCustomSerializationOnReturnValue(details);
			}
			catch (Exception ex)
			{
				Invoke_FireInvokeCanceledEvent(details, ex);
			}
			finally
			{
				Invoke_CompleteTransactionScope(details);
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
		public void AddEventHandler(string interfaceName, DelegateCorrelationInfo correlation)
		{
			if (string.IsNullOrEmpty(interfaceName))
				throw new ArgumentException(LanguageResource.ArgumentException_InterfaceNameMissing, "interfaceName");

			if (!_host.ComponentRegistry.ContainsKey(interfaceName))
				throw new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_CannotFindComponentForInterface, interfaceName));

			var details = new InvocationDetails() 
			{ 
				InterfaceName = interfaceName
			};

			Invoke_LoadCallContextData(details);
			
			details.Registration = _host.ComponentRegistry[interfaceName];

			if (details.Registration.ActivationType != ActivationType.Singleton)
				return;

			Invoke_SetSession(details);
			Invoke_ResolveComponentInstance(details);
			
			var correlationSet = new List<DelegateCorrelationInfo>();
			correlationSet.Add(correlation);

			CreateClientServerWires(details.Type, details.Instance, correlationSet, details.Registration.EventWirings);
		}

		/// <summary>
		/// Removes a handler from an event of a server component.
		/// </summary>
		/// <param name="interfaceName">Name of the server component interface</param>
		/// <param name="correlation">Correlation information</param>
		public void RemoveEventHandler(string interfaceName, DelegateCorrelationInfo correlation)
		{
			if (string.IsNullOrEmpty(interfaceName))
				throw new ArgumentException(LanguageResource.ArgumentException_InterfaceNameMissing, "interfaceName");

			if (!_host.ComponentRegistry.ContainsKey(interfaceName))
				throw new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_CannotFindComponentForInterface, interfaceName));

			var details = new InvocationDetails()
			{
				InterfaceName = interfaceName
			};

			Invoke_LoadCallContextData(details);

			details.Registration = _host.ComponentRegistry[interfaceName];

			if (details.Registration.ActivationType != ActivationType.Singleton)
				return;

			Invoke_SetSession(details);
			Invoke_ResolveComponentInstance(details);

			List<DelegateCorrelationInfo> correlationSet = new List<DelegateCorrelationInfo>();
			correlationSet.Add(correlation);

			RemoveClientServerWires(details.Type, details.Instance, correlationSet, details.Registration.EventWirings);
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
				ServerSession.CurrentSession = null;

				AuthResponseMessage authResponse = _host.Authenticate(new AuthRequestMessage() { Credentials = credentials });
				if (!authResponse.Success)
				{
					var exception = authResponse.Exception ?? new SecurityException(authResponse.ErrorMessage);
					throw exception;
				}

				var sessionVariableAdapter = new SessionVariableAdapter(_host.SessionManager, sessionID);
				var session = new ServerSession(sessionID, authResponse.AuthenticatedIdentity, sessionVariableAdapter);
				_host.SessionManager.StoreSession(session);
				ServerSession.CurrentSession = session;
				PutClientAddressToCurrentSession();

				_host.OnClientLoggedOn(new LoginEventArgs(LoginEventType.Logon, session.Identity, session.ClientAddress, session.Timestamp));
			}
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

			if (clientAddress!=null)
				clientIP=clientAddress.ToString();

			try
			{
				if (identity != null)
					_host.OnClientLoggedOff(new LoginEventArgs(LoginEventType.Logoff, identity, clientIP, timestamp));
			}
			finally
			{
				// reset current session after the client is logged off
				ServerSession.CurrentSession = null;
			}
		}

		#endregion

		#region Notification (old NotificationService feature)

		/// <summary>
		/// Subscribe to a specified NotificationService event.
		/// </summary>
		/// <param name="eventName">Event name</param>
		/// <param name="handler">Delegate to client side event handler</param>
		public void Subscribe(string eventName, EventHandler<NotificationEventArgs> handler)
		{
			if (!_host.IsNotificationServiceRunning)
				throw new ApplicationException(LanguageResource.ApplicationException_NotificationServiceNotRunning);

			_host.NotificationService.Subscribe(eventName, handler);
		}

		/// <summary>
		/// Unsubscribe from a specified NotificationService event.
		/// </summary>
		/// <param name="eventName">Event name</param>
		/// <param name="handler">Delegate to client side event handler</param>
		public void Unsubscribe(string eventName, EventHandler<NotificationEventArgs> handler)
		{
			if (!_host.IsNotificationServiceRunning)
				throw new ApplicationException(LanguageResource.ApplicationException_NotificationServiceNotRunning);

			_host.NotificationService.Unsubscribe(eventName, handler);
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
			LogicalCallContextData data = CallContext.GetData("__ZyanContextData_" + _host.Name) as LogicalCallContextData;

			if (data != null)
			{
				if (data.Store.ContainsKey("sessionid"))
				{
					Guid sessionID = (Guid)data.Store["sessionid"];

					if (_host.SessionManager.ExistSession(sessionID))
					{
						ServerSession session = _host.SessionManager.GetSessionBySessionID(sessionID);
						session.Timestamp = DateTime.Now;

						ServerSession.CurrentSession = session;
					}
					else
					{
						InvalidSessionException ex = new InvalidSessionException(string.Format("Sitzungsschlüssel '{0}' ist ungültig! Bitte melden Sie sich erneut am Server an.", sessionID.ToString()));
						throw ex;
					}
				}
			}
			else
			{
				SecurityException ex = new SecurityException(LanguageResource.SecurityException_ContextInfoMissing);
				throw ex;
			}
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
			if (ClientHeartbeatReceived!=null)
				ThreadPool.QueueUserWorkItem(state  => ClientHeartbeatReceived(this,e));
		}

		/// <summary>
		/// Called from client to send a heartbeat signal.
		/// </summary>
		/// <param name="sessionID">Client´s session key</param>
		public void ReceiveClientHeartbeat(Guid sessionID)
		{
			OnClientHeartbeatReceived(new ClientHeartbeatEventArgs(DateTime.Now, sessionID));
		}

		#endregion
	}
}
