using System;
using System.Collections;

namespace Zyan.Communication.Security
{
	/// <summary>
	/// Default single-step authentication client.
	/// </summary>
	public class SimpleAuthenticationClient: IAuthenticationClient
	{
		/// <summary>
		/// Authenticates a specific user based on their credentials.
		/// </summary>
		/// <param name="sessionId">Session identity.</param>
		/// <param name="credentials">Authentication credentials</param>
		/// <param name="dispatcher">Remote dispatcher</param>
		public void Authenticate(Guid sessionId, Hashtable credentials, IZyanDispatcher dispatcher)
		{
			dispatcher.Logon(sessionId, credentials);
		}
	}
}