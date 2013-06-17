using System;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Interception fixture for remote delegate invocation.
	/// </summary>
	public class DelegateInterceptor : MarshalByRefObject
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
		/// Invokes the wired client delegate.
		/// </summary>
		/// <param name="args">Parameters</param>
		public object InvokeClientDelegate(params object[] args)
		{
			try
			{
				Delegate clientDelegate = (Delegate)ClientDelegate;
				return clientDelegate.DynamicInvoke(args);
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

		/// <summary>
		/// Ensures unlimited Remoting lifetime.
		/// </summary>
		/// <returns>Always null</returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
