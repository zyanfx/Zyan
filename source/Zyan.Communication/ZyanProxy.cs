using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Reflection;
using System.Transactions;
using System.Threading.Tasks;

namespace Zyan.Communication
{
    /// <summary>
    /// Stellvertreterobjekt für den Zugriff auf eine entfernte Komponente.
    /// </summary>
    public class ZyanProxy : RealProxy
    {
        // Felder
        private Type _interfaceType = null;
        private IComponentInvoker _remoteInvoker = null;
        private ArrayList _outputMessageCorrelationSet = null;
        private bool _implicitTransactionTransfer = false;
        private Guid _sessionID;
        private string _componentHostName = string.Empty;
        private bool _autoLoginOnExpiredSession = false;
        private Hashtable _autoLoginCredentials = null;
        private ZyanConnection _connection = null;
               
        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="type">Schnittstelle der entfernten Komponente</param>
        /// <param name="connection">Verbindungsobjekt</param>
        /// <param name="remoteInvoker">Aufrufer für entfernte Komponenten</param>   
        /// <param name="implicitTransactionTransfer">Implizite Transaktionsübertragung</param>
        /// <param name="autoLoginOnExpiredSession">Gibt an, ob sich der Proxy automatisch neu anmelden soll, wenn die Sitzung abgelaufen ist</param>
        /// <param name="autoLogoninCredentials">Optional! Anmeldeinformationen, die nur benötigt werden, wenn autoLoginOnExpiredSession auf Wahr eingestellt ist</param>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        public ZyanProxy(Type type, ZyanConnection connection, bool implicitTransactionTransfer, Guid sessionID, string componentHostName, bool autoLoginOnExpiredSession, Hashtable autoLogoninCredentials)
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

            // Sitzungsschlüssel übernehmen
            _sessionID = sessionID;

            // Verbindungsobjekt übernehmen
            _connection = connection;
            
            // Name des Komponentenhosts übernehmen
            _componentHostName = componentHostName;

            // Schnittstellentyp übernehmen
            _interfaceType = type;

            // Aufrufer von Verbindung übernehmen
            _remoteInvoker = _connection.RemoteComponentFactory;
         
            // Schalter für implizite Transaktionsübertragung übernehmen
            _implicitTransactionTransfer = implicitTransactionTransfer;

            // Schalter für automatische Anmeldung bei abgelaufender Sitzung übernehmen
            _autoLoginOnExpiredSession = autoLoginOnExpiredSession;

            // Wenn automatische Anmeldung aktiv ist ...
            if (_autoLoginOnExpiredSession)
                // Anmeldeinformationen speichern
                _autoLoginCredentials = autoLogoninCredentials;

            // Sammlung für Korrelationssatz erzeugen
            _outputMessageCorrelationSet = new ArrayList();            
        }

