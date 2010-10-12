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

namespace Zyan.Communication.Protocols.Ipc
{
    /// <summary>
    /// Beschreibt clientseitige Einstellungen für binäre IPC Kommunkation (über Named Pipes).
    /// </summary>
    public class IpcBinaryClientProtocolSetup : IClientProtocolSetup
    {
        // Felder
        private string _channelName=string.Empty;
        private bool _useWindowsSecurity=false;
        private TokenImpersonationLevel _impersonationLevel=TokenImpersonationLevel.Identification;
        private ProtectionLevel _protectionLevel=ProtectionLevel.EncryptAndSign;
        
        /// <summary>
        /// Gibt zurück, ob integrierte Windows-Sicherheit verwendet werden soll, oder legt dies fest.
        /// </summary>
        public bool UseWindowsSecurity
        {
            get { return _useWindowsSecurity; }
            set { _useWindowsSecurity = value; }
        }

        /// <summary>
        /// Gibt die Impersonierungsstufe zurück, oder legt sie fest.
        /// </summary>
        public TokenImpersonationLevel ImpersonationLevel
        {
            get { return _impersonationLevel; }
            set { _impersonationLevel = value; }
        }

        /// <summary>
        /// Gibt den Absicherungsgrad zurück, oder legt ihn fest.
        /// </summary>
        public ProtectionLevel ProtectionLevel
        {
            get { return _protectionLevel; }
            set { _protectionLevel = value; }
        }

        /// <summary>
        /// Erstellt eine neue Instanz von IpcBinaryClientProtocolSetup.
        /// </summary>
        public IpcBinaryClientProtocolSetup ()
	    {
            // Zufälligen Kanalnamen vergeben
            _channelName = "IpcWindowsSecuredClientProtocol_" + Guid.NewGuid().ToString();
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
                // Konfiguration für den TCP-Kanal erstellen
                System.Collections.IDictionary channelSettings = new System.Collections.Hashtable();
                channelSettings["name"] = _channelName;
                channelSettings["portName"] = "zyan_" + Guid.NewGuid().ToString();
                channelSettings["secure"] = _useWindowsSecurity;

                // Wenn Sicherheit aktiviert ist ...
                if (_useWindowsSecurity)
                {
                    // Impersonierung entsprechend der Einstellung aktivieren oder deaktivieren
                    channelSettings["tokenImpersonationLevel"] = _impersonationLevel;

                    // Signatur und Verschlüssung explizit aktivieren
                    channelSettings["protectionLevel"] = _protectionLevel;
                }
                // Binäre Serialisierung von komplexen Objekten aktivieren
                BinaryServerFormatterSinkProvider serverFormatter = new BinaryServerFormatterSinkProvider();
                serverFormatter.TypeFilterLevel = TypeFilterLevel.Full;
                BinaryClientFormatterSinkProvider clientFormatter = new BinaryClientFormatterSinkProvider();

                // Neuen IPC-Kanal erzeugen
                channel = new IpcChannel(channelSettings, clientFormatter, serverFormatter);

                // Sicherstellen, dass vollständige Ausnahmeinformationen übertragen werden
                if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
                    RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

                // Kanal zurückgeben
                return channel;
            }
            // Nichts zurückgeben
            return null;
        }
    }
}
