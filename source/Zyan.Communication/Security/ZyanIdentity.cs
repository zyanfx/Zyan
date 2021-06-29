using System;
using System.Security.Principal;

namespace Zyan.Communication.Security
{
	/// <summary>
	/// OS independend user identity.
	/// </summary>
	[Serializable]
	public class ZyanIdentity : IIdentity
	{
		/// <summary>
		/// Creates a new instance of the ZyanIdentity class.
		/// </summary>
		/// <param name="name">User name</param>
		/// <param name="authenticationType">Type of authentication used to identify the user</param>
		/// <param name="isAuthenticated">Indicates whether the user has been authenticated</param>
		public ZyanIdentity(string name, string authenticationType = "", bool isAuthenticated = false)
		{
			Name = name;
			AuthenticationType = authenticationType;
			IsAuthenticated = isAuthenticated;
		}
		
		/// <summary>Gets the type of authentication used.</summary>
		/// <returns>The type of authentication used to identify the user.</returns>
		public string AuthenticationType { get; }
		
		/// <summary>Gets a value that indicates whether the user has been authenticated.</summary>
		/// <returns>true if the user was authenticated; otherwise, false.</returns>
		public bool IsAuthenticated { get; }
		
		/// <summary>Gets the name of the current user.</summary>
		/// <returns>The name of the user on whose behalf the code is running.</returns>
		public string Name { get; }
	}
}