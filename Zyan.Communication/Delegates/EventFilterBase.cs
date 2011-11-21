using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Base class for strong-typed event filters.
	/// </summary>
	/// <typeparam name="TEventArgs">Event arguments type.</typeparam>
	[Serializable]
	public abstract class EventFilterBase<TEventArgs> : IEventFilter where TEventArgs : EventArgs
	{
		/// <summary>
		/// Determines whether this filter allows invoking the event handler.
		/// </summary>
		/// <param name="parameters">Event parameters (typically, object sender and EventArgs args).</param>
		/// <returns>
		///   <c>true</c> if invocation is allowed; otherwise, <c>false</c>.
		/// </returns>
		public bool AllowInvocation(object[] parameters)
		{
			var sender = parameters.First();
			var args = parameters.Skip(1).OfType<TEventArgs>().FirstOrDefault();
			return AllowInvocation(sender, args);
		}

		/// <summary>
		/// Determines whether this filter allows invoking the event handler.
		/// </summary>
		/// <param name="sender">Event sender (typically null for the events initiated on the server side).</param>
		/// <param name="args">Event arguments.</param>
		/// <returns>
		///   <c>true</c> if invocation is allowed; otherwise, <c>false</c>.
		/// </returns>
		protected abstract bool AllowInvocation(object sender, TEventArgs args);
	}
}