        /// <summary>
        /// Entfernte Methode aufrufen.
        /// </summary>
        /// <param name="message">Remoting-Nachricht mit Details für den entfernten Methodenaufruf</param>
        /// <returns>Remoting Antwortnachricht</returns>
        public override IMessage Invoke(IMessage message)
        {
            // Wenn keine Nachricht angegeben wurde ...
            if (message == null)
                // Ausnahme werfen
                throw new ArgumentNullException("message");

            // Nachricht in benötigte Schnittstelle casten
            IMethodCallMessage methodCallMessage = (IMethodCallMessage)message;

            // Methoden-Metadaten abrufen
            MethodInfo methodInfo = (MethodInfo)methodCallMessage.MethodBase;

            // Wenn die Methode ein Ausgabe-Pin ist ...
            if (methodInfo.ReturnType.Equals(typeof(void)) && 
                methodCallMessage.InArgCount==1 &&
                methodCallMessage.ArgCount==1 &&
                methodCallMessage.Args[0]!=null &&
                typeof(Delegate).IsAssignableFrom(methodCallMessage.Args[0].GetType()) &&
                methodCallMessage.MethodName.StartsWith("set_")) 
            {
                // EBC-Eingangsnachricht abrufen
                object inputMessage = methodCallMessage.GetArg(0);

                // "set_" wegschneiden
                string propertyName = methodCallMessage.MethodName.Substring(4);

                // Verdrahtungskonfiguration festschreiben
                RemoteOutputPinWiring wiring = new RemoteOutputPinWiring()
                {
                    ClientReceiver = inputMessage,
                    ServerPropertyName = propertyName,                    
                };
                // Verdrahtung in der Sammlung ablegen
                _outputMessageCorrelationSet.Add(wiring);
                
                // Leere Remoting-Antwortnachricht erstellen und zurückgeben
                return new ReturnMessage(null, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
            }
            else
            {
                // Transferobjekt für Kontextdaten erzeugen, die implizit übertragen werden sollen 
                LogicalCallContextData data = new LogicalCallContextData();

                // Sitzungsschlüssel im Transferobjekt ablegen
                data.Store.Add("sessionid", _sessionID);

                // Wenn eine Umgebungstransaktion aktiv ist die implizite Transaktionsübertragung eingeschaltet ist ...
                if (_implicitTransactionTransfer && Transaction.Current != null)
                {
                    // Umgebungstransaktion abrufen
                    Transaction transaction = Transaction.Current;

                    // Wenn die Transaktion noch aktiv ist ...
                    if (transaction.TransactionInformation.Status == TransactionStatus.InDoubt || 
                        transaction.TransactionInformation.Status == TransactionStatus.Active)
                    {
                        // Transaktion im Transferobjekt ablegen                        
                        data.Store.Add("transaction",transaction);                        
                    }
                }
                // Transferobjekt in den Aufrufkontext einhängen
                CallContext.SetData("__ZyanContextData_" + _componentHostName, data);

                // Entfernten Methodenaufruf durchführen und jede Antwortnachricht sofort über einen Rückkanal empfangen
                return InvokeRemoteMethod(methodCallMessage);                
            }
        }

        /// <summary>
        /// Führt einen entfernten Methodenaufruf aus.
        /// </summary>
        /// <param name="methodCallMessage">Remoting-Nachricht mit Details für den entfernten Methodenaufruf</param>
        /// <returns>Remoting Antwortnachricht</returns>
        private IMessage InvokeRemoteMethod(IMethodCallMessage methodCallMessage)
        {
            // Aufrufschlüssel vergeben
            Guid trackingID = Guid.NewGuid();

            try
            {                
                // Variable für Rückgabewert
                object returnValue = null;
                
                // Ereignisargumente für BeforeInvoke erstellen
                BeforeInvokeEventArgs cancelArgs = new BeforeInvokeEventArgs()
                {
                    TrackingID = trackingID,
                    InterfaceName = _interfaceType.FullName,
                    OutputPinCorrelationSet = _outputMessageCorrelationSet,
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
                try
                {
                    // Entfernten Methodenaufruf durchführen
                    returnValue = _remoteInvoker.Invoke(trackingID, _interfaceType.FullName, _outputMessageCorrelationSet, methodCallMessage.MethodName, methodCallMessage.Args);
                                                                
                    // Ereignisargumente für AfterInvoke erstellen
                    AfterInvokeEventArgs afterInvokeArgs = new AfterInvokeEventArgs()
                    {
                        TrackingID = trackingID,
                        InterfaceName = _interfaceType.FullName,
                        OutputPinCorrelationSet = _outputMessageCorrelationSet,
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
                        _remoteInvoker.Logon(_sessionID, _autoLoginCredentials);

                        // Entfernten Methodenaufruf erneut versuchen                        
                        returnValue = _remoteInvoker.Invoke(trackingID, _interfaceType.FullName, _outputMessageCorrelationSet, methodCallMessage.MethodName, methodCallMessage.Args);
                    }
                }
                // Remoting-Antwortnachricht erstellen und zurückgeben
                return new ReturnMessage(returnValue, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
            }
            catch (TargetInvocationException targetInvocationException)
            {
                // Aufrufausnahme als Remoting-Nachricht zurückgeben
                return new ReturnMessage(targetInvocationException, methodCallMessage);
            }
            catch (SocketException socketException)
            {
                // TCP-Sockelfehler als Remoting-Nachricht zurückgeben
                return new ReturnMessage(socketException, methodCallMessage);
            }
            catch (InvalidSessionException sessionException)
            {
                // Sitzungsfehler als Remoting-Nachricht zurückgeben
                return new ReturnMessage(sessionException, methodCallMessage);
            }
        }
    }
}

