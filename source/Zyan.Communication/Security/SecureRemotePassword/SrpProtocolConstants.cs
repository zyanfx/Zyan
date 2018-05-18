namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// SRP-6a protocol constants used by SrpCredentials and SrpAuthenticationProvider.
	/// </summary>
	public class SrpProtocolConstants
	{
		/// <summary>
		/// The SRP step number.
		/// </summary>
		public const string SRP_STEP_NUMBER = "#";

		/// <summary>
		/// The SRP session identity.
		/// </summary>
		public const string SRP_SESSION_ID = "*";

		/// <summary>
		/// The SRP username.
		/// </summary>
		public const string SRP_USERNAME = "I";

		/// <summary>
		/// The SRP salt.
		/// </summary>
		public const string SRP_SALT = "s";

		/// <summary>
		/// The SRP server public ephemeral.
		/// </summary>
		public const string SRP_SERVER_PUBLIC_EPHEMERAL = "B";

		/// <summary>
		/// The SRP client public ephemeral.
		/// </summary>
		public const string SRP_CLIENT_PUBLIC_EPHEMERAL = "A";

		/// <summary>
		/// The SRP client session proof.
		/// </summary>
		public const string SRP_CLIENT_SESSION_PROOF = "M1";

		/// <summary>
		/// The SRP server session proof.
		/// </summary>
		public const string SRP_SERVER_SESSION_PROOF = "M2";
	}
}
