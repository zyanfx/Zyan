using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Reflection;
using System.Transactions;

namespace Cyan.Communication
{
    /// <summary>
    /// Stellvertreterobjekt für den Zugriff auf eine entfernte Komponente.
    /// </summary>
    public class CyanProxy : RealProxy
    {
        // Felder
        private Type _interfaceType = null;
        private IComponentInvoker _remoteInvoker = null;
        private ArrayList _outputMessageCorrelationSet = null;
        private bool _implicitTransactionTransfer = false;
        private Guid _sessionID;
               
        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="type">Schnittstelle der entfernten Komponente</param>
        /// <param name="remoteInvoker">Aufrufer für entfernte Komponenten</param>   
        /// <param name="implicitTransactionTransfer">Implizite Transaktionsübertragung</param>
        /// <param name="sessionID">Sitzungsschlüssel</param>
        public CyanProxy(Type type, IComponentInvoker remoteInvoker, bool implicitTransactionTransfer, Guid sessionID)
            : base(type)
        {
            // Wenn kein Typ angegeben wurde ...
            if (type == null)
                // Ausnahme werfen
                throw new ArgumentNullException("type");

            // Wenn kein Wrapper angegeben wurde ...
            if (remoteInvoker == null)
                // Ausnahme werfen
                throw new ArgumentNullException("remoteInvoker");
            
            // Sitzungsschlüssel übernehmen
            _sessionID = sessionID;

            // Schnittstellentyp übernehmen
            _interfaceType = type;

            // Aufrufer übernehmen
            _remoteInvoker = remoteInvoker;   
         
            // Schalter für implizite Transaktionsübertragung übernehmen
            _implicitTransactionTransfer = implicitTransactionTransfer;

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
                CallContext.SetData("__CyanContextData", data);

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
            try
            {                
                // Methodeninformationen ermitteln
                MethodInfo info = (MethodInfo)methodCallMessage.MethodBase;                                   

                // Entfernten Methodenaufruf durchführen
                object returnValue = _remoteInvoker.Invoke(_interfaceType.FullName, _outputMessageCorrelationSet, methodCallMessage.MethodName, methodCallMessage.Args);

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
        }
    }
}

