using System;
using System.Collections;

namespace Zyan.Communication.Security
{
	/// <summary>
	/// Authentication request message.
	/// </summary>
	[Serializable]
	public class AuthRequestMessage
	{
		/// <summary>
		/// User name constant.
		/// </summary>
		public const string CREDENTIAL_USERNAME = "username";

		/// <summary>
		/// Password constant.
		/// </summary>
		public const string CREDENTIAL_PASSWORD = "password";

		/// <summary>
		/// Domain constant.
		/// </summary>
		public const string CREDENTIAL_DOMAIN = "domain";

		/// <summary>
		/// Security token name constant.
		/// </summary>
		public const string CREDENTIAL_WINDOWS_SECURITY_TOKEN = "windowssecuritytoken";

		/// <summary>
		/// Gets or sets user's credentials.
		/// </summary>
		public Hashtable Credentials { get; set; }

		/// <summary>
		/// Gets or sets the IP Address of the calling client.
		/// </summary>
		public string ClientAddress { get; set; }
	}
}
