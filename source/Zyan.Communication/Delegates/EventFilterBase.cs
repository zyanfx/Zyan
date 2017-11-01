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
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			if (parameters.Length != 2)
				throw new InvalidOperationException("Event handler should have 2 parameters: object and EventArgs.");

			var sender = parameters[0];
			var originalArgs = parameters[1] as TEventArgs;
			var modifiedArgs = originalArgs;
			var result = AllowInvocation(sender, originalArgs, out modifiedArgs);

			// replace the event arguments before sending
			parameters[1] = modifiedArgs;
			return result;
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

		/// <summary>
		/// Determines whether this filter allows invoking the event handler
		/// and transforms the event arguments before sending them.
		/// </summary>
		/// <param name="sender">Event sender (typically null for the events initiated on the server side).</param>
		/// <param name="originalArgs">Original event arguments.</param>
		/// <param name="modifiedArgs">Modified event arguments.</param>
		/// <returns>
		///   <c>true</c> if invocation is allowed; otherwise, <c>false</c>.
		/// </returns>
		protected virtual bool AllowInvocation(object sender, TEventArgs originalArgs, out TEventArgs modifiedArgs)
		{
			return AllowInvocation(sender, modifiedArgs = originalArgs);
		}

		/// <summary>
		/// Determines whether this filter contains nested event filter of the given type.
		/// </summary>
		/// <typeparam name="TEventFilter">Event filter type.</typeparam>
		/// <returns></returns>
		public bool Contains<TEventFilter>() where TEventFilter : IEventFilter
		{
			return this is TEventFilter;
		}
	}
}
