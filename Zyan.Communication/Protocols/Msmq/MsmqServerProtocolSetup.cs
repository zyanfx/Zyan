using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Net.Security;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.Security;

namespace Zyan.Communication.Protocols.Msmq
{
    /// <summary>
    /// Beschreibt serverseitige Einstellungen für MSMQ Kommunkation.
    /// </summary>
    public class MsmqServerProtocolSetup : IServerProtocolSetup
    {
        // Felder
        private string _listenerQueue = string.Empty;
        private string _channelName = string.Empty;        
        private IAuthenticationProvider _authProvider = null;

        /// <summary>
        /// Gibt den Namen der MSMQ Listener Queue zurück, oder legt ihn fest.
        /// </summary>
        public string ListenerQueue
        {
            get { return _listenerQueue; }
            set { _listenerQueue = value; }
        }

        /// <summary>
        /// Erstellt eine neue Instanz von TcpBinaryServerProtocolSetup.
        /// </summary>
        /// <param name="listenerQueue">MSMQ Listener Queue</param>
        public MsmqServerProtocolSetup(string listenerQueue)
        {
            // Wenn Zyan mit mono ausgeführt wird ...
            if (MonoCheck.IsRunningOnMono)
                // Ausnahme werfen
                throw new NotSupportedException();

            // Zufälligen Kanalnamen vergeben
            _channelName = "MsmqServerProtocolSetup_" + Guid.NewGuid().ToString();

            // Portnamen übernehmen
            _listenerQueue = listenerQueue;
        }

        /// <summary>
        /// Erzeugt einen fertig konfigurierten Remoting-Kanal.
        /// <remarks>
        /// Wenn der Kanal in der aktuellen Anwendungsdomäne bereits registriert wurde, wird null zurückgegeben.
        /// </remarks>
        /// </summary>
        /// <returns>Remoting Kanal</returns>
        public IChannel CreateChannel()
        {
            // Kanal suchen
            IChannel channel = ChannelServices.GetChannel(_channelName);

            // Wenn der Kanal nicht gefunden wurde ...
            if (channel == null)
            {
                // Konfiguration für den Kanal erstellen
                System.Collections.IDictionary channelSettings = new System.Collections.Hashtable();
                channelSettings["name"] = _channelName;
                channelSettings["listener"] = _listenerQueue;
                                                
                // Binäre Serialisierung von komplexen Objekten aktivieren
                BinaryServerFormatterSinkProvider serverFormatter = new BinaryServerFormatterSinkProvider();
                serverFormatter.TypeFilterLevel = TypeFilterLevel.Full;
                BinaryClientFormatterSinkProvider clientFormatter = new BinaryClientFormatterSinkProvider();

                // Neuen MSMQ-Kanal erzeugen
                channel = new RKiss.MSMQChannelLib.MSMQReceiver(channelSettings, serverFormatter);

                // Wenn Zyan nicht mit mono ausgeführt wird ...
                if (!MonoCheck.IsRunningOnMono)
                {
                    // Sicherstellen, dass vollständige Ausnahmeinformationen übertragen werden
                    if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
                        RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
                }
                // Kanal zurückgeben
                return channel;
            }
            // Nichts zurückgeben
            return null;
        }

        /// <summary>
        /// Gibt den Authentifizierungsanbieter zurück.
        /// </summary>
        public IAuthenticationProvider AuthenticationProvider
        {
            get
            {
                // Authentifizierungsanbieter zurückgeben
                return _authProvider;
            }
            set
            {
                // Wenn null übergeben wurde ...
                if (value == null)
                    // Null-Authentifizierungsanbieter verwenden
                    _authProvider = new NullAuthenticationProvider();
                else
                    // Angegebenen Authentifizierungsanbieter verwenden
                    _authProvider = value;
            }
        }
    }
}
