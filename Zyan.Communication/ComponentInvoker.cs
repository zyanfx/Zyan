using System;
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
using System.Dynamic;
using Zyan.Communication.Security;

namespace Zyan.Communication
{
    /// <summary>
    /// Allgemeiner Wrapper für eine verteilte Komponente.
    /// Nimmt Remoting-Aufrufe für eine bestimmte Komponente entgegen und
    /// leitet sie lokal an die Komponente weiter.
    /// </summary>
    public class ComponentInvoker : MarshalByRefObject, IComponentInvoker
    {
        // Felder
        private ZyanComponentHost _host = null;

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="host">Komponentenhost</param>
        public ComponentInvoker(ZyanComponentHost host)
        {
            // Wenn kein Komponentenhost übergeben wurde ...
            if (host == null)
                // Ausnahme werfen
                throw new ArgumentNullException("host");

            // Host übernehmen
            _host = host;
        }

        /// <summary>
        /// Erstellt Drähte zwischen Client- und Server-Komponente (wenn im Korrelationssatz angegeben).
        /// </summary>
        /// <param name="type">Implementierungstyp der Server-Komponente</param>
        /// <param name="instance">Instanz der Serverkomponente</param>
        /// <param name="outputPinCorrelationSet">Korrelationssatz mit Verdrahtungsinformationen</param>
        private void CreateClientServerWires(Type type, object instance, ArrayList outputPinCorrelationSet)
        {
            // Wenn kein Korrelationssatz angegeben wurde ...
            if (outputPinCorrelationSet == null)
                // Prozedur abbrechen
                return;

            // Alle Einträge des Korrelationssatzes durchlaufen
            foreach (RemoteOutputPinWiring correlationInfo in outputPinCorrelationSet)
            {
                // Dynamischen Draht erzeugen
                object dynamicWire = DynamicWireFactory.Instance.CreateDynamicWire(type, correlationInfo.ServerPropertyName);

                // Typ des dynamischen Drahtes ermitteln
                Type dynamicWireType = dynamicWire.GetType();

                // Dynamischen Draht mit Client-Fernsteuerung verdrahten
                dynamicWireType.GetProperty("ClientPinWiring").SetValue(dynamicWire, correlationInfo, null);

                // Metadaten des aktuellen Ausgabe-Pins abufen                
                PropertyInfo outputPinMetaData = type.GetProperty(correlationInfo.ServerPropertyName);

                // Delegat zu dynamischem Draht erzeugen
                Delegate dynamicWireDelegate = Delegate.CreateDelegate(outputPinMetaData.PropertyType, dynamicWire, dynamicWireType.GetMethod("In"));

                // Ausgehende Nachrichten mit passender Abfangvorrichtung verdrahten 
                outputPinMetaData.SetValue(instance, dynamicWireDelegate, null);
            }            
        }

        /// <summary>
        /// Entfernt Drähte zwischen Client- und Server-Komponente (wenn im Korrelationssatz angegeben).
        /// </summary>
        /// <param name="type">Implementierungstyp der Server-Komponente</param>
        /// <param name="instance">Instanz der Serverkomponente</param>
        /// <param name="outputPinCorrelationSet">Korrelationssatz mit Verdrahtungsinformationen</param>
        private void RemoveClientServerWires(Type type, object instance, ArrayList outputPinCorrelationSet)
        {
            // Wenn kein Korrelationssatz angegeben wurde ...
            if (outputPinCorrelationSet == null)
                // Prozedur abbrechen
                return;

            // Alle Einträge des Korrelationssatzes durchlaufen
            foreach (RemoteOutputPinWiring correlationInfo in outputPinCorrelationSet)
            {
                // Metadaten des aktuellen Ausgabe-Pins abufen                
                PropertyInfo outputPinMetaData = type.GetProperty(correlationInfo.ServerPropertyName);

                // Verdrahtung aufheben
                outputPinMetaData.SetValue(instance, null, null);
            }
        }

