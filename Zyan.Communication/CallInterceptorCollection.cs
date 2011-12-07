using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication
{
	/// <summary>
	/// Auflistung von Aufrufabfangvorrichtungen.
	/// </summary>
	public class CallInterceptorCollection : Collection<CallInterceptor>
	{
		// Sperrobjekt (für Threadsync.)
		private object _lockObject = new object();

		/// <summary>
		/// Erzeugt eine neue Instanz der CallInterceptorCollection-Klasse.
		/// </summary>
		internal CallInterceptorCollection()
			: base()
		{ }

		/// <summary>
		/// Wird aufgerufen, wenn ein neuer Eintrag eingefügt wird.
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="item">Objekt</param>
		protected override void InsertItem(int index, CallInterceptor item)
		{
			lock (_lockObject)
			{
				base.InsertItem(index, item);
			}
		}

		/// <summary>
		/// Wird aufgerufen, wenn ein Eintrag entfernt wird.
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
		/// Wird aufgerufen, wenn ein Eintrag neu zugewiesen wird.
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="item">Objekt</param>
		protected override void SetItem(int index, CallInterceptor item)
		{
			lock (_lockObject)
			{
				base.SetItem(index, item);
			}
		}

		/// <summary>
		/// Wird aufgerufen, wenn alle Einträge entfernt werden sollen.
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
		/// Sucht eine passende Aufrufabfangvorrichtung für ein bestimmten Methodenaufruf.
		/// </summary>
		/// <param name="interfaceType">Typ der Dienstschnittstelle</param>
		/// <param name="uniqueName">Unique name of the intercepted component.</param>
		/// <param name="remotingMessage">Remoting-Nachricht des Methodenaufrufs vom Proxy</param>
		/// <returns>Aufrufabfangvorrichtung oder null</returns>
		public CallInterceptor FindMatchingInterceptor(Type interfaceType, string uniqueName, IMethodCallMessage remotingMessage)
		{
			// Wenn keine Abfangvorrichtungen registriert sind ...
			if (Count == 0)
				// null zurückgeben
				return null;

			// Passende Aufrufabfangvorrichtung suchen und zurückgeben
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

			return matchingInterceptors.FirstOrDefault();
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
		/// Creates call interceptor helper for the given interface
		/// </summary>
		public CallInterceptorHelper<T> For<T>()
		{
			return new CallInterceptorHelper<T>(this);
		}
	}
}
