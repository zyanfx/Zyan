using System;
using System.Security.Principal;

namespace Zyan.Communication.SessionMgmt
{
	/// <summary>
	/// Session manager interface.
	/// </summary>
	public interface ISessionManager : IDisposable
	{
		/// <summary>
		/// Gets or sets session age limit (minutes).
		/// </summary>
		int SessionAgeLimit
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets session cleanup interval (minutes).
		/// </summary>
		int SessionSweepInterval
		{
			get;
			set;
		}

		/// <summary>
		/// Checks whether the given session exists.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <returns>True if <see cref="ServerSession"/> with the given identifier exists, otherwise, false.</returns>
		bool ExistSession(Guid sessionID);

		/// <summary>
		/// Returns <see cref="ServerSession"/> identified by sessionID.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <returns><see cref="ServerSession"/> or null, if session with the given identifier is not found.</returns>
		ServerSession GetSessionBySessionID(Guid sessionID);

		/// <summary>
		/// Creates a new <see cref="ServerSession"/> with the given arguments.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <param name="timeStamp">Session time stamp.</param>
		/// <param name="identity"><see cref="IIdentity"/> for the user to associate a new session with.</param>
		/// <returns>New <see cref="ServerSession"/> instance associated with the current <see cref="ISessionManager"/> component.</returns>
		ServerSession CreateServerSession(Guid sessionID, DateTime timeStamp, IIdentity identity);

		/// <summary>
		/// Sets the current server session.
		/// </summary>
		/// <param name="session">The session.</param>
		void SetCurrentSession(ServerSession session);

		/// <summary>
		/// Stores the given <see cref="ServerSession"/> to the session list.
		/// </summary>
		/// <param name="session">The <see cref="ServerSession"/> to store.</param>
		void StoreSession(ServerSession session);

		/// <summary>
		/// Renews the given session.
		/// </summary>
		/// <param name="session">The <see cref="ServerSession"/> to renew.</param>
		void RenewSession(ServerSession session);

		/// <summary>
		/// Removes the given session from the session list.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		void RemoveSession(Guid sessionID);

		/// <summary>
		/// Removes the given session and raises the ClientSessionTerminated event.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		void TerminateSession(Guid sessionID);

		/// <summary>
		/// Occurs when the client session is terminated abnormally.
		/// </summary>
		event EventHandler<LoginEventArgs> ClientSessionTerminated;

		/// <summary>
		/// Returns the value of the session variable.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <param name="name">Variable name.</param>
		/// <returns>Value of the given session variable or null, if the variable is not defined.</returns>
		object GetSessionVariable(Guid sessionID, string name);

		/// <summary>
		/// Sets the new value of the session variable.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <param name="name">Variable name.</param>
		/// <param name="value">Variable value.</param>
		void SetSessionVariable(Guid sessionID, string name, object value);
	}
}
