using System;

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
		public object ClientDelegate
		{
			get;
			set;
		}

		/// <summary>
		/// Invokes the wired client delegate.
		/// </summary>
		/// <param name="args">Parameters</param>
		public object InvokeClientDelegate(params object[] args)
		{
			Delegate clientDelegate = (Delegate)ClientDelegate;
			return clientDelegate.DynamicInvoke(args);
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
