
namespace Zyan.Communication.Security
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
