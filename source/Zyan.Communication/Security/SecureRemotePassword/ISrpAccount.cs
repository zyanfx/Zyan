namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// SRP-6a account data.
	/// </summary>
	public interface ISrpAccount
	{
		/// <summary>
		/// Gets the name of the user.
		/// </summary>
		string UserName { get; }

		/// <summary>
		/// Gets the salt.
		/// </summary>
		string Salt { get; }

		/// <summary>
		/// Gets the password verifier.
		/// </summary>
		string Verifier { get; }
	}
}