        /// <summary>
        /// Verarbeitet BeforeInvoke-Abos (falls welche registriert sind).
        /// </summary>
        /// <param name="trackingID">Aufrufschlüssel zur Nachverfolgung</param>
        /// <param name="interfaceName">Name der Komponentenschnittstelle</param>
        /// <param name="outputPinCorrelationSet">Korrelationssatz für die Verdrahtung bestimmter Ausgangs-Pins mit entfernten Methoden</param>
        /// <param name="methodName">Methodenname</param>
        /// <param name="args">Parameter</param>   
        private void ProcessBeforeInvoke(Guid trackingID, ref string interfaceName,  ref ArrayList outputPinCorrelationSet, ref string methodName, ref object[] args)
        {
            // Wenn BeforeInvoke-Abos vorhanden sind ...
            if (_host.HasBeforeInvokeSubscriptions())
            {
                // Ereignisargumente für BeforeInvoke erstellen
                BeforeInvokeEventArgs cancelArgs = new BeforeInvokeEventArgs()
                {
                    TrackingID = trackingID,
                    InterfaceName = interfaceName,
                    OutputPinCorrelationSet = outputPinCorrelationSet,
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
                    outputPinCorrelationSet = cancelArgs.OutputPinCorrelationSet;
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
        /// <param name="outputPinCorrelationSet">Korrelationssatz für die Verdrahtung bestimmter Ausgangs-Pins mit entfernten Methoden</param>
        /// <param name="methodName">Methodenname</param>
        /// <param name="args">Parameter</param>   
        /// <param name="returnValue">Rückgabewert</param>
        private void ProcessAfterInvoke(Guid trackingID, ref string interfaceName, ref ArrayList outputPinCorrelationSet, ref string methodName, ref object[] args, ref object returnValue)
        {
            // Wenn AfterInvoke-Abos registriert sind ...
            if (_host.HasAfterInvokeSubscriptions())
            {
                // Ereignisargumente für AfterInvoke erstellen
                AfterInvokeEventArgs afterInvokeArgs = new AfterInvokeEventArgs()
                {
                    TrackingID = trackingID,
                    InterfaceName = interfaceName,
                    OutputPinCorrelationSet = outputPinCorrelationSet,
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
        /// <param name="outputPinCorrelationSet">Korrelationssatz für die Verdrahtung bestimmter Ausgangs-Pins mit entfernten Methoden</param>
        /// <param name="methodName">Methodenname</param>
        /// <param name="args">Parameter</param>        
        /// <returns>Rückgabewert</returns>
        public object Invoke(Guid trackingID, string interfaceName, ArrayList outputPinCorrelationSet, string methodName, params object[] args)
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
            ProcessBeforeInvoke(trackingID, ref interfaceName, ref outputPinCorrelationSet, ref methodName, ref args);
            
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

            // Bei Bedarf Client- und Server-Komponente miteinander verdrahten
            CreateClientServerWires(type, instance, outputPinCorrelationSet);            

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
            Type[] types = new Type[args.Length];

            // Alle Parameter durchlaufen
            for (int i = 0; i < args.Length; i++)
            {
                // Typ in Array einfügen
                types[i] = args[i].GetType();
            }
            // Ausnahme-Schalter
            bool exceptionThrown = false;

            try
            {
                // Methode aufrufen
                returnValue = type.GetMethod(methodName, types).Invoke(instance, args);
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
                // Verdrahtung aufheben
                RemoveClientServerWires(type, instance, outputPinCorrelationSet);
            }
            // Ggf. AfterInvoke-Abos verarbeiten
            ProcessAfterInvoke(trackingID, ref interfaceName, ref outputPinCorrelationSet, ref methodName, ref args, ref returnValue);
            
            // Rückgabewert zurückgeben
            return returnValue;
        }

        /// <summary>
        /// Gibt eine Liste mit allen registrierten Komponenten zurück.
        /// </summary>
        /// <returns>Liste mit Namen der registrierten Komponenten</returns>
        public string[] GetRegisteredComponents()
        {
            // Daten vom Host abrufen
            return _host.GetRegisteredComponents().ToArray();
        }

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

                // Neue Sitzung erstellen
                ServerSession session = new ServerSession(sessionID, authResponse.AuthenticatedIdentity);

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

    public class Dummy
    {
        public RemoteOutputPinWiring ClientPinWiring { get; set; }

        public decimal In(decimal p1,decimal p2)
        {
            return (decimal)ClientPinWiring.InvokeDynamicClientPin(p1,p2);
        }
    }
}
