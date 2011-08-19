using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Strongly typed wrapper for DelegateInterceptor.
	/// </summary>
	/// <typeparam name="T">Delegate type.</typeparam>
	internal class DynamicWire<T> : DynamicWireBase
	{
		/// <summary>
		/// Initializes <see cref="DynamicWire{T}"/> instance.
		/// </summary>
		public DynamicWire()
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
