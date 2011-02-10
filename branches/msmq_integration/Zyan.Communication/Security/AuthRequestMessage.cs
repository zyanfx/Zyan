using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Security
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
        public const string CREDENTIAL_USERNAME = "username";

        /// <summary>
        /// Konstante für Anmeldeinformation Kennwort.
        /// </summary>
        public const string CREDENTIAL_PASSWORD = "password";

        /// <summary>
        /// Konstante für Anmeldeinformation Domäne.
        /// </summary>
        public const string CREDENTIAL_DOMAIN = "domain";

        /// <summary>
        /// Konstante für Anmeldeinformation Windows-Sicherheitstoken.
        /// </summary>
        public const string CREDENTIAL_WINDOWS_SECURITY_TOKEN = "windowssecuritytoken";
        
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
