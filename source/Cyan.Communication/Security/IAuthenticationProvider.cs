using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;

namespace Cyan.Communication.Security
{
    /// <summary>
    /// Schnittstelle für Authentifzierungs-Anbieter.
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Authentifiziert einen bestimmten Benutzer anhand seiner Anmeldeinformationen.
        /// </summary>
        /// <param name="authRequest">Authentifizierungs-Anfragenachricht mit Anmeldeinformationen</param>
        /// <returns>Antwortnachricht des Authentifizierungssystems</returns>
        AuthResponseMessage Authenticate(AuthRequestMessage authRequest);
    }
}
