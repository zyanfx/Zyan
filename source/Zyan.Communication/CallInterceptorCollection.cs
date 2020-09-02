using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication
{
	/// <summary>
	/// Collection of call interception devices.
	/// </summary>
	public class CallInterceptorCollection : ConcurrentCollection<CallInterceptor>
	{
		/// <summary>
		/// Creates a new instance of the CallInterceptorCollection class.
		/// </summary>
		internal CallInterceptorCollection()
			: base()
		{
		}

		/// <summary>
		/// Adds call interceptors to the collection.
		/// </summary>
		/// <param name="interceptors">The interceptors to add.</param>
		public void AddRange(IEnumerable<CallInterceptor> interceptors)
		{
			lock (SyncRoot)
			{
				foreach (var interceptor in interceptors)
				{
					base.Add(interceptor);
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

			var matchingInterceptors = this
				.Where(ic => ic.Enabled)
				.Where(ic => Regex.IsMatch(uniqueName, ic.UniqueNameFilter))
				.Where(ic => Equals(ic.InterfaceType, interfaceType))
				.Where(ic => ic.MemberType == remotingMessage.MethodBase.MemberType)
				.Where(ic => ic.MemberName == remotingMessage.MethodName)
				.Where(ic => GetTypeList(ic.ParameterTypes) == GetTypeList(remotingMessage.MethodBase.GetParameters()))
				.ToList();

			lock (SyncRoot)
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
