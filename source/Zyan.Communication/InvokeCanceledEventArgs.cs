using System;

namespace Zyan.Communication
{
	/// <summary>
	/// Arguments for the InvokeCanceled event.
	/// </summary>
	public class InvokeCanceledEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets a unique ID for call tracking.
		/// </summary>
		public Guid TrackingID { get; set; }

		/// <summary>
		/// Gets or sets the exception in case of cancellation.
		/// </summary>
		public Exception CancelException { get; set; }
	}
}
