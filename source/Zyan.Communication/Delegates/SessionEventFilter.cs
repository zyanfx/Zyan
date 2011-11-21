using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Event filter for session-bound events.
	/// </summary>
	[Serializable]
	public class SessionEventFilter : EventFilterBase<SessionEventArgs>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SessionEventFilter"/> class.
		/// </summary>
		/// <param name="sessionId">Session identity.</param>
		public SessionEventFilter(Guid sessionId)
		{
			SessionID = sessionId;
		}

		/// <summary>
		/// Gets the session identity for this event filter.
		/// </summary>
		public Guid SessionID { get; private set; }

		/// <summary>
		/// Returns true if <see cref="SessionEventArgs"/> has the same session identity as the current server session.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="args">The <see cref="Zyan.Communication.Delegates.SessionEventArgs"/> instance containing the event data.</param>
		/// <returns>
		///   <c>true</c> if invocation is allowed; otherwise, <c>false</c>.
		/// </returns>
		protected override bool AllowInvocation(object sender, SessionEventArgs args)
		{
			// check event arguments
			if (args == null || args.SessionID == Guid.Empty)
			{
				return true;
			}

			// compare session id
			return args.SessionID == SessionID;
		}
	}
}
