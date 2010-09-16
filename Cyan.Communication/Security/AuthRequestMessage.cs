using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cyan.Communication.Security
{
    /// <summary>
    /// Nachricht für Authentifizierungs-Anforderung.
    /// </summary>
    [Serializable]
    public class AuthRequestMessage
    {
        /// <summary>
        /// Konstante für Anmeldeinformation Benutzername.
        /// </summary>
        public const string CREDENTIAL_USERNAME = "UserName";

        /// <summary>
        /// Konstante für Anmeldeinformation Kennwort.
        /// </summary>
        public const string CREDENTIAL_PASSWORD = "Password";

        /// <summary>
        /// Konstante für Anmeldeinformation Windows-Sicherheitstoken.
        /// </summary>
        public const string CREDENTIAL_WINDOWS_SECURITY_TOKEN = "WindowsSecurityToken";
        
        /// <summary>
        /// Gibt die Anmeldeinformationen zurück, oder legt sie fest.
        /// </summary>
        public Hashtable Credentials
        {
            get;
            set;
        }
    }
}
