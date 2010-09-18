using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;

namespace Zyan.Communication.Security
{
    /// <summary>
    /// Null-Authentifizierungsanbieter. 
    /// Wird benötigt, wenn keine Authentifizierung benötigt wird.
    /// </summary>
    public class NullAuthenticationProvider : IAuthenticationProvider
    {
        /// <summary>
        /// Authentifiziert einen bestimmten Benutzer anhand seiner Anmeldeinformationen.
        /// </summary>
        /// <param name="authRequest">Authentifizierungs-Anfragenachricht mit Anmeldeinformationen</param>
        /// <returns>Antwortnachricht des Authentifizierungssystems</returns>
        public AuthResponseMessage Authenticate(AuthRequestMessage authRequest)
        { 
            // Anonyme Identität erstellen
            IIdentity identity = WindowsIdentity.GetAnonymous();

            // Erfolgsmelung zurückgeben
            return new AuthResponseMessage()
            {
                ErrorMessage = string.Empty,
                Success = true,
                AuthenticatedIdentity = identity
            };
        }
    }
}
