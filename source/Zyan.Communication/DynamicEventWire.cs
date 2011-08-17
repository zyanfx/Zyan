using System;
using System.Reflection;

namespace Zyan.Communication
{
	/// <summary>
	/// Strongly typed event handler wrapper for DelegateInterceptor.
	/// </summary>
	public class DynamicEventWire<T> : DynamicWire<T>
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
		/// Invokes client event handler.
		/// If the handler throws an exception, event subsription is cancelled.
		/// </summary>
		/// <param name="args">Event handler parameters.</param>
		/// <returns>Event handler return value.</returns>
		protected override object InvokeClientDelegate(params object[] args)
		{
			try
			{
				return Interceptor.InvokeClientDelegate(args);
			} 
			catch
			{
				// unsubscribe
				var dynamicWireDelegate = (Delegate)(object)In;
				ServerEventInfo.RemoveEventHandler(Component, dynamicWireDelegate);
				throw;
			}
		}
	}
}
