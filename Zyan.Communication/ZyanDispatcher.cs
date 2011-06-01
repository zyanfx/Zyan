using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Transactions;
using Zyan.Communication.Notification;
using Zyan.Communication.Security;
using Zyan.Communication.SessionMgmt;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication
{
	/// <summary>
	/// Allgemeiner Wrapper für eine verteilte Komponente.
	/// Nimmt Remoting-Aufrufe für eine bestimmte Komponente entgegen und
	/// leitet sie lokal an die Komponente weiter.
	/// </summary>
	public class ZyanDispatcher : MarshalByRefObject, IZyanDispatcher
	{
		#region Konstruktor

		/// <summary>
		/// Konstruktor.
		/// </summary>
		/// <param name="host">Komponentenhost</param>
		public ZyanDispatcher(ZyanComponentHost host)
		{
			// Wenn kein Komponentenhost übergeben wurde ...
			if (host == null)
				// Ausnahme werfen
				throw new ArgumentNullException("host");

			// Host übernehmen
			_host = host;
		}

		#endregion

		#region Komponentenaufruf

		// Felder
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
			
			foreach (DelegateCorrelationInfo correlationInfo in delegateCorrelationSet)
			{				
				if (wiringList.ContainsKey(correlationInfo.CorrelationID))					
					continue;

				object dynamicWire = DynamicWireFactory.Instance.CreateDynamicWire(type, correlationInfo.DelegateMemberName, correlationInfo.IsEvent);
				Type dynamicWireType = dynamicWire.GetType();
				dynamicWireType.GetProperty("Interceptor").SetValue(dynamicWire, correlationInfo.ClientDelegateInterceptor, null);

				if (correlationInfo.IsEvent)
				{
					EventInfo eventInfo = type.GetEvent(correlationInfo.DelegateMemberName);

					dynamicWireType.GetProperty("ServerEventInfo").SetValue(dynamicWire, eventInfo, null);
					dynamicWireType.GetProperty("Component").SetValue(dynamicWire, instance, null);

					Delegate dynamicWireDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, dynamicWire, dynamicWireType.GetMethod("In"));

					eventInfo.AddEventHandler(instance, dynamicWireDelegate);
					
                    wiringList.Add(correlationInfo.CorrelationID, dynamicWireDelegate);
				}
				else
				{
					PropertyInfo outputPinMetaData = type.GetProperty(correlationInfo.DelegateMemberName);
					Delegate dynamicWireDelegate = Delegate.CreateDelegate(outputPinMetaData.PropertyType, dynamicWire, dynamicWireType.GetMethod("In"));
					outputPinMetaData.SetValue(instance, dynamicWireDelegate, null);

					wiringList.Add(correlationInfo.CorrelationID, dynamicWireDelegate);
				}
			}
		}

		/// <summary>
		/// Entfernt Drähte zwischen Client- und Server-Komponente (wenn im Korrelationssatz angegeben).
		/// </summary>
		/// <param name="type">Implementierungstyp der Server-Komponente</param>
		/// <param name="instance">Instanz der Serverkomponente</param>
		/// <param name="delegateCorrelationSet">Korrelationssatz mit Verdrahtungsinformationen</param>
		/// <param name="wiringList">Auflistung mit gespeicherten Verdrahtungen</param>
		private void RemoveClientServerWires(Type type, object instance, List<DelegateCorrelationInfo> delegateCorrelationSet, Dictionary<Guid, Delegate> wiringList)
		{
			// Wenn kein Korrelationssatz angegeben wurde ...
			if (delegateCorrelationSet == null)
				// Prozedur abbrechen
				return;

			// Alle Einträge des Korrelationssatzes durchlaufen
			foreach (DelegateCorrelationInfo correlationInfo in delegateCorrelationSet)
			{
				// Wenn es sich um ein Ereignis handelt ...
				if (correlationInfo.IsEvent)
				{
					// Wenn eine Verdrahtung mit dem angegebenen Korrelationsschlüssel gespeichert ist ...
					if (wiringList.ContainsKey(correlationInfo.CorrelationID))
					{
						// Metadaten des Ereignisses abufen                
						EventInfo eventInfo = type.GetEvent(correlationInfo.DelegateMemberName);

						// Delegat abrufen
						Delegate dynamicWireDelegate = wiringList[correlationInfo.CorrelationID];

						// Verdrahtung aufheben
						eventInfo.RemoveEventHandler(instance, dynamicWireDelegate);
					}
				}
				else
				{
					// Metadaten des aktuellen Ausgabe-Pins abufen                
					PropertyInfo delegatePropInfo = type.GetProperty(correlationInfo.DelegateMemberName);

					// Verdrahtung aufheben
					delegatePropInfo.SetValue(instance, null, null);
				}
			}
		}

		/// <summary>
		/// Verarbeitet BeforeInvoke-Abos (falls welche registriert sind).
		/// </summary>
		/// <param name="trackingID">Aufrufschlüssel zur Nachverfolgung</param>
		/// <param name="interfaceName">Name der Komponentenschnittstelle</param>
		/// <param name="delegateCorrelationSet">Korrelationssatz für die Verdrahtung bestimmter Delegaten und Ereignisse mit entfernten Methoden</param>
		/// <param name="methodName">Methodenname</param>
		/// <param name="args">Parameter</param>   
		private void ProcessBeforeInvoke(Guid trackingID, ref string interfaceName, ref List<DelegateCorrelationInfo> delegateCorrelationSet, ref string methodName, ref object[] args)
		{
			// Wenn BeforeInvoke-Abos vorhanden sind ...
			if (_host.HasBeforeInvokeSubscriptions())
			{
				// Ereignisargumente für BeforeInvoke erstellen
				BeforeInvokeEventArgs cancelArgs = new BeforeInvokeEventArgs()
				{
					TrackingID = trackingID,
					InterfaceName = interfaceName,
					DelegateCorrelationSet = delegateCorrelationSet,
					MethodName = methodName,
					Arguments = args,
					Cancel = false
				};
				// BeforeInvoke-Ereignis feuern
				_host.OnBeforeInvoke(cancelArgs);

				// Wenn der Aufruf abgebrochen werden soll ...
				if (cancelArgs.Cancel)
				{
					// Wenn keine Abbruchausnahme definiert ist ...
					if (cancelArgs.CancelException == null)
						// Standard-Abbruchausnahme erstellen
						cancelArgs.CancelException = new InvokeCanceledException();

					// InvokeCanceled-Ereignis feuern
					_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = cancelArgs.CancelException });

					// Abbruchausnahme werfen
					throw cancelArgs.CancelException;
				}
				else // Wenn der Aufruf nicht abgebrochen werden soll ...
				{
					// Einstellungen der Ereignisargumente übernehmen
					interfaceName = cancelArgs.InterfaceName;
					delegateCorrelationSet = cancelArgs.DelegateCorrelationSet;
					methodName = cancelArgs.MethodName;
					args = cancelArgs.Arguments;
				}
			}
		}

		/// <summary>
		/// Verarbeitet AfterInvoke-Abos (falls welche registriert sind).
		/// </summary>
		/// <param name="trackingID">Aufrufschlüssel zur Nachverfolgung</param>
		/// <param name="interfaceName">Name der Komponentenschnittstelle</param>
		/// <param name="delegateCorrelationSet">Korrelationssatz für die Verdrahtung bestimmter Delegaten und Ereignisse mit entfernten Methoden</param>
		/// <param name="methodName">Methodenname</param>
		/// <param name="args">Parameter</param>   
		/// <param name="returnValue">Rückgabewert</param>
		private void ProcessAfterInvoke(Guid trackingID, ref string interfaceName, ref List<DelegateCorrelationInfo> delegateCorrelationSet, ref string methodName, ref object[] args, ref object returnValue)
		{
			// Wenn AfterInvoke-Abos registriert sind ...
			if (_host.HasAfterInvokeSubscriptions())
			{
				// Ereignisargumente für AfterInvoke erstellen
				AfterInvokeEventArgs afterInvokeArgs = new AfterInvokeEventArgs()
				{
					TrackingID = trackingID,
					InterfaceName = interfaceName,
					DelegateCorrelationSet = delegateCorrelationSet,
					MethodName = methodName,
					Arguments = args,
					ReturnValue = returnValue
				};
				// AfterInvoke-Ereignis feuern
				_host.OnAfterInvoke(afterInvokeArgs);
			}
		}

		//TODO: This method needs refactoring. It´s too big.
		/// <summary>
		/// Processes remote method invocation.        
		/// </summary>
		/// <param name="trackingID">Key for call tracking</param>
		/// <param name="interfaceName">Name of the component interface</param>
		/// <param name="delegateCorrelationSet">Correlation set for dynamic event and delegate wiring</param>
		/// <param name="methodName">Name of the invoked method</param>
		/// <param name="paramDefs">Reflection info of parameter types</param>
		/// <param name="args">Parameter values</param>        
		/// <returns>Return value</returns>
		public object Invoke(Guid trackingID, string interfaceName, List<DelegateCorrelationInfo> delegateCorrelationSet, string methodName, ParameterInfo[] paramDefs, params object[] args)
		{
			if (string.IsNullOrEmpty(interfaceName))
				throw new ArgumentException(LanguageResource.ArgumentException_InterfaceNameMissing, "interfaceName");

			if (string.IsNullOrEmpty(methodName))
				throw new ArgumentException(LanguageResource.ArgumentException_MethodNameMissing, "methodName");

			ProcessBeforeInvoke(trackingID, ref interfaceName, ref delegateCorrelationSet, ref methodName, ref args);

			if (!_host.ComponentRegistry.ContainsKey(interfaceName))
			{
				//TODO: Localize the exception text.
				KeyNotFoundException ex = new KeyNotFoundException(string.Format("Cannot find component for interface '{0}'.", interfaceName));
				_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });
				throw ex;
			}
			ComponentRegistration registration = _host.ComponentRegistry[interfaceName];
			object instance = _host.GetComponentInstance(registration);
			Type type = instance.GetType();

			Dictionary<Guid, Delegate> wiringList = null;

			if (registration.ActivationType == ActivationType.SingleCall)
			{
				wiringList = new Dictionary<Guid, Delegate>();
				CreateClientServerWires(type, instance, delegateCorrelationSet, wiringList);
			}
			TransactionScope scope = null;

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
						//TODO: Locaize the exception text.
						InvalidSessionException ex = new InvalidSessionException(string.Format("Session ID '{0}' is invalid. Please log on first.", sessionID.ToString()));
						_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });
						throw ex;
					}
				}
				if (data.Store.ContainsKey("transaction"))
					scope = new TransactionScope((Transaction)data.Store["transaction"]);
			}
			else
			{
				SecurityException ex = new SecurityException(LanguageResource.SecurityException_ContextInfoMissing);
				_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });
				throw ex;
			}
			object returnValue = null;

			Type[] types = new Type[paramDefs.Length];

			Dictionary<int, DelegateInterceptor> delegateParamIndexes = new Dictionary<int, DelegateInterceptor>();

			for (int i = 0; i < paramDefs.Length; i++)
			{
				types[i] = paramDefs[i].ParameterType;
				DelegateInterceptor delegateParamInterceptor = args[i] as DelegateInterceptor;

				if (delegateParamInterceptor != null)
					delegateParamIndexes.Add(i, delegateParamInterceptor);
				else
				{
					CustomSerializationContainer container = args[i] as CustomSerializationContainer;

					if (container != null)
					{
						ISerializationHandler serializationHandler = _host.SerializationHandling[container.HandledType];

						if (serializationHandler == null)
						{
							KeyNotFoundException ex = new KeyNotFoundException(string.Format(LanguageResource.KeyNotFoundException_SerializationHandlerNotFound, container.HandledType.FullName));
							_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });
							throw ex;
						}
						args[i] = serializationHandler.Deserialize(container.DataType, container.Data);
					}
				}
			}
			bool exceptionThrown = false;

			try
			{
				MethodInfo methodInfo = type.GetMethod(methodName, types);
				ParameterInfo[] serverMethodParamDefs = methodInfo.GetParameters();

				foreach (int index in delegateParamIndexes.Keys)
				{
					DelegateInterceptor delegateParamInterceptor = delegateParamIndexes[index];
					ParameterInfo serverMethodParamDef = serverMethodParamDefs[index];

					object dynamicWire = DynamicWireFactory.Instance.CreateDynamicWire(type, serverMethodParamDef.ParameterType);
					Type dynamicWireType = dynamicWire.GetType();
					dynamicWireType.GetProperty("Interceptor").SetValue(dynamicWire, delegateParamInterceptor, null);
					Delegate dynamicWireDelegate = Delegate.CreateDelegate(serverMethodParamDef.ParameterType, dynamicWire, dynamicWireType.GetMethod("In"));
					args[index] = dynamicWireDelegate;
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
				_host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });
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
					RemoveClientServerWires(type, instance, delegateCorrelationSet, wiringList);
			}
			ProcessAfterInvoke(trackingID, ref interfaceName, ref delegateCorrelationSet, ref methodName, ref args, ref returnValue);

			return returnValue;
		}

		#endregion

		#region Ereignis-Unterstützung

		/// <summary>
		/// Abonniert ein Ereignis einer Serverkomponente.
		/// </summary>
		/// <param name="interfaceName">Schnittstellenname der Serverkomponente</param>
		/// <param name="correlation">Korrelationsinformation</param>
		public void AddEventHandler(string interfaceName, DelegateCorrelationInfo correlation)
		{
			// Wenn kein Schnittstellenname angegeben wurde ...
			if (string.IsNullOrEmpty(interfaceName))
				// Ausnahme werfen
				throw new ArgumentException(LanguageResource.ArgumentException_InterfaceNameMissing, "interfaceName");

			// Wenn für den angegebenen Schnittstellennamen keine Komponente registriert ist ...
			if (!_host.ComponentRegistry.ContainsKey(interfaceName))
				// Ausnahme erzeugen
				throw new KeyNotFoundException(string.Format("Für die angegebene Schnittstelle '{0}' ist keine Komponente registiert.", interfaceName));

			// Komponentenregistrierung abrufen
			ComponentRegistration registration = _host.ComponentRegistry[interfaceName];

			// Wenn die Komponente nicht Singletonaktiviert ist ...
			if (registration.ActivationType != ActivationType.Singleton)
				// Prozedur abbrechen
				return;

			// Komponenteninstanz erzeugen
			object instance = _host.GetComponentInstance(registration);

			// Implementierungstyp abrufen
			Type type = instance.GetType();

			// Liste für Übergabe der Korrelationsinformation erzeugen
			List<DelegateCorrelationInfo> correlationSet = new List<DelegateCorrelationInfo>();
			correlationSet.Add(correlation);

			// Client- und Server-Komponente miteinander verdrahten
			CreateClientServerWires(type, instance, correlationSet, registration.EventWirings);
		}

		/// <summary>
		/// Entfernt das Abonnement eines Ereignisses einer Serverkomponente.
		/// </summary>
		/// <param name="interfaceName">Schnittstellenname der Serverkomponente</param>
		/// <param name="correlation">Korrelationsinformation</param>
		public void RemoveEventHandler(string interfaceName, DelegateCorrelationInfo correlation)
		{
			// Wenn kein Schnittstellenname angegeben wurde ...
			if (string.IsNullOrEmpty(interfaceName))
				// Ausnahme werfen
				throw new ArgumentException(LanguageResource.ArgumentException_InterfaceNameMissing, "interfaceName");

			// Wenn für den angegebenen Schnittstellennamen keine Komponente registriert ist ...
			if (!_host.ComponentRegistry.ContainsKey(interfaceName))
				// Ausnahme erzeugen
				throw new KeyNotFoundException(string.Format("Für die angegebene Schnittstelle '{0}' ist keine Komponente registiert.", interfaceName));

			// Komponentenregistrierung abrufen
			ComponentRegistration registration = _host.ComponentRegistry[interfaceName];

			// Wenn die Komponente nicht Singletonaktiviert ist ...
			if (registration.ActivationType != ActivationType.Singleton)
				// Prozedur abbrechen
				return;

			// Komponenteninstanz erzeugen
			object instance = _host.GetComponentInstance(registration);

			// Implementierungstyp abrufen
			Type type = instance.GetType();

			// Liste für Übergabe der Korrelationsinformation erzeugen
			List<DelegateCorrelationInfo> correlationSet = new List<DelegateCorrelationInfo>();
			correlationSet.Add(correlation);

			// Client- und Server-Komponente miteinander verdrahten
			RemoveClientServerWires(type, instance, correlationSet, registration.EventWirings);
		}

		#endregion

		#region Metadaten abfragen

		/// <summary>
		/// Gibt eine Liste mit allen registrierten Komponenten zurück.
		/// </summary>
		/// <returns>Liste mit Namen der registrierten Komponenten</returns>
		public ComponentInfo[] GetRegisteredComponents()
		{
			// Daten vom Host abrufen
			return _host.GetRegisteredComponents().ToArray();
		}

		#endregion

		#region An- und Abmelden

		/// <summary>
		/// Meldet einen Client am Applikationserver an.
		/// </summary>
		/// <param name="sessionID">Sitzungsschlüssel (wird vom Client erstellt)</param>
		/// <param name="credentials">Anmeldeinformationen</param>
		public void Logon(Guid sessionID, Hashtable credentials)
		{
			// Wenn kein eindeutiger Sitzungsschlüssel angegeben wurde ...
			if (sessionID == Guid.Empty)
				// Ausnahme werfen
				throw new ArgumentException(LanguageResource.ArgumentException_EmptySessionIDIsNotAllowed, "sessionID");

			// Wenn noch keine Sitzung mit dem angegebenen Sitzungsschlüssel existiert ...
			if (!_host.SessionManager.ExistSession(sessionID))
			{
				// Authentifizieren
				AuthResponseMessage authResponse = _host.Authenticate(new AuthRequestMessage() { Credentials = credentials });

				// Wenn die Authentifizierung fehlgeschlagen ist ...
				if (!authResponse.Success)
					// Ausnahme werfen
					throw new SecurityException(authResponse.ErrorMessage);

				// Sitzungsvariablen-Adapter erzeugen
				SessionVariableAdapter sessionVariableAdapter = new SessionVariableAdapter(_host.SessionManager, sessionID);

				// Neue Sitzung erstellen
				ServerSession session = new ServerSession(sessionID, authResponse.AuthenticatedIdentity, sessionVariableAdapter);

				// Sitzung speichern
				_host.SessionManager.StoreSession(session);

				// Aktuelle Sitzung im Threadspeicher ablegen
				ServerSession.CurrentSession = session;
			}
		}

		/// <summary>
		/// Meldet einen Client vom Applikationsserver ab.
		/// </summary>
		/// <param name="sessionID">Sitzungsschlüssel</param>
		public void Logoff(Guid sessionID)
		{
			// Sitzung entfernen
			_host.SessionManager.RemoveSession(sessionID);
		}

		#endregion

		#region Benachrichtigungen

		/// <summary>
		/// Registriert einen Client für den Empfang von Benachrichtigungen bei einem bestimmten Ereignis.
		/// </summary>
		/// <param name="eventName">Ereignisname</param>
		/// <param name="handler">Delegat auf Client-Ereignisprozedur</param>
		public void Subscribe(string eventName, EventHandler<NotificationEventArgs> handler)
		{
			// Wenn auf dem Host kein Benachrichtigungsdienst läuft ...
			if (!_host.IsNotificationServiceRunning)
				// Ausnahme werfen
				throw new ApplicationException(LanguageResource.ApplicationException_NotificationServiceNotRunning);

			// Für Benachrichtigung registrieren
			_host.NotificationService.Subscribe(eventName, handler);
		}

		/// <summary>
		/// Hebt eine Registrierung für den Empfang von Benachrichtigungen eines bestimmten Ereignisses auf.
		/// </summary>
		/// <param name="eventName">Ereignisname</param>
		/// <param name="handler">Delegat auf Client-Ereignisprozedur</param>
		public void Unsubscribe(string eventName, EventHandler<NotificationEventArgs> handler)
		{
			// Wenn auf dem Host kein Benachrichtigungsdienst läuft ...
			if (!_host.IsNotificationServiceRunning)
				// Ausnahme werfen
				throw new ApplicationException(LanguageResource.ApplicationException_NotificationServiceNotRunning);

			// Registrierung aufheben
			_host.NotificationService.Unsubscribe(eventName, handler);
		}

		#endregion

		#region Sitzungsverwaltung

		/// <summary>
		/// Gibt die maximale Sitzungslebensdauer (in Minuten) zurück.
		/// </summary>
		public int SessionAgeLimit
		{
			get { return _host.SessionManager.SessionAgeLimit; }
		}

		/// <summary>
		/// Verlängert die Sitzung des Aufrufers und gibt die aktuelle Sitzungslebensdauer zurück.
		/// </summary>
		/// <returns>Sitzungslebensdauer (in Minuten)</returns>
		public int RenewSession()
		{
			// Kontextdaten aus dem Aufrufkontext lesen (Falls welche hinterlegt sind)
			LogicalCallContextData data = CallContext.GetData("__ZyanContextData_" + _host.Name) as LogicalCallContextData;

			// Wenn Kontextdaten übertragen wurden ...
			if (data != null)
			{
				// Wenn ein Sitzungsschlüssel übertragen wurde ...
				if (data.Store.ContainsKey("sessionid"))
				{
					// Sitzungsschlüssel lesen
					Guid sessionID = (Guid)data.Store["sessionid"];

					// Wenn eine Sitzung mit dem angegebenen Schlüssel existiert ...
					if (_host.SessionManager.ExistSession(sessionID))
					{
						// Sitzung abrufen
						ServerSession session = _host.SessionManager.GetSessionBySessionID(sessionID);

						// Sitzung verlängern
						session.Timestamp = DateTime.Now;

						// Aktuelle Sitzung im Threadspeicher ablegen
						ServerSession.CurrentSession = session;
					}
					else
					{
						// Ausnahme erzeugen
						InvalidSessionException ex = new InvalidSessionException(string.Format("Sitzungsschlüssel '{0}' ist ungültig! Bitte melden Sie sich erneut am Server an.", sessionID.ToString()));

						// Ausnahme werfen
						throw ex;
					}
				}
			}
			else
			{
				// Ausnahme erzeugen
				SecurityException ex = new SecurityException(LanguageResource.SecurityException_ContextInfoMissing);

				// Ausnahme werfen
				throw ex;
			}
			// Sitzungslebensdauer zurückgeben
			return SessionAgeLimit;
		}

		#endregion

		#region Lebenszeitsteuerung

		/// <summary>
		/// Inizialisiert die Lebenszeitsteuerung des Objekts.
		/// </summary>
		/// <returns>Lease</returns>
		public override object InitializeLifetimeService()
		{
			// Laufzeitumgebungen für Ereignisbasierte Komponenten leben ewig
			return null;
		}

		#endregion
	}
}
