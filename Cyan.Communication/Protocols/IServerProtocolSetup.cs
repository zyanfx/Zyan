using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using Cyan.Communication.Security;

namespace Cyan.Communication.Protocols
{
    /// <summary>
    /// Beschreibt Protokoll-Einstellungen für die Netzwerkkommunikation.
    /// </summary>
    public interface IServerProtocolSetup
    {
        /// <summary>
        /// Erzeugt einen fertig konfigurierten Remoting-Kanal.
        /// <remarks>
        /// Wenn der Kanal in der aktuellen Anwendungsdomäne bereits registriert wurde, wird null zurückgegeben.
        /// </remarks>
        /// </summary>
        /// <returns>Remoting Kanal</returns>
        IChannel CreateChannel();

        /// <summary>
        /// Gibt den Authentifizierungsanbieter zurück.
        /// </summary>
        IAuthenticationProvider AuthenticationProvider
        {
            get;
        }
    }
}
