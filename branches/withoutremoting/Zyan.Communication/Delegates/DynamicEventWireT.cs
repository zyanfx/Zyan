using System;
using System.Reflection;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Strongly typed event handler wrapper for DelegateInterceptor.
	/// </summary>
	internal class DynamicEventWire<T> : DynamicEventWireBase
	{
		/// <summary>
		/// Initializes <see cref="DynamicWire{T}"/> instance.
		/// </summary>
		public DynamicEventWire()
		{
			In = BuildDelegate<T>();
		}

		/// <summary>
		/// Dynamic wire delegate.
		/// </summary>
		public T In { get; private set; }

		/// <summary>
		/// Gets the untyped In delegate.
		/// </summary>
		public override Delegate InDelegate
		{
			get { return (Delegate)(object)In; }
		}
	}
}
