using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication
{
	/// <summary>
	/// Collection of call interception devices.
	/// </summary>
	public class CallInterceptorCollection : Collection<CallInterceptor>
	{
		// Lock objekt for thread synchronization.
		private object _lockObject = new object();

		/// <summary>
		/// Creates a new instance of the CallInterceptorCollection class.
		/// </summary>
		internal CallInterceptorCollection()
			: base()
		{ }

		/// <summary>
		/// Is called when a new item is added.
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="item">Added item</param>
		protected override void InsertItem(int index, CallInterceptor item)
		{
			lock (_lockObject)
			{
				base.InsertItem(index, item);
			}
		}

		/// <summary>
		/// Is called when a item is removed.
		/// </summary>
		/// <param name="index">Index</param>
		protected override void RemoveItem(int index)
		{
			lock (_lockObject)
			{
				base.RemoveItem(index);
			}
		}

		/// <summary>
		/// Is called when a item is set.
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="item">Item</param>
		protected override void SetItem(int index, CallInterceptor item)
		{
			lock (_lockObject)
			{
				base.SetItem(index, item);
			}
		}

		/// <summary>
		/// Is called when the collection should be cleared.
		/// </summary>
		protected override void ClearItems()
		{
			lock (_lockObject)
			{
				base.ClearItems();
			}
		}

		/// <summary>
		/// Adds call interceptors to the collection.
		/// </summary>
		/// <param name="interceptors">The interceptors to add.</param>
		public void AddRange(IEnumerable<CallInterceptor> interceptors)
		{
			lock (_lockObject)
			{
				foreach (var interceptor in interceptors)
				{
					base.InsertItem(base.Count, interceptor);
				}
			}
		}

		/// <summary>
		/// Adds call interceptors to the collection.
		/// </summary>
		/// <param name="interceptors">The interceptors to add.</param>
		public void AddRange(params CallInterceptor[] interceptors)
		{
			if (interceptors != null)
			{
				AddRange(interceptors.AsEnumerable());
			}
		}

		/// <summary>
		/// Finds a matching call interceptor for a specified method call.
		/// </summary>
		/// <param name="interfaceType">Componenet interface type</param>
		/// <param name="uniqueName">Unique name of the intercepted component.</param>
		/// <param name="remotingMessage">Remoting message from proxy</param>
		/// <returns>Call interceptor or null</returns>
		public CallInterceptor FindMatchingInterceptor(Type interfaceType, string uniqueName, IMethodCallMessage remotingMessage)
		{
			if (Count == 0)
				return null;

			var matchingInterceptors =
				from interceptor in this
				where
					interceptor.Enabled &&
					interceptor.InterfaceType.Equals(interfaceType) &&
					interceptor.UniqueName == uniqueName &&
					interceptor.MemberType == remotingMessage.MethodBase.MemberType &&
					interceptor.MemberName == remotingMessage.MethodName &&
					GetTypeList(interceptor.ParameterTypes) == GetTypeList(remotingMessage.MethodBase.GetParameters())
				select interceptor;

			lock (_lockObject)
			{
				return matchingInterceptors.FirstOrDefault();
			}
		}

		private string GetTypeList(Type[] types)
		{
			return string.Join("|", types.Select(type => type.FullName).ToArray());
		}

		private string GetTypeList(ParameterInfo[] parameters)
		{
			return string.Join("|", parameters.Select(p => p.ParameterType.FullName).ToArray());
		}

		/// <summary>
		/// Creates call interceptor helper for the given interface.
		/// </summary>
		public CallInterceptorHelper<T> For<T>()
		{
			return new CallInterceptorHelper<T>(this);
		}
	}
}
