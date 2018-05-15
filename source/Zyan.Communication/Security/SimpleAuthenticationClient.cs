using System;
using System.Collections;
using System.Security;

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
	}
}