using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;

namespace Cyan.Communication.Security
{
    /// <summary>
    /// Antwortnachricht des Authentifizierungs-Systems.
    /// </summary>
    [Serializable]
    public class AuthResponseMessage
    {
        /// <summary>
        /// Gibt zurück, ob die Authentifizierung erfolgreich war, oder nicht.
        /// </summary>
        public bool Success
        { get; set; }

        /// <summary>
        /// Gibt eine Fehlermeldung zurück oder legt sie fest.
        /// </summary>
        public string ErrorMessage
        { get; set; }

        /// <summary>
        /// Gibt die authentifizierte Identität des angemeldeten Benutzers zurück.
        /// </summary>
        public IIdentity AuthenticatedIdentity
        { get; set; }
    }
}
