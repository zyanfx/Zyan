using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Security.Principal;
using System.Transactions;
using Zyan.Communication.Delegates;
using Zyan.Communication.Notification;
using Zyan.Communication.Security;
using Zyan.Communication.SessionMgmt;
using Zyan.Communication.Toolbox;
using System.Threading;

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

				var dynamicWire = DynamicWireFactory.CreateDynamicWire(type, correlationInfo.DelegateMemberName, correlationInfo.IsEvent);
				dynamicWire.Interceptor = correlationInfo.ClientDelegateInterceptor;

				if (correlationInfo.IsEvent)
				{
					var eventInfo = type.GetEvent(correlationInfo.DelegateMemberName);
					var dynamicEventWire = (DynamicEventWireBase)dynamicWire;

					dynamicEventWire.ServerEventInfo = eventInfo;
					dynamicEventWire.Component = instance;

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
		/// <param name="trackingID">Unique key for call tracking</param>
		/// <param name="interfaceName">Component interface name</param>
		/// <param name="delegateCorrelationSet">Set of correlation information to wire events and delegates</param>
		/// <param name="methodName">Method name</param>
		/// <param name="args">Arguments</param>   
		private void ProcessBeforeInvoke(Guid trackingID, ref string interfaceName, ref List<DelegateCorrelationInfo> delegateCorrelationSet, ref string methodName, ref object[] args)
		{
			if (_host.HasBeforeInvokeSubscriptions())
			{
				BeforeInvokeEventArgs cancelArgs = new BeforeInvokeEventArgs()
				{
					TrackingID = trackingID,
					InterfaceName = interfaceName,
					DelegateCorrelationSet = delegateCorrelationSet,
					MethodName = methodName,
					Arguments = args,
					Cancel = false
				};
				_host.OnBeforeInvoke(cancelArgs);

				if (cancelArgs.Cancel)
				{
					if (cancelArgs.CancelException == null)
						cancelArgs.CancelException = new InvokeCanceledException();

					_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = cancelArgs.CancelException });

					throw cancelArgs.CancelException;
				}
				else
				{
					interfaceName = cancelArgs.InterfaceName;
					delegateCorrelationSet = cancelArgs.DelegateCorrelationSet;
					methodName = cancelArgs.MethodName;
					args = cancelArgs.Arguments;
				}
			}
		}

		/// <summary>
		/// Processes AfterInvoke event subscriptions (if there any).
		/// </summary>
		/// <param name="trackingID">Unique key for call tracking</param>
		/// <param name="interfaceName">Component interface name</param>
		/// <param name="delegateCorrelationSet">Set of correlation information to wire events and delegates</param>
		/// <param name="methodName">Method name</param>
		/// <param name="args">Arguments</param>   
		/// <param name="returnValue">Return value</param>
		private void ProcessAfterInvoke(Guid trackingID, ref string interfaceName, ref List<DelegateCorrelationInfo> delegateCorrelationSet, ref string methodName, ref object[] args, ref object returnValue)
		{
			if (_host.HasAfterInvokeSubscriptions())
			{
				AfterInvokeEventArgs afterInvokeArgs = new AfterInvokeEventArgs()
				{
					TrackingID = trackingID,
					InterfaceName = interfaceName,
					DelegateCorrelationSet = delegateCorrelationSet,
					MethodName = methodName,
					Arguments = args,
					ReturnValue = returnValue
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

		//TODO: This method needs refactoring. It´s too big.
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

			ProcessBeforeInvoke(trackingID, ref interfaceName, ref delegateCorrelationSet, ref methodName, ref args);

			TransactionScope scope = null;

			try
			{
				// look up the component registration info
				if (!_host.ComponentRegistry.ContainsKey(interfaceName))
				{
					throw new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_CannotFindComponentForInterface, interfaceName));
				}

				// check for logical context data
				var data = CallContext.GetData("__ZyanContextData_" + _host.Name) as LogicalCallContextData;
				if (data == null)
				{
					throw new SecurityException(LanguageResource.SecurityException_ContextInfoMissing);
				}

				// validate session
				var sessionID = data.Store.ContainsKey("sessionid") ? (Guid)data.Store["sessionid"] : Guid.Empty;
				if (!_host.SessionManager.ExistSession(sessionID))
				{
					throw new InvalidSessionException(string.Format(LanguageResource.InvalidSessionException_SessionIDInvalid, sessionID.ToString()));
				}

				// set current session
				var session = _host.SessionManager.GetSessionBySessionID(sessionID);
				session.Timestamp = DateTime.Now;
				ServerSession.CurrentSession = session;
				PutClientAddressToCurrentSession();

				// transfer implicit transaction
				var transaction = data.Store.ContainsKey("transaction") ? (Transaction)data.Store["transaction"] : null;
				if (transaction != null)
				{
					scope = new TransactionScope(transaction);
				}
			}
			catch (Exception ex)
			{
				_host.OnInvokeCanceled(new InvokeCanceledEventArgs
				{
					TrackingID = trackingID,
					CancelException = ex
				});

				throw ex;
			}

			// convert method arguments
			var delegateParamIndexes = new Dictionary<int, DelegateInterceptor>();
			for (int i = 0; i < paramTypes.Length; i++)
			{
				var delegateParamInterceptor = args[i] as DelegateInterceptor;
				if (delegateParamInterceptor != null)
				{
					delegateParamIndexes.Add(i, delegateParamInterceptor);
					continue;
				}

				var container = args[i] as CustomSerializationContainer;
				if (container != null)
				{
					var serializationHandler = _host.SerializationHandling[container.HandledType];
					if (serializationHandler == null)
					{
						var ex = new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_SerializationHandlerNotFound, container.HandledType.FullName));
						_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });
						throw ex;
					}

					args[i] = serializationHandler.Deserialize(container.DataType, container.Data);
				}
			}

			// get component instance
			var registration = _host.ComponentRegistry[interfaceName];
			var instance = _host.GetComponentInstance(registration);
			var type = instance.GetType();

			// wire up event handlers
			Dictionary<Guid, Delegate> wiringList = null;
			if (registration.ActivationType == ActivationType.SingleCall)
			{
				wiringList = new Dictionary<Guid, Delegate>();
				CreateClientServerWires(type, instance, delegateCorrelationSet, wiringList);
			}

			// prepare return value and invoke method
			object returnValue = null;
			bool exceptionThrown = false;

			try
			{
				var methodInfo = type.GetMethod(methodName, genericArguments, paramTypes);
				if (methodInfo == null)
				{
					var methodSignature = MessageHelpers.GetMethodSignature(type, methodName, paramTypes);
					var exceptionMessage = String.Format(LanguageResource.MissingMethodException_MethodNotFound, methodSignature);
					throw new MissingMethodException(exceptionMessage);
				}

				var serverMethodParamDefs = methodInfo.GetParameters();
				foreach (int index in delegateParamIndexes.Keys)
				{
					var delegateParamInterceptor = delegateParamIndexes[index];
					var serverMethodParamDef = serverMethodParamDefs[index];

					var dynamicWire = DynamicWireFactory.CreateDynamicWire(type, serverMethodParamDef.ParameterType);
					dynamicWire.Interceptor = delegateParamInterceptor;
					args[index] = dynamicWire.InDelegate;
				}

				returnValue = methodInfo.Invoke(instance, args, methodInfo.IsOneWay());
				if (returnValue != null)
				{
					Type returnValueType = returnValue.GetType();

					Type handledType;
					ISerializationHandler handler;
					_host.SerializationHandling.FindMatchingSerializationHandler(returnValueType, out handledType, out handler);

					if (handler != null)
					{
						byte[] raw = handler.Serialize(returnValue);
						returnValue = new CustomSerializationContainer(handledType, returnValueType, raw);
					}
				}
			}
			catch (Exception ex)
			{
				exceptionThrown = true;

				_host.OnInvokeCanceled(new InvokeCanceledEventArgs
				{
					TrackingID = trackingID,
					CancelException = ex
				});

				throw ex;
			}
			finally
			{
				if (scope != null)
				{
					if (!exceptionThrown)
						scope.Complete();

					scope.Dispose();
				}

				if (registration.ActivationType == ActivationType.SingleCall)
				{
					RemoveClientServerWires(type, instance, delegateCorrelationSet, wiringList);
					_host.ComponentCatalog.CleanUpComponentInstance(registration, instance);
				}
			}

			ProcessAfterInvoke(trackingID, ref interfaceName, ref delegateCorrelationSet, ref methodName, ref args, ref returnValue);

			return returnValue;
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
				throw new KeyNotFoundException(string.Format("Für die angegebene Schnittstelle '{0}' ist keine Komponente registiert.", interfaceName));

			ComponentRegistration registration = _host.ComponentRegistry[interfaceName];

			if (registration.ActivationType != ActivationType.Singleton)
				return;

			object instance = _host.GetComponentInstance(registration);
			Type type = instance.GetType();

			List<DelegateCorrelationInfo> correlationSet = new List<DelegateCorrelationInfo>();
			correlationSet.Add(correlation);

			CreateClientServerWires(type, instance, correlationSet, registration.EventWirings);
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
				throw new KeyNotFoundException(string.Format("Für die angegebene Schnittstelle '{0}' ist keine Komponente registiert.", interfaceName));

			ComponentRegistration registration = _host.ComponentRegistry[interfaceName];

			if (registration.ActivationType != ActivationType.Singleton)
				return;

			object instance = _host.GetComponentInstance(registration);
			Type type = instance.GetType();

			List<DelegateCorrelationInfo> correlationSet = new List<DelegateCorrelationInfo>();
			correlationSet.Add(correlation);

			RemoveClientServerWires(type, instance, correlationSet, registration.EventWirings);
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
