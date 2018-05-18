using System;
using System.Security.Principal;
using System.Security;
using System.Collections;

namespace Zyan.Communication.Security
{
	/// <summary>
	/// Reply message of the authentication system.
	/// </summary>
	[Serializable]
	public class AuthResponseMessage
	{
		/// <summary>
		/// Gets or sets a value indicating whether the authentication procedure is completed.
		/// </summary>
		public bool Completed { get; set; } = true;

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

		/// <summary>
		/// Gets or sets the additional parameters.
		/// </summary>
		public Hashtable Parameters { get; set; }

		/// <summary>
		/// Adds a parameter.
		/// </summary>
		/// <param name="name">Parameter name.</param>
		/// <param name="value">Parameter value.</param>
		public void AddParameter(string name, string value)
		{
			Parameters = Parameters ?? new Hashtable();
			Parameters.Add(name, value);
		}
	}
}
