using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication
{
	/// <summary>
	/// Describes subscriptions and unsubscriptions.
	/// </summary>
	public class SubscriptionEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets the type of the component.
		/// </summary>
		public Type ComponentType { get; set; }

		/// <summary>
		/// Gets or sets the name of the delegate member.
		/// </summary>
		public string DelegateMemberName { get; set; }

		/// <summary>
		/// Gets or sets the correlation ID.
		/// </summary>
		public Guid CorrelationID { get; set; }

		/// <summary>
		/// Gets or sets the exception which caused the subscription to be canceled.
		/// </summary>
		public Exception Exception { get; set; }
	}
}
