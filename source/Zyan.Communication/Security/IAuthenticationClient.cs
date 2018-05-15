using System;
using System.Collections;

namespace Zyan.Communication.Security
{
	/// <summary>
	/// Interface for the authentication client.
	/// </summary>
	public interface IAuthenticationClient
	{
		/// <summary>
		/// Authenticates a specific user based on their credentials.
		/// </summary>
		/// <param name="sessionId">Session identity.</param>
		/// <param name="credentials">Authentication credentials</param>
		/// <param name="dispatcher">Remote dispatcher</param>
		void Authenticate(Guid sessionId, Hashtable credentials, IZyanDispatcher dispatcher);
	}
}