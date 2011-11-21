using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Base class for session-bound event arguments.
	/// </summary>
	[Serializable]
	public class SessionEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SessionEventArgs"/> class.
		/// Session identity is taken from ServerSession.CurrentSession.
		/// </summary>
		public SessionEventArgs()
		{
			SessionID = ServerSession.CurrentSession != null ? ServerSession.CurrentSession.SessionID : Guid.Empty;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionEventArgs"/> class.
		/// </summary>
		/// <param name="sessionId">Target session id (Guid.Empty matches all sessions).</param>
		public SessionEventArgs(Guid sessionId)
		{
			SessionID = sessionId;
		}

		/// <summary>
		/// Gets the session ID.
		/// </summary>
		public Guid SessionID { get; private set; }
	}
}
