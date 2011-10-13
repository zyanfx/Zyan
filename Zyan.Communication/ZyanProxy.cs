using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Zyan.Communication.Delegates;
using Zyan.InterLinq;

namespace Zyan.Communication
{
	/// <summary>
	/// Delegat für den Aufruf von InvokeRemoteMethod.
	/// </summary>
	/// <param name="methodCallMessage">Remoting-Nachricht</param>
	/// <param name="allowCallInterception">Gibt an, ob das Abfangen von Aufrufen zugelassen wird, oder nicht</param>
	/// <returns>Antwortnachricht</returns>
	public delegate IMessage InvokeRemoteMethodDelegate(IMethodCallMessage methodCallMessage, bool allowCallInterception);

	/// <summary>
	/// Stellvertreterobjekt für den Zugriff auf eine entfernte Komponente.
	/// </summary>
	public class ZyanProxy : RealProxy
	{
		// Felder
		private Type _interfaceType = null;
		private IZyanDispatcher _remoteDispatcher = null;
		private List<DelegateCorrelationInfo> _delegateCorrelationSet = null;
		private bool _implicitTransactionTransfer = false;
		private Guid _sessionID;
		private string _componentHostName = string.Empty;
		private bool _autoLoginOnExpiredSession = false;
		private Hashtable _autoLoginCredentials = null;
		private ZyanConnection _connection = null;
		private ActivationType _activationType = ActivationType.SingleCall;
		private string _uniqueName = string.Empty;

		/// <summary>
		/// Konstruktor.
		/// </summary>
		/// <param name="uniqueName">Eindeutiger Komponentenname</param>
		/// <param name="type">Schnittstelle der entfernten Komponente</param>
		/// <param name="connection">Verbindungsobjekt</param>
		/// <param name="implicitTransactionTransfer">Implizite Transaktionsübertragung</param>
		/// <param name="sessionID">Sitzungsschlüssel</param>
		/// <param name="componentHostName">Name des entfernten Komponentenhosts</param>
		/// <param name="autoLoginOnExpiredSession">Gibt an, ob sich der Proxy automatisch neu anmelden soll, wenn die Sitzung abgelaufen ist</param>
		/// <param name="autoLogoninCredentials">Optional! Anmeldeinformationen, die nur benötigt werden, wenn autoLoginOnExpiredSession auf Wahr eingestellt ist</param>              
		/// <param name="activationType">Aktivierungsart</param>
		public ZyanProxy(string uniqueName, Type type, ZyanConnection connection, bool implicitTransactionTransfer, Guid sessionID, string componentHostName, bool autoLoginOnExpiredSession, Hashtable autoLogoninCredentials, ActivationType activationType)
			: base(type)
		{
			// Wenn kein Typ angegeben wurde ...
			if (type.Equals(null))
				// Ausnahme werfen
				throw new ArgumentNullException("type");

			// Wenn kein Verbindungsobjekt angegeben wurde ...
			if (connection == null)
				// Ausnahme werfen
				throw new ArgumentNullException("connection");

			// Wenn kein eindeutiger Name angegeben wurde ...
			if (string.IsNullOrEmpty(uniqueName))
				// Name der Schnittstelle verwenden
				_uniqueName = type.FullName;
			else
				_uniqueName = uniqueName;

			// Sitzungsschlüssel übernehmen
			_sessionID = sessionID;

			// Verbindungsobjekt übernehmen
			_connection = connection;

			// Name des Komponentenhosts übernehmen
			_componentHostName = componentHostName;

			// Schnittstellentyp übernehmen
			_interfaceType = type;

			// Aktivierungsart übernehmen
			_activationType = activationType;

			// Aufrufer von Verbindung übernehmen
			_remoteDispatcher = _connection.RemoteDispatcher;

			// Schalter für implizite Transaktionsübertragung übernehmen
			_implicitTransactionTransfer = implicitTransactionTransfer;

			// Schalter für automatische Anmeldung bei abgelaufender Sitzung übernehmen
			_autoLoginOnExpiredSession = autoLoginOnExpiredSession;

			// Wenn automatische Anmeldung aktiv ist ...
			if (_autoLoginOnExpiredSession)
				// Anmeldeinformationen speichern
				_autoLoginCredentials = autoLogoninCredentials;

			// Sammlung für Korrelationssatz erzeugen
			_delegateCorrelationSet = new List<DelegateCorrelationInfo>();
		}

		/// <summary>
		/// Gets the name of the remote Component Host.
		/// </summary>
		public string ComponentHostName
		{
			get { return _componentHostName; }
		}

