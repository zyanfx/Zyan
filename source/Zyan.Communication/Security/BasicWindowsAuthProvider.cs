using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;

namespace Zyan.Communication.Security
{
    /// <summary>
    /// Authentifizierungsanbieter für einfache Authentifizierung mit Benutzername und Passwort im Klartext.
    /// </summary>
    public class BasicWindowsAuthProvider : IAuthenticationProvider
    {
        /// <summary>
        /// Überprüft Windows-Anmeldeinformationen.
        /// </summary>
        /// <param name="username">Windows-Benutzername</param>
        /// <param name="password">Windows-Kennwort</param>
        /// <param name="domain">Windows-Computername oder Active Directory-Domäne</param>
        /// <returns>Wahr, wenn die Anmeldung erflgreich war, ansonsten Falsch</returns>
        private bool ValidateWindowsCredentials(string username, string password, string domain)
        {
            // Variable für Windows-Sicherheitstoken
            IntPtr token = IntPtr.Zero;

            try
            {
                // Windows-Anmeldung durchführen
                WindowsSecurityTools.LogonUser(
                    username,
                    domain,
                    password,
                    WindowsSecurityTools.LogonType.LOGON32_LOGON_NETWORK, WindowsSecurityTools.ProviderType.LOGON32_PROVIDER_DEFAULT,
                    out token);

                // Falsch zurückgeben, wenn kein Windows-Sicherheitstoken erzeugt wurde
                return token != IntPtr.Zero;
            }
            finally
            {
                // Unverwalteten Hande auf den Sicherheitstoken schließen
                WindowsSecurityTools.CloseHandle(token);
            }
        }

        /// <summary>
        /// Authentifiziert einen bestimmten Benutzer anhand seiner Anmeldeinformationen.
        /// </summary>
        /// <param name="authRequest">Authentifizierungs-Anfragenachricht mit Anmeldeinformationen</param>
        /// <returns>Antwortnachricht des Authentifizierungssystems</returns>
        public AuthResponseMessage Authenticate(AuthRequestMessage authRequest)
        {
            // Wenn keine Nachricht angegeben wurde ...
            if (authRequest == null)
                // Ausnahme werfen
                throw new ArgumentNullException("authRequest");
            
            // Wenn keine Anmeldeinformationen übergeben wurden ...
            if (authRequest.Credentials==null)
                // Ausnahme werfen
                throw new SecurityException("Es wurden keine Anmeldeinformationen angegeben.");

            // Wenn kein Benutzername angegeben wurde ...
            if (!authRequest.Credentials.ContainsKey("username"))
                // Ausnahme werfen
                throw new SecurityException("Kein Benutzername angegben.");

            // Wenn kein Passwort angegeben wurde ...
            if (!authRequest.Credentials.ContainsKey("password"))
                // Ausnahme werfen
                throw new SecurityException("Kein Kennwort angegben.");
            
            // Benutzer und Kennwort lesen
            string userName = authRequest.Credentials["username"] as string;
            string password = authRequest.Credentials["password"] as string;

            // Wenn der Benutzer bekannt ist und das Kennwort stimmt ...
            if (ValidateWindowsCredentials(userName,password,string.Empty))              
                // Erfolgsmeldung zurückgeben
                return new AuthResponseMessage()
                {
                    Success = true,
                    ErrorMessage = string.Empty,
                    AuthenticatedIdentity = new GenericIdentity(userName)
                };

            // Fehlermeldung zurückgeben
            return new AuthResponseMessage()
            {
                Success = false,
                ErrorMessage = "Benutzername und/oder Kennwort sind nicht korrekt.",
                AuthenticatedIdentity = null
            };
        }
    }
}
