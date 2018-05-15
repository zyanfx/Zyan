
namespace Zyan.Communication.Security
{
	/// <summary>
	/// Interface for the authentication provider.
	/// </summary>
	public interface IAuthenticationProvider
	{
		/// <summary>
		/// Authenticates a specific user based on their credentials.
		/// </summary>
		/// <param name="authRequest">Authentication request message with credentials</param>
		/// <returns>Response message of the authentication system</returns>
		AuthResponseMessage Authenticate(AuthRequestMessage authRequest);
	}
}