		/// <summary>
		/// Invoke remote method.
		/// </summary>
		/// <param name="message">Remoting method invocation message.</param>
		/// <returns>Reply message</returns>
		public override IMessage Invoke(IMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			// Get method details
			var methodCallMessage = (IMethodCallMessage)message;
			var methodInfo = (MethodInfo)methodCallMessage.MethodBase;

			// handle Object class method calls locally
			if (methodInfo.DeclaringType == typeof(object))
			{
				var returnMessage = InvokeLocally(methodInfo.Name, methodCallMessage);
				if (returnMessage != null)
				{
					return returnMessage;
				}
			}

			try
			{
				// Check for delegate parameters in properties and events
				if (methodInfo.ReturnType.Equals(typeof(void)) &&
					methodCallMessage.InArgCount == 1 &&
					methodCallMessage.ArgCount == 1 &&
					methodCallMessage.Args[0] != null &&
					typeof(Delegate).IsAssignableFrom(methodCallMessage.Args[0].GetType()) &&
					(methodCallMessage.MethodName.StartsWith("set_") || methodCallMessage.MethodName.StartsWith("add_")))
				{
					// Get client delegate
					object receiveMethodDelegate = methodCallMessage.GetArg(0);

					// Trim "set_" or "add_" prefix
					string propertyName = methodCallMessage.MethodName.Substring(4);

					// Create delegate correlation info
					DelegateInterceptor wiring = new DelegateInterceptor()
					{
						ClientDelegate = receiveMethodDelegate
					};

					DelegateCorrelationInfo correlationInfo = new DelegateCorrelationInfo()
					{
						IsEvent = methodCallMessage.MethodName.StartsWith("add_"),
						DelegateMemberName = propertyName,
						ClientDelegateInterceptor = wiring
					};

					// If component is singleton, attach event handler
					if (_activationType == ActivationType.Singleton)
					{
						_connection.PrepareCallContext(false);
						_connection.RemoteDispatcher.AddEventHandler(_interfaceType.FullName, correlationInfo, _uniqueName);
					}
					// Save delegate correlation info
					_delegateCorrelationSet.Add(correlationInfo);

					return new ReturnMessage(null, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
				}
				else if (methodInfo.ReturnType.Equals(typeof(void)) &&
					methodCallMessage.InArgCount == 1 &&
					methodCallMessage.ArgCount == 1 &&
					methodCallMessage.Args[0] != null &&
					typeof(Delegate).IsAssignableFrom(methodCallMessage.Args[0].GetType()) &&
					(methodCallMessage.MethodName.StartsWith("remove_")))
				{
					object inputMessage = methodCallMessage.GetArg(0);
					string propertyName = methodCallMessage.MethodName.Substring(7);

					if (_delegateCorrelationSet.Count > 0)
					{
						DelegateCorrelationInfo found = (from correlationInfo in _delegateCorrelationSet.ToArray()
														 where correlationInfo.DelegateMemberName.Equals(propertyName) && correlationInfo.ClientDelegateInterceptor.ClientDelegate.Equals(inputMessage)
														 select correlationInfo).FirstOrDefault();

						if (found != null)
						{
							if (_activationType == ActivationType.SingleCall)
								_delegateCorrelationSet.Remove(found);
							else
							{
								_connection.PrepareCallContext(false);
								_connection.RemoteDispatcher.RemoveEventHandler(_interfaceType.FullName, found, _uniqueName);
							}
						}
					}
					return new ReturnMessage(null, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
				}
				else if (methodInfo.GetParameters().Length == 0 &&
					methodInfo.GetGenericArguments().Length == 1 &&
					typeof(IEnumerable).IsAssignableFrom(methodInfo.ReturnType))
				{
					var elementType = methodInfo.GetGenericArguments().First();
					var serverHandlerName = ZyanMethodQueryHandler.GetMethodQueryHandlerName(_uniqueName, methodInfo);
					var clientHandler = new ZyanClientQueryHandler(_connection, serverHandlerName);
					var returnValue = clientHandler.Get(elementType);
					return new ReturnMessage(returnValue, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
				}
				else if (methodInfo.GetParameters().Length == 0 && 
					methodInfo.GetGenericArguments().Length == 1 &&
					typeof(IQueryable).IsAssignableFrom(methodInfo.ReturnType))
				{
					var elementType = methodInfo.GetGenericArguments().First();
					var serverHandlerName = ZyanMethodQueryHandler.GetMethodQueryHandlerName(_uniqueName, methodInfo);
					var clientHandler = new ZyanClientQueryHandler(_connection, serverHandlerName);
					var returnValue = clientHandler.Get(elementType);
					return new ReturnMessage(returnValue, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
				}
				else
				{
					_connection.PrepareCallContext(_implicitTransactionTransfer);
					return InvokeRemoteMethod(methodCallMessage, true);
				}
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
							throw ex;
						case ZyanErrorAction.Retry:
							return Invoke(message);
						case ZyanErrorAction.Ignore:
							return new ReturnMessage(null, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
					}
				}

				throw ex;
			}
		}

		/// <summary>
		/// Handles certain invocations locally for methods declared by System.Object class.
		/// </summary>
		private ReturnMessage InvokeLocally(string methodName, IMethodCallMessage methodCallMessage)
		{
			Func<object, ReturnMessage> GetResult =
				result => new ReturnMessage(result, null, 0, null, methodCallMessage);

			switch (methodName)
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

		/// <summary>
		/// Führt einen entfernten Methodenaufruf aus.
		/// </summary>
		/// <param name="methodCallMessage">Remoting-Nachricht mit Details für den entfernten Methodenaufruf</param>
		/// <param name="allowCallInterception">Gibt an, ob das Abfangen von Aufrufen zugelassen wird, oder nicht</param>
		/// <returns>Remoting Antwortnachricht</returns>
		internal IMessage InvokeRemoteMethod(IMethodCallMessage methodCallMessage, bool allowCallInterception)
		{
			// Aufrufschlüssel vergeben
			Guid trackingID = Guid.NewGuid();

			try
			{
				// Variable für Rückgabewert
				object returnValue = null;

				// Variable für Verdrahtungskorrelationssatz
				List<DelegateCorrelationInfo> correlationSet = null;

				// Wenn die Komponente SingleCallaktiviert ist ...
				if (_activationType == ActivationType.SingleCall)
					// Korrelationssatz übernehmen (wird mit übertragen)
					correlationSet = _delegateCorrelationSet;

				// Ereignisargumente für BeforeInvoke erstellen
				BeforeInvokeEventArgs cancelArgs = new BeforeInvokeEventArgs()
				{
					TrackingID = trackingID,
					InterfaceName = _interfaceType.FullName,
					DelegateCorrelationSet = correlationSet,
					MethodName = methodCallMessage.MethodName,
					Arguments = methodCallMessage.Args,
					Cancel = false
				};
				// BeforeInvoke-Ereignis feuern
				_connection.OnBeforeInvoke(cancelArgs);

				// Wenn der Aufruf abgebrochen werden soll ...
				if (cancelArgs.Cancel)
				{
					// Wenn keine Abbruchausnahme definiert ist ...
					if (cancelArgs.CancelException == null)
						// Standard-Abbruchausnahme erstellen
						cancelArgs.CancelException = new InvokeCanceledException();

					// InvokeCanceled-Ereignis feuern
					_connection.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = cancelArgs.CancelException });

					// Abbruchausnahme werfen
					throw cancelArgs.CancelException;
				}

				// Prepare generic method arguments
				Type[] genericArgs = null;
				if (methodCallMessage.MethodBase.IsGenericMethod)
				{
					genericArgs = methodCallMessage.MethodBase.GetGenericArguments();
				}

				// Parametertypen ermitteln
				var paramDefs = methodCallMessage.MethodBase.GetParameters();
				var paramTypes = paramDefs.Select(p => p.ParameterType).ToArray();

				// Abfragen, ob Abfangvorrichtungen verarbeitet werden sollen
				bool callInterception = _connection.CallInterceptionEnabled && allowCallInterception;

				// Wenn Aufrufabfangvorrichtungen verarbeitet werden sollen ...
				if (callInterception)
				{
					// Passende Aufrufabfangvorrichtung suchen
					CallInterceptor interceptor = _connection.CallInterceptors.FindMatchingInterceptor(_interfaceType, methodCallMessage);

					// Wenn eine passende Aufrufabfangvorrichtung gefunden wurde ...
					if (interceptor != null)
					{
						// Aufrufdaten zusammenstellen
						CallInterceptionData interceptionData = new CallInterceptionData(methodCallMessage.Args, new InvokeRemoteMethodDelegate(this.InvokeRemoteMethod), methodCallMessage);

						// Wenn ein Delegat für die Behandlung der Abfangaktion hinterlegt ist ...
						if (interceptor.OnInterception != null)
							// Aufruf abfangen
							interceptor.OnInterception(interceptionData);

						// Wenn der Aufruf abgefangen wurde ...
						if (interceptionData.Intercepted)
							// Rückgabewert übernehmen
							returnValue = interceptionData.ReturnValue;
						else
							// Schalter für Aufrufabfangverarbeitung zurücksetzen
							callInterception = false;
					}
				}
				// Wenn der Aufruf nicht abgefangen wurde ...
				if (!callInterception)
				{
					try
					{
						// Ggf. Delegaten-Parameter abfangen
						object[] checkedArgs = InterceptDelegateParameters(methodCallMessage);

						// Entfernten Methodenaufruf durchführen
						returnValue = _remoteDispatcher.Invoke(trackingID, _uniqueName, correlationSet, methodCallMessage.MethodName, genericArgs, paramTypes, checkedArgs);

						// Ereignisargumente für AfterInvoke erstellen
						AfterInvokeEventArgs afterInvokeArgs = new AfterInvokeEventArgs()
						{
							TrackingID = trackingID,
							InterfaceName = _interfaceType.FullName,
							DelegateCorrelationSet = correlationSet,
							MethodName = methodCallMessage.MethodName,
							Arguments = methodCallMessage.Args,
							ReturnValue = returnValue
						};
						// AfterInvoke-Ereignis feuern
						_connection.OnAfterInvoke(afterInvokeArgs);
					}
					catch (InvalidSessionException)
					{
						// Wenn automatisches Anmelden bei abgelaufener Sitzung aktiviert ist ...
						if (_autoLoginOnExpiredSession)
						{
							// Neu anmelden
							_remoteDispatcher.Logon(_sessionID, _autoLoginCredentials);

							// Entfernten Methodenaufruf erneut versuchen
							returnValue = _remoteDispatcher.Invoke(trackingID, _uniqueName, correlationSet, methodCallMessage.MethodName, genericArgs, paramTypes, methodCallMessage.Args);
						}
						else
						{
							throw;
						}
					}
				}
				// Versuchen den Rückgabewert in einen Serialisierungscontainer zu casten
				CustomSerializationContainer container = returnValue as CustomSerializationContainer;

				// Wenn der aktuelle Parameter ein Serialisierungscontainer ist ...
				if (container != null)
				{
					// Passenden Serialisierungshandler suchen
					ISerializationHandler serializationHandler = _connection.SerializationHandling[container.HandledType];

					// Wenn kein passender Serialisierungshandler registriert ist ...
					if (serializationHandler == null)
						// Ausnahme werfen
						throw new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_SerializationHandlerNotFound, container.HandledType.FullName));

					// Deserialisierung durchführen
					returnValue = serializationHandler.Deserialize(container.DataType, container.Data);
				}
				// Remoting-Antwortnachricht erstellen und zurückgeben
				return new ReturnMessage(returnValue, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
			}
			catch (Exception ex)
			{
				if (_connection.ErrorHandlingEnabled)
					throw ex;
				else
					return new ReturnMessage(ex, methodCallMessage);
			}
		}

		/// <summary>
		/// Ersetzt Delegaten-Parameter einer Remoting-Nachricht durch eine entsprechende Delegaten-Abfangvorrichtung.
		/// </summary>
		/// <param name="message">Remoting-Nachricht</param>
		/// <returns>argumentliste</returns>
		private object[] InterceptDelegateParameters(IMethodCallMessage message)
		{
			// Argument-Array erzeugen
			object[] result = new object[message.ArgCount];

			// Parametertypen ermitteln
			ParameterInfo[] paramDefs = message.MethodBase.GetParameters();

			// Alle Parameter durchlaufen
			for (int i = 0; i < message.ArgCount; i++)
			{
				// Parameter abrufen
				object arg = message.Args[i];

				// Wenn der aktuelle Parameter ein Delegat ist ...
				if (arg != null && typeof(Delegate).IsAssignableFrom(arg.GetType()))
				{
					// Abfangvorrichtung erzeugen
					DelegateInterceptor interceptor = new DelegateInterceptor()
					{
						ClientDelegate = arg
					};
					// Original-Parameter durch Abfangvorrichting in der Remoting-Nachricht ersetzen
					result[i] = interceptor;
				}
				else
				{
					// Typ des Parameters abfragen
					Type argType = paramDefs[i].ParameterType;

					// Passenden Serialisierungshandler suchen
					Type handledType;
					ISerializationHandler handler;
					_connection.SerializationHandling.FindMatchingSerializationHandler(argType, out handledType, out handler);

					// Wenn für diesen Typ ein passender Serialisierungshandler registriert ist ...
					if (handler != null)
					{
						// Serialisierung durchführen
						byte[] raw = handler.Serialize(arg);

						// Parameter durch Serialisierungscontainer ersetzen
						result[i] = new CustomSerializationContainer(handledType, argType, raw);
					}
					else
						// 1:1
						result[i] = arg;
				}
			}
			// Arument-Array zurückgeben
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
				for(int i=_delegateCorrelationSet.Count-1; i>=0;i--)
				{
					var correlationInfo = _delegateCorrelationSet[i];

					if (_activationType == ActivationType.SingleCall)
						_delegateCorrelationSet.Remove(correlationInfo);
					else
					{
						_connection.PrepareCallContext(false);
						_connection.RemoteDispatcher.RemoveEventHandler(_interfaceType.FullName, correlationInfo, _uniqueName);
					}
				}
			}
		}
	}
}

