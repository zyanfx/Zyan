using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication
{
	/// <summary>
	/// Describes arguments for client heartbeat events.
	/// </summary>
	[Serializable]
	public class ClientHeartbeatEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets, when the client heartbeat was received.
		/// </summary>
		public DateTime HeartbeatReceiveTime { get; set; }

		/// <summary>
		/// Gets or sets the session ID of the client, which sent the heartbeat signal.
		/// </summary>
		public Guid SessionID { get; set; }

		/// <summary>
		/// Creates a new instance of the ClientHeartbeatEventArgs class.
		/// </summary>
		public ClientHeartbeatEventArgs()
		{

		}

		/// <summary>
		/// Creates a new instance of the ClientHeartbeatEventArgs class.
		/// </summary>
		/// <param name="receiveTime">Receive time</param>
		/// <param name="sessionID">Client session key</param>
		public ClientHeartbeatEventArgs(DateTime receiveTime, Guid sessionID)
		{
			HeartbeatReceiveTime = receiveTime;
			SessionID = sessionID;
		}
	}
}
