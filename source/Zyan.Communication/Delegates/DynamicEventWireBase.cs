using System;
using System.Reflection;
using Zyan.Communication.SessionMgmt;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Base class for event wires.
	/// </summary>
	internal abstract class DynamicEventWireBase : DynamicWireBase
	{
		/// <summary>
		/// Session validation handler.
		/// Returns True if client's session is valid, otherwise, False.
		/// </summary>
		public Func<bool> ValidateSession { get; set; }

		/// <summary>
		/// Gets or sets the method to cancel subscription.
		/// </summary>
		public Action CancelSubscription { get; set; }

		/// <summary>
		/// Gets or sets the event filter.
		/// </summary>
		public IEventFilter EventFilter { get; set; }

		/// <summary>
		/// Invokes client event handler.
		/// If the handler throws an exception, event subsription is cancelled.
		/// </summary>
		/// <param name="args">Event handler parameters.</param>
		/// <returns>Event handler return value.</returns>
		protected override object InvokeClientDelegate(params object[] args)
		{
			try
			{
				if (ValidateSession != null && !ValidateSession())
					throw new InvalidSessionException();

				if (EventFilter != null && !EventFilter.AllowInvocation(args))
					return null;

				return Interceptor.InvokeClientDelegate(args);
			}
			catch (Exception ex)
			{
				// unsubscribe
				if (CancelSubscription != null)
				{
					CancelSubscription();
				}

				// log diagnostic message
				Trace.WriteLine("Warning! Event subscription is canceled due to exception: {0}", ex);
				return null;
			}
		}
	}
}
