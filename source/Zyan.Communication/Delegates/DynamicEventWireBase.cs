using System;
using System.Reflection;
using Zyan.Communication.SessionMgmt;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Base class for event wires.
	/// </summary>
	internal abstract class DynamicEventWireBase : DynamicWireBase
	{
		/// <summary>
		/// Server component.
		/// </summary>
		public object Component { get; set; }

		/// <summary>
		/// Server component's event descriptor.
		/// </summary>
		public EventInfo ServerEventInfo { get; set; }

		/// <summary>
		/// Session validation handler.
		/// Returns True if client's session is valid, otherwise, False.
		/// </summary>
		public Func<bool> ValidateSession { get; set; }

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

				return Interceptor.InvokeClientDelegate(args);
			} 
			catch
			{
				// unsubscribe
				ServerEventInfo.RemoveEventHandler(Component, InDelegate);
				throw;
			}
		}
	}
}
