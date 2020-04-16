using System;
using System.Reflection;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Base class for dynamic wires.
	/// </summary>
	internal abstract class DynamicWireBase : IDisposable
	{
		/// <inheritdoc/>
		public virtual void Dispose()
		{
			IsDisposed = true;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is disposed.
		/// </summary>
		protected bool IsDisposed { get; set; }

		/// <summary>
		/// Client delegate interceptor.
		/// </summary>
		public DelegateInterceptor Interceptor { get; set; }

		/// <summary>
		/// Invokes intercepted delegate.
		/// </summary>
		/// <param name="args">Delegate parameters.</param>
		/// <returns>Delegate return value.</returns>
		protected virtual object InvokeClientDelegate(params object[] args)
		{
			return Interceptor.InvokeClientDelegate(args);
		}

		/// <summary>
		/// <see cref="MethodInfo"/> for <see cref="InvokeClientDelegate"/> method.
		/// </summary>
		protected static readonly MethodInfo InvokeClientDelegateMethodInfo =
			typeof(DynamicWireBase).GetMethod("InvokeClientDelegate", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Gets the untyped In delegate.
		/// </summary>
		public abstract Delegate InDelegate { get; }

		/// <summary>
		/// Builds strong-typed delegate of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Delegate type.</typeparam>
		/// <returns>Delegate to call <see cref="InvokeClientDelegate"/> method.</returns>
		protected T BuildDelegate<T>()
		{
			return DynamicWireFactory.BuildDelegate<T>(InvokeClientDelegateMethodInfo, this);
		}
	}
}
