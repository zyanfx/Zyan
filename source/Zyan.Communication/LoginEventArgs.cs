using System;
using System.Security.Principal;

namespace Zyan.Communication
{
	/// <summary>
	/// Event arguments for login events.
	/// </summary>
	[Serializable]
	public class LoginEventArgs : EventArgs
	{
		public IIdentity Identity
		{ get; set; }

		public string ClientAddress
		{ get; set; }

		public DateTime Timestamp
		{ get; set; }

		public LoginEventType EventType
		{ get; set; }

		public LoginEventArgs(LoginEventType eventType, IIdentity identity, string clientAddress, DateTime timestamp)
		{
			EventType = eventType;
			Identity = identity;
			ClientAddress = clientAddress;
			Timestamp = timestamp;
		}

		public LoginEventArgs(LoginEventType eventType, IIdentity identity, DateTime timestamp)
		{
			EventType = eventType;
			Identity = identity;
			ClientAddress = string.Empty;
			Timestamp = timestamp;
		}
	}

	/// <summary>
	/// Available types of login events.
	/// </summary>
	public enum LoginEventType : short
	{
		Logon = 1,
		Logoff
	}
}
