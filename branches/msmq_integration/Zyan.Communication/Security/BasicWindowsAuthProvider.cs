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
                bool success = WindowsSecurityTools.LogonUser(
                       username,
                       domain,
                       password,
                       WindowsSecurityTools.LogonType.LOGON32_LOGON_NETWORK, 
                       WindowsSecurityTools.ProviderType.LOGON32_PROVIDER_DEFAULT,
                       out token)!=0;

                // Wenn die Anmeldefunktion erfolgreich verarbeitet wurde ...
                if (success && token != IntPtr.Zero)
                {
                    // Authentifizierte Identität abfragen
                    WindowsIdentity identity = new WindowsIdentity(token);

                    // Wahr zurückgeben, wenn die Identität weder ein Gast noch anonym ist
                    return identity.IsAuthenticated && !(identity.IsGuest || identity.IsAnonymous);
                }
                // Falsch zurückgeben
                return false;
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
                throw new SecurityException(LanguageResource.SecurityException_CredentialsMissing);

            // Wenn kein Benutzername angegeben wurde ...
            if (!authRequest.Credentials.ContainsKey(AuthRequestMessage.CREDENTIAL_USERNAME))
                // Ausnahme werfen
                throw new SecurityException(LanguageResource.SecurityException_UserNameMissing);

            // Wenn kein Passwort angegeben wurde ...
            if (!authRequest.Credentials.ContainsKey(AuthRequestMessage.CREDENTIAL_PASSWORD))
                // Ausnahme werfen
                throw new SecurityException(LanguageResource.SecurityException_PasswordMissing);
            
            // Benutzer und Kennwort lesen
            string userName = authRequest.Credentials[AuthRequestMessage.CREDENTIAL_USERNAME] as string;
            string password = authRequest.Credentials[AuthRequestMessage.CREDENTIAL_PASSWORD] as string;
            
            // Variable für Domäne
            string domain = ".";

            // Wenn eine Domäne angegeben wurde ...
            if (authRequest.Credentials.ContainsKey(AuthRequestMessage.CREDENTIAL_DOMAIN))
                // Domäne übernehmen
                domain = authRequest.Credentials[AuthRequestMessage.CREDENTIAL_DOMAIN] as string;

            // Wenn der Benutzer bekannt ist und das Kennwort stimmt ...
            if (ValidateWindowsCredentials(userName,password,domain))              
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
                ErrorMessage = LanguageResource.InvalidCredentials,
                AuthenticatedIdentity = null
            };
        }
    }
}
