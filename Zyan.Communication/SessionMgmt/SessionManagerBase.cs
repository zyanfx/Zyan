using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Timers;

namespace Zyan.Communication.SessionMgmt
{
	/// <summary>
	/// Abstract base class for <see cref="ISessionManager"/> implementations.
	/// Contains session sweeping logic, <see cref="CreateServerSession"/> utility method and implements <see cref="IDisposable"/> pattern. 
	/// </summary>
	public abstract class SessionManagerBase : ISessionManager
	{
		private object timerLockObject = new object();
		private Timer sessionSweepTimer = null;
		private int sessionSweepInterval;

		/// <summary>
		/// Initializes a <see cref="SessionManagerBase"/> instance with the default values for SessionAgeLimit and SessionSweepInterval.
		/// </summary>
		public SessionManagerBase()
			: this(240, 15)
		{
		}

		/// <summary>
		/// Initializes a <see cref="SessionManagerBase"/> instance.
		/// </summary>
		/// <param name="sessionAgeLimit">Session age limit (in minutes).</param>
		/// <param name="sessionSweepInterval">Session sweep interval (in minutes).</param>
		public SessionManagerBase(int sessionAgeLimit, int sessionSweepInterval)
		{
			IsDisposed = false;
			SessionAgeLimit = sessionAgeLimit;
			SessionSweepInterval = sessionSweepInterval;
		}

		/// <summary>
		/// Creates a new <see cref="ServerSession"/> with the given arguments.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <param name="startTime">Session start time.</param>
		/// <param name="identity"><see cref="IIdentity"/> for the user to associate a new session with.</param>
		/// <returns>New <see cref="ServerSession"/> instance associated with the current <see cref="ISessionManager"/> component.</returns>
		protected ServerSession CreateServerSession(Guid sessionID, DateTime startTime, IIdentity identity)
		{
			return new ServerSession(sessionID, startTime, identity, new SessionVariableAdapter(this, sessionID));
		}

		/// <summary>
		/// Returns true if the instance is disposed.
		/// </summary>
		protected bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer method.
		/// </summary>
		~SessionManagerBase()
		{
			Dispose(false);
		}

		/// <summary>
		/// Release allocated resources. 
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
			IsDisposed = true;
		}

		/// <summary>
		/// Release allocated resources.
		/// </summary>
		/// <param name="disposing">True, if the Dispose() method was called by user code.</param>
		public virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				StopSweepTimer();
			}
		}

		/// <summary>
		/// Gets or sets session age limit (minutes).
		/// </summary>
		public int SessionAgeLimit { get; set; }

		/// <summary>
		/// Gets or sets session cleanup interval (minutes).
		/// </summary>
		public int SessionSweepInterval
		{
			get { return sessionSweepInterval; }
			set
			{
				StopSweepTimer();
				sessionSweepInterval = value;
				StartSweepTimer();
			}
		}

		/// <summary>
		/// Starts session sweeping timer.
		/// </summary>
		protected void StartSweepTimer()
		{
			if (sessionSweepTimer == null)
			{
				lock (timerLockObject)
				{
					if (sessionSweepTimer == null)
					{
						sessionSweepTimer = new Timer(SessionSweepInterval * 60000);
						sessionSweepTimer.Elapsed += (sender, args) => SweepExpiredSessions();
						sessionSweepTimer.Start();
					}
				}
			}
		}

		/// <summary>
		/// Stops session sweeping timer.
		/// </summary>
		private void StopSweepTimer()
		{
			if (sessionSweepTimer != null)
			{
				lock (timerLockObject)
				{
					if (sessionSweepTimer != null)
					{
						if (sessionSweepTimer.Enabled)
							sessionSweepTimer.Stop();

						sessionSweepTimer.Dispose();
						sessionSweepTimer = null;
					}
				}
			}
		}

		/// <summary>
		/// Removes all sessions older than <see cref="SessionAgeLimit"/>.
		/// </summary>
		protected virtual void SweepExpiredSessions()
		{
			foreach (var id in ExpiredSessions)
			{
				RemoveSession(id);
			}
		}

		/// <summary>
		/// Returns list of expired sessions.
		/// </summary>
		protected virtual IEnumerable<Guid> ExpiredSessions
		{
			get
			{
				return
					from s in AllSessions
					where s.Timestamp.ToUniversalTime().AddMinutes(SessionAgeLimit) < DateTime.Now.ToUniversalTime()
					select s.SessionID;
			}
		}

		/// <summary>
		/// Returns list of all sessions.
		/// </summary>
		protected virtual IEnumerable<ServerSession> AllSessions
		{
			get { throw new NotImplementedException(); }
		}

		#region Abstract methods

		/// <summary>
		/// Checks whether the given session exists.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <returns>True if <see cref="ServerSession"/> with the given identifier exists, otherwise, false.</returns>
		public abstract bool ExistSession(Guid sessionID);

		/// <summary>
		/// Returns <see cref="ServerSession"/> identified by sessionID.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <returns><see cref="ServerSession"/> or null, if session with the given identifier is not found.</returns>
		public abstract ServerSession GetSessionBySessionID(Guid sessionID);

		/// <summary>
		/// Stores the given <see cref="ServerSession"/> to the session list.
		/// </summary>
		/// <param name="session"><see cref="ServerSession"/> to store.</param>
		public abstract void StoreSession(ServerSession session);

		/// <summary>
		/// Removes the given session from the session list.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		public abstract void RemoveSession(Guid sessionID);

		/// <summary>
		/// Returns the value of the session variable.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <param name="name">Variable name.</param>
		/// <returns>Value of the given session variable or null, if the variable is not defined.</returns>
		public abstract object GetSessionVariable(Guid sessionID, string name);

		/// <summary>
		/// Sets the new value of the session variable.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</parparam>
		/// <param name="name">Variable name.</param>
		/// <param name="value">Variable value.</param>
		public abstract void SetSessionVariable(Guid sessionID, string name, object value);

		#endregion
	}
}
