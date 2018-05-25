using System;
using System.Collections;
using System.Security;

namespace Zyan.Communication.Security
{
	/// <summary>
	/// Authentication credentials used by <see cref="ZyanConnection"/> to authenticate.
	/// </summary>
	public class AuthCredentials
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AuthCredentials"/> class.
		/// </summary>
		/// <param name="credentials">The credentials.</param>
		public AuthCredentials(Hashtable credentials = null)
		{
			CredentialsHashtable = credentials ?? new Hashtable();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthCredentials"/> class.
		/// </summary>
		/// <param name="userName">Name of the user.</param>
		/// <param name="password">The password.</param>
		public AuthCredentials(string userName, string password)
		{
			CredentialsHashtable = new Hashtable
			{
				{ AuthRequestMessage.CREDENTIAL_USERNAME, userName },
				{ AuthRequestMessage.CREDENTIAL_PASSWORD, password },
			};
		}

		/// <summary>
		/// Gets or sets the name of the user.
		/// </summary>
		public virtual string UserName
		{
			get { return (string)CredentialsHashtable[AuthRequestMessage.CREDENTIAL_USERNAME]; }
			set { CredentialsHashtable[AuthRequestMessage.CREDENTIAL_USERNAME] = value; }
		}

		/// <summary>
		/// Gets or sets the password.
		/// </summary>
		public virtual string Password
		{
			get { return (string)CredentialsHashtable[AuthRequestMessage.CREDENTIAL_PASSWORD]; }
			set { CredentialsHashtable[AuthRequestMessage.CREDENTIAL_PASSWORD] = value; }
		}

		/// <summary>
		/// Gets or sets the domain.
		/// </summary>
		public virtual string Domain
		{
			get { return (string)CredentialsHashtable[AuthRequestMessage.CREDENTIAL_DOMAIN]; }
			set { CredentialsHashtable[AuthRequestMessage.CREDENTIAL_DOMAIN] = value; }
		}

		/// <summary>
		/// Gets or sets the Windows security token.
		/// </summary>
		public virtual string WindowsSecurityToken
		{
			get { return (string)CredentialsHashtable[AuthRequestMessage.CREDENTIAL_WINDOWS_SECURITY_TOKEN]; }
			set { CredentialsHashtable[AuthRequestMessage.CREDENTIAL_WINDOWS_SECURITY_TOKEN] = value; }
		}

		/// <summary>
		/// Gets or sets the credentials hashtable.
		/// </summary>
		protected Hashtable CredentialsHashtable { get; set; }

		/// <summary>
		/// Adds the specified name/value pair.
		/// </summary>
		/// <param name="name">The name of the property, like UserName.</param>
		/// <param name="value">The value of the property.</param>
		public virtual void Add(string name, string value)
		{
			CredentialsHashtable = CredentialsHashtable ?? new Hashtable();
			CredentialsHashtable.Add(name, value);
		}

		/// <summary>
		/// Authenticates a specific user based on their credentials.
		/// </summary>
		/// <param name="sessionId">Session identity.</param>
		/// <param name="dispatcher">Remote dispatcher</param>
		public virtual void Authenticate(Guid sessionId, IZyanDispatcher dispatcher)
		{
			var credentials = CredentialsHashtable;
			if (credentials != null && credentials.Count == 0)
				credentials = null;

			var response = dispatcher.Logon(sessionId, credentials);
			if (!response.Completed)
			{
				throw new SecurityException(response.ErrorMessage ?? "Authentication is not completed.");
			}

			// this case is likely to be handled by ZyanDispatcher itself
			if (!response.Success)
			{
				throw new SecurityException(response.ErrorMessage ?? "Authentication is not successful.");
			}
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="Hashtable"/> to <see cref="AuthCredentials"/>.
		/// </summary>
		/// <param name="credentials">The credentials.</param>
		public static implicit operator AuthCredentials(Hashtable credentials) => new AuthCredentials(credentials);

		/// <summary>
		/// Gets a value indicating whether this instance has no credentials specified.
		/// </summary>
		public bool IsEmpty => CredentialsHashtable == null || CredentialsHashtable.Count == 0;
	}
}
