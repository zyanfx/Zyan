using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication
{
	/// <summary>
	/// Describes arguments for the Disconnected event.
	/// </summary>
	[Serializable]
	public class DisconnectedEventArgs: EventArgs
	{
		/// <summary>
		/// Gets or sets the exception occured on polling.
		/// </summary>
		public Exception Exception { get; set; }

		/// <summary>
		/// Gets or sets whether Zyan should retry to connect.
		/// </summary>
		public bool Retry { get; set; }

		/// <summary>
		/// Gets or sets the number of retry attempts.
		/// </summary>
		public int RetryCount { get; set; }

		/// <summary>
		/// Creates a new instance of the DisconnectedEventArgs class.
		/// </summary>
		public DisconnectedEventArgs()
		{

		}

		/// <summary>
		/// Creates a new instance of the DisconnectedEventArgs class.
		/// </summary>
		public DisconnectedEventArgs(Exception exception)
		{
			this.Exception = exception;
		}
	}
}
