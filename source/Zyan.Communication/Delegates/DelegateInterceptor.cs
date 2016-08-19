using System;
using System.Threading;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Interception fixture for remote delegate invocation.
	/// </summary>
	public class DelegateInterceptor : DisposableMarshalByRefObject
	{
		/// <summary>
		/// Creates a new instance of the DelegateInterceptor class.
		/// </summary>
		public DelegateInterceptor()
		{
		}

		/// <summary>
		/// Gets or sets the client delegate.
		/// </summary>
		public object ClientDelegate { get; set; }

		/// <summary>
		/// Gets or sets the synchronization context of the client delegate.
		/// </summary>
		public SynchronizationContext SynchronizationContext { get; set; }

		/// <summary>
		/// Invokes the wired client delegate.
		/// </summary>
		/// <param name="args">Parameters</param>
		public object InvokeClientDelegate(params object[] args)
		{
			try
			{
				// check if the delegate is initialized and not garbage collected
				var clientDelegate = ClientDelegate as Delegate;
				if (clientDelegate == null)
				{
					return null;
				}

				// execute as is
				if (SynchronizationContext == null)
				{
					return clientDelegate.DynamicInvoke(args);
				}

				// use synchronization context if specified
				object result = null;
				SynchronizationContext.Send(x =>
				{
					result = clientDelegate.DynamicInvoke(args);
				}, null);
				return result;
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Client delegate throws exception: {0}", ex);
				if (ZyanSettings.LegacyUnprotectedEventHandlers)
				{
					throw;
				}
			}

			return null;
		}
	}
}
