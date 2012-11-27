using System;
using System.Security;
using System.Security.Principal;
using System.Threading;

namespace Zyan.Communication.Security
{
	/// <summary>
	/// Authentifizierungsanbieter für Windows-Sicherheitstoken-Authentifizierung.
	/// </summary>
	public class IntegratedWindowsAuthProvider : IAuthenticationProvider
	{
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

			// Windows-identität aus dem aktuellen Thread abrufen
			WindowsIdentity identity = Thread.CurrentPrincipal.Identity as WindowsIdentity;

			// Wenn kein Token angegeben wurde ...
			if (identity == null)
				// Ausnahme werfen
				throw new SecurityException(LanguageResource.SecurityException_WindowsTokenMissing);

			// Wenn die Windows-Identität nicht authentifiziert ist ...
			if (!identity.IsAuthenticated)
				// Authentifizierungsanfrage ablehnen
				return new AuthResponseMessage()
				{
					Success = false,
					ErrorMessage = LanguageResource.UserNotAuthenicatedByWindows,
					AuthenticatedIdentity = null
				};

			// Wenn es sich um einen Gast-Benutzer handelt ...
			if (identity.IsGuest || identity.IsAnonymous)
				// Authentifizierungsanfrage ablehnen
				return new AuthResponseMessage()
				{
					Success = false,
					ErrorMessage = "Anonyme Anmeldung und Gastzugriffe sind untersagt.",
					AuthenticatedIdentity = null
				};

			// Erfolgsmeldung zurückgeben
			return new AuthResponseMessage()
			{
				Success = true,
				ErrorMessage = string.Empty,
				AuthenticatedIdentity = new GenericIdentity(identity.Name)
			};
		}
	}
}
