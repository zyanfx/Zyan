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
    /// Beschreibt clientseitige Einstellungen für MSMQ Kommunkation.
    /// </summary>
    public class MsmqClientProtocolSetup : IClientProtocolSetup
    {
        // Felder
        private string _channelName = string.Empty;
        
        /// <summary>
        /// Erstellt eine neue Instanz von IpcBinaryClientProtocolSetup.
        /// </summary>
        public MsmqClientProtocolSetup()
        {
            // Zufälligen Kanalnamen vergeben
            _channelName = "MsmqClientProtocol_" + Guid.NewGuid().ToString();
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
                                
                // Binäre Serialisierung von komplexen Objekten aktivieren
                BinaryServerFormatterSinkProvider serverFormatter = new BinaryServerFormatterSinkProvider();
                serverFormatter.TypeFilterLevel = TypeFilterLevel.Full;
                //BinaryClientFormatterSinkProvider clientFormatter = new BinaryClientFormatterSinkProvider();

                // Neuen MSMQ-Kanal erzeugen
                channel = new RKiss.MSMQChannelLib.MSMQSender(channelSettings, serverFormatter);

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
    }
}
