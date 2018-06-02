using System.Security.Principal;

namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// Account repository for the SRP-6a protocol implementation.
	/// </summary>
	public interface ISrpAccountRepository
	{
		/// <summary>
		/// Finds the user account data by the given username.
		/// </summary>
		/// <param name="userName">Name of the user.</param>
		ISrpAccount FindByName(string userName);

		/// <summary>
		/// Gets the identity for the given user account.
		/// </summary>
		/// <param name="account">The account.</param>
		IIdentity GetIdentity(ISrpAccount account);
	}
}
