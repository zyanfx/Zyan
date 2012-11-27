using System;
using System.Security.Principal;
using System.Security;

namespace Zyan.Communication.Security
{
	/// <summary>
	/// Reply message of the authentication system.
	/// </summary>
	[Serializable]
	public class AuthResponseMessage
	{
		/// <summary>
		/// Gets or sets value indicating whether the authentication procedure completed successfully.
		/// </summary>
		public bool Success { get; set; }

		/// <summary>
		/// Gets or sets error message.
		/// </summary>
		public string ErrorMessage { get; set; }

		/// <summary>
		/// Gets or sets authenticated user's identity object.
		/// </summary>
		public IIdentity AuthenticatedIdentity { get; set; }

		/// <summary>
		/// Gets or sets security exception thrown on authentication failure.
		/// </summary>
		public SecurityException Exception { get; set; }
	}
}
