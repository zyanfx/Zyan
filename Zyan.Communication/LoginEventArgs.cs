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
		/// <summary>
		/// Gets or sets <see cref="IIdentity"/> object.
		/// </summary>
		public IIdentity Identity
		{ get; set; }

		/// <summary>
		/// Gets or sets connected client's address, for example, <see cref="System.Net.IPAddress"/>.
		/// </summary>
		public string ClientAddress
		{ get; set; }

		/// <summary>
		/// Gets or sets event time stamp.
		/// </summary>
		public DateTime Timestamp
		{ get; set; }

		/// <summary>
		/// Gets or sets <see cref="LoginEventType"/> (Login or Logoff).
		/// </summary>
		public LoginEventType EventType
		{ get; set; }

		/// <summary>
		/// Initializes LoginEventArgs instance.
		/// </summary>
		/// <param name="eventType">Login event type.</param>
		/// <param name="identity">Client's identity.</param>
		/// <param name="clientAddress">Client's address.</param>
		/// <param name="timestamp">Event timestamp.</param>
		public LoginEventArgs(LoginEventType eventType, IIdentity identity, string clientAddress, DateTime timestamp)
		{
			EventType = eventType;
			Identity = identity;
			ClientAddress = clientAddress;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Initializes LoginEventArgs instance.
		/// </summary>
		/// <param name="eventType">Login event type.</param>
		/// <param name="identity">Client's identity.</param>
		/// <param name="timestamp">Event timestamp.</param>
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
