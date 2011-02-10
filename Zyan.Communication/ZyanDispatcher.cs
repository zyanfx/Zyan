using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Security.Authentication;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Transactions;
using Zyan.Communication.Security;
using Zyan.Communication.Notification;
using Zyan.Communication.SessionMgmt;
using System.Linq.Expressions;

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
        /// Erstellt Drähte zwischen Client- und Server-Komponente (wenn im Korrelationssatz angegeben).
        /// </summary>
        /// <param name="type">Implementierungstyp der Server-Komponente</param>
        /// <param name="instance">Instanz der Serverkomponente</param>
        /// <param name="delegateCorrelationSet">Korrelationssatz mit Verdrahtungsinformationen</param>
        /// <param name="wiringList">Auflistung mit gespeicherten Verdrahtungen</param>
        private void CreateClientServerWires(Type type, object instance, List<DelegateCorrelationInfo> delegateCorrelationSet, Dictionary<Guid, Delegate> wiringList)
        {
            // Wenn kein Korrelationssatz angegeben wurde ...
            if (delegateCorrelationSet == null)
                // Prozedur abbrechen
                return;

            // Alle Einträge des Korrelationssatzes durchlaufen
            foreach (DelegateCorrelationInfo correlationInfo in delegateCorrelationSet)
            {                
                // Wenn mit diesem Korrelationsschlüssel schon verdrahtet wurde ...
                if (wiringList.ContainsKey(correlationInfo.CorrelationID))
                    // Mit dem nächsten Eintrag weitermachen
                    continue;
                
                // Dynamischen Draht erzeugen
                object dynamicWire = DynamicWireFactory.Instance.CreateDynamicWire(type, correlationInfo.DelegateMemberName, correlationInfo.IsEvent);

                // Typ des dynamischen Drahtes ermitteln
                Type dynamicWireType = dynamicWire.GetType();

                // Dynamischen Draht mit Client-Fernsteuerung verdrahten
                dynamicWireType.GetProperty("Interceptor").SetValue(dynamicWire, correlationInfo.ClientDelegateInterceptor, null);
                                               
                // wenn es sich um ein Ereignis handelt ...
                if (correlationInfo.IsEvent)
                {
                    // Metadaten des Ereignisses abufen                
                    EventInfo eventInfo = type.GetEvent(correlationInfo.DelegateMemberName);

                    // Zusatzinformationen übergeben
                    dynamicWireType.GetProperty("ServerEventInfo").SetValue(dynamicWire, eventInfo, null);
                    dynamicWireType.GetProperty("Component").SetValue(dynamicWire, instance, null);

                    // Delegat zu dynamischem Draht erzeugen
                    Delegate dynamicWireDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, dynamicWire, dynamicWireType.GetMethod("In"));

                    // Ausgehende Nachrichten mit passender Abfangvorrichtung verdrahten 
                    eventInfo.AddEventHandler(instance, dynamicWireDelegate);
                    
                    // Verdrahtung speichern
                    wiringList.Add(correlationInfo.CorrelationID, dynamicWireDelegate);
                }
                else
                {
                    // Metadaten des aktuellen Ausgabe-Pins abufen                
                    PropertyInfo outputPinMetaData = type.GetProperty(correlationInfo.DelegateMemberName);                                       

                    // Delegat zu dynamischem Draht erzeugen
                    Delegate dynamicWireDelegate = Delegate.CreateDelegate(outputPinMetaData.PropertyType, dynamicWire, dynamicWireType.GetMethod("In"));

                    // Ausgehende Nachrichten mit passender Abfangvorrichtung verdrahten 
                    outputPinMetaData.SetValue(instance, dynamicWireDelegate, null);

                    // Verdrahtung speichern
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

        /// <summary>
        /// Ruft eine bestimmte Methode einer Komponente auf und übergibt die angegebene Nachricht als Parameter.
        /// Für jeden Aufruf wird temporär eine neue Instanz der Komponente erstellt.
        /// </summary>
        /// <param name="trackingID">Aufrufschlüssel zur Nachverfolgung</param>
        /// <param name="interfaceName">Name der Komponentenschnittstelle</param>
        /// <param name="delegateCorrelationSet">Korrelationssatz für die Verdrahtung bestimmter Delegaten oder Ereignisse mit entfernten Methoden</param>
        /// <param name="methodName">Methodenname</param>
        /// <param name="paramDefs">Parameter-Definitionen</param>
        /// <param name="args">Parameter</param>        
        /// <returns>Rückgabewert</returns>
        public object Invoke(Guid trackingID, string interfaceName, List<DelegateCorrelationInfo> delegateCorrelationSet, string methodName, ParameterInfo[] paramDefs, params object[] args)
        {
            // Wenn kein Schnittstellenname angegeben wurde ...
            if (string.IsNullOrEmpty(interfaceName))
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_InterfaceNameMissing, "interfaceName");

            // Wenn kein Methodenname angegben wurde ...
            if (string.IsNullOrEmpty(methodName))
                // Ausnahme werfen
                throw new ArgumentException(LanguageResource.ArgumentException_MethodNameMissing, "methodName");

            // Ggf. BeforeInvoke-Abos verarbeiten
            ProcessBeforeInvoke(trackingID, ref interfaceName, ref delegateCorrelationSet, ref methodName, ref args);

            // Wenn für den angegebenen Schnittstellennamen keine Komponente registriert ist ...
            if (!_host.ComponentRegistry.ContainsKey(interfaceName))
            {
                // Ausnahme erzeugen
                KeyNotFoundException ex = new KeyNotFoundException(string.Format("Für die angegebene Schnittstelle '{0}' ist keine Komponente registiert.", interfaceName));

                // InvokeCanceled-Ereignis feuern
                _host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });

                // Ausnahme werfen
                throw ex;
            }
            // Komponentenregistrierung abrufen
            ComponentRegistration registration = _host.ComponentRegistry[interfaceName];

            // Komponenteninstanz erzeugen
            object instance = _host.GetComponentInstance(registration);

            // Implementierungstyp abrufen
            Type type = instance.GetType();

            // Auflistung für Ereignisverdrahtungen
            Dictionary<Guid, Delegate> wiringList = null;

            // Wenn die Komponente SingleCallaktiviert ist ...
            if (registration.ActivationType == ActivationType.SingleCall)
            {
                // Auflistung für Ereignisverdrahtungen erzeugen
                wiringList = new Dictionary<Guid, Delegate>();
                
                // Bei Bedarf Client- und Server-Komponente miteinander verdrahten
                CreateClientServerWires(type, instance, delegateCorrelationSet, wiringList);
            }
            // Transaktionsbereich
            TransactionScope scope = null;

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

                        // InvokeCanceled-Ereignis feuern
                        _host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });

                        // Ausnahme werfen
                        throw ex;
                    }
                }
                // Wenn eine Transaktion übertragen wurde ...
                if (data.Store.ContainsKey("transaction"))
                    // Transaktionsbereich erzeugen
                    scope = new TransactionScope((Transaction)data.Store["transaction"]);
            }
            else
            {
                // Ausnahme erzeugen
                SecurityException ex = new SecurityException(LanguageResource.SecurityException_ContextInfoMissing);

                // InvokeCanceled-Ereignis feuern
                _host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });

                // Ausnahme werfen
                throw ex;
            }
            // Rückgabewert
            object returnValue = null;

            // Typen-Array (zur Ermittlung der passenden Signatur) erzeugen
            Type[] types = new Type[paramDefs.Length];

            // Auflistung der Indizes von Parametern, für die eine Delegatenverdrahtung notwendig ist
            Dictionary<int,DelegateInterceptor> delegateParamIndexes=new Dictionary<int,DelegateInterceptor>();

            // Alle Parametertypen durchlaufen
            for (int i = 0; i < paramDefs.Length; i++)
            {   
                // Typ in Array einfügen
                types[i] = paramDefs[i].ParameterType;

                // Versuchen den aktuellen Parameter in eine Delegaten-Abfangvorrichtung zu casten
                DelegateInterceptor delegateParamInterceptor = args[i] as DelegateInterceptor;

                // Wenn aktuelle Parameter eine Delegaten-Abfangvorrichtung ist ...
                if (delegateParamInterceptor != null)
                    // Parameter der Delegaten-Verdrahtungsliste zufügen
                    delegateParamIndexes.Add(i, delegateParamInterceptor);
            }
            // Ausnahme-Schalter
            bool exceptionThrown = false;

            try
            {
                // Metadaten der aufzurufenden Methode abrufen
                MethodInfo methodInfo = type.GetMethod(methodName, types);

                // Metadaten der Parameter abrufen
                ParameterInfo[] serverMethodParamDefs = methodInfo.GetParameters();

                // Delegaten-Verdrahtungsliste durchlaufen
                foreach(int index in delegateParamIndexes.Keys)
                { 
                    // Abfangvorrichtung adressieren
                    DelegateInterceptor delegateParamInterceptor = delegateParamIndexes[index];

                    // Metadaten des passenden Parameters der Serverkomponenten-Methode adressieren
                    ParameterInfo serverMethodParamDef = serverMethodParamDefs[index];
                    
                    // Dynamischen Draht erzeugen
                    object dynamicWire = DynamicWireFactory.Instance.CreateDynamicWire(type, serverMethodParamDef.ParameterType, delegateParamInterceptor);

                    // Typ des dynamischen Drahtes ermitteln
                    Type dynamicWireType = dynamicWire.GetType();

                    // Dynamischen Draht mit Client-Fernsteuerung verdrahten
                    dynamicWireType.GetProperty("Interceptor").SetValue(dynamicWire, delegateParamInterceptor, null);
                                        
                    // Delegat zu dynamischem Draht erzeugen
                    Delegate dynamicWireDelegate = Delegate.CreateDelegate(serverMethodParamDef.ParameterType, dynamicWire, dynamicWireType.GetMethod("In"));
                    
                    // Abfangvorrichtung durch dynamischen Draht austauschen
                    args[index] = dynamicWireDelegate;
                }            
                // Methode aufrufen
                returnValue = methodInfo.Invoke(instance, args);
            }
            catch (Exception ex)
            {
                // Ausnahme-Schalter setzen
                exceptionThrown = true;

                // InvokeCanceled-Ereignis feuern
                _host.OnInvokeCanceled(new InvokeCanceledEventArgs() { TrackingID = trackingID, CancelException = ex });

                // Ausnahme weiterwerfen
                throw ex;
            }
            finally
            {
                // Wenn ein Transaktionsbereich existiert ...
                if (scope != null)
                {
                    // Wenn keine Ausnahme aufgetreten ist ...
                    if (!exceptionThrown)
                        // Transaktionsbereich abschließen
                        scope.Complete();

                    // Transaktionsbereich entsorgen
                    scope.Dispose();
                }
                // Wenn die Komponente SingleCallaktiviert ist ...
                if (registration.ActivationType == ActivationType.SingleCall)
                    // Verdrahtung aufheben
                    RemoveClientServerWires(type, instance, delegateCorrelationSet, wiringList);
            }
            // Ggf. AfterInvoke-Abos verarbeiten
            ProcessAfterInvoke(trackingID, ref interfaceName, ref delegateCorrelationSet, ref methodName, ref args, ref returnValue);

            // Rückgabewert zurückgeben
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
