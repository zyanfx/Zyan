using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Zyan.Communication.Threading;
using Zyan.Communication.Toolbox;
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
		/// Asynchronous event invocation handler.
		/// </summary>
		public Action<WaitCallback> QueueEventInvocation { get; set; }

		/// <summary>
		/// Gets or sets the method to cancel subscription.
		/// </summary>
		public Action<Exception> CancelSubscription { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the subscription is canceled.
		/// </summary>
		public bool Canceled { get; private set; }

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
			if (Canceled)
			{
				return null;
			}

			// legacy events are used by unit tests
			if (ZyanSettings.LegacyBlockingEvents)
			{
				return SyncInvokeClientDelegate(args);
			}

			// by default, events are triggered asynchronously
			QueueEventInvocation?.Invoke(x => SyncInvokeClientDelegate(args));
			return null;
		}

		private object SyncInvokeClientDelegate(params object[] args)
		{
			try
			{
				if (Canceled)
					return null;

				if (ValidateSession != null && !ValidateSession())
					throw new InvalidSessionException();

				var newArgs = args.ToArray();
				if (EventFilter != null && !EventFilter.AllowInvocation(newArgs))
					return null;

				return Interceptor.InvokeClientDelegate(newArgs);
			}
			catch (Exception ex)
			{
				// canceled due to an exception
				Canceled = true;

				// unsubscribe
				if (CancelSubscription != null)
				{
					// skip meaningless TargetInvocationException
					var innerException = ex;
					if (ex is TargetInvocationException)
					{
						innerException = ex.InnerException;
					}

					CancelSubscription(innerException);
				}

				// log diagnostic message
				Trace.WriteLine("Warning! Event subscription is canceled due to exception: {0}", ex);
				return null;
			}
		}
	}
}
