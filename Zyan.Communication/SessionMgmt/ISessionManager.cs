using System;

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
		/// Stores the given <see cref="ServerSession"/> to the session list.
		/// </summary>
		/// <param name="session"><see cref="ServerSession"/> to store.</param>
		void StoreSession(ServerSession session);

		/// <summary>
		/// Removes the given session from the session list.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		void RemoveSession(Guid sessionID);

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
