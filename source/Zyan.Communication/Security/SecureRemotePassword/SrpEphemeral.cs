namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// Ephemeral values used during the SRP authentication.
	/// </summary>
	public class SrpEphemeral
	{
		/// <summary>
		/// Gets or sets the public part.
		/// </summary>
		public string Public { get; set; }

		/// <summary>
		/// Gets or sets the secret part.
		/// </summary>
		public string Secret { get; set; }
	}
}
