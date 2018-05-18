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
	}
}
