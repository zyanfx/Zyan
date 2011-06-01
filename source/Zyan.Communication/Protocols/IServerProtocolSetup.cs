using System.Runtime.Remoting.Channels;
using Zyan.Communication.Security;

namespace Zyan.Communication.Protocols
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
