using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication
{
    /// <summary>
    /// Proxy für die Kommunikation mit dem Komponentenaufrufer.
    /// </summary>
    internal class ComponentInvokerProxy : RealProxy
    {
        // Felder
        private ZyanConnection _connection = null;
        private IMessageSink _messageSink = null;

        /// <summary>
        /// Erstellt eine neue Instanz von ComponentInvokerProxy.
        /// </summary>
        /// <param name="type">Schnittstelle der entfernten Komponente</param>
        /// <param name="connection">Zyan-Verbindungsobjekt</param>
        public ComponentInvokerProxy(Type type, ZyanConnection connection) : base(type)
        {
            // Wenn kein Typ angegeben wurde ...
            if (type.Equals(null))
                // Ausnahme werfen
                throw new ArgumentNullException("type");

            // Wenn keine Verbindung angegeben wurde ...
            if (connection == null)
                // Ausnahme werfen
                throw new ArgumentNullException("connection");

            // Verbindung übernehmen
            _connection = connection;
        }

        /// <summary>
        /// Sendet eine Nachricht an den Komponentenaufrufer.
        /// </summary>
        /// <param name="message">Remoting-Nachricht mit Details für den entfernten Methodenaufruf</param>
        /// <returns>Remoting Antwortnachricht</returns>
        public override IMessage Invoke(IMessage message)
        { 
            // Wenn noch keine Nachrichtensenke erstellt wurde ...
            if (_messageSink == null)
            {
                // Sendekanal vom Verbindungsobjekt abrufen
                IChannelSender channel = _connection.Channel as IChannelSender;

                // Nachrichtensenke erstellen
                string sinkObjektUri;
                _messageSink = channel.CreateMessageSink(_connection.ServerUrl, null, out sinkObjektUri);
            }
            // Nachricht an Server senden 
            IMessage returnMessage = _messageSink.SyncProcessMessage(message);

            // Antwortnachricht zurückgeben
            return returnMessage;
        }
    }
}
