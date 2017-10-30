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
	public abstract class EventFilterBase<TEventArgs> : IEventTransformFilter where TEventArgs : EventArgs
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
		/// Transforms the event arguments before sending them across the wire.
		/// </summary>
		/// <param name="parameters">The parameters (sender and arguments).</param>
		public object[] TransformEventArguments(params object[] parameters)
		{
			// assume that parameters are: sender, args
			var newParams = new[] { parameters.FirstOrDefault(), null };
			var args = parameters.Skip(1).OfType<TEventArgs>().FirstOrDefault();
			newParams[1] = TransformEventArguments(args);
			return newParams;
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
		/// Transforms the event arguments before sending them accross the wire.
		/// </summary>
		/// <param name="args">The instance containing the event data.</param>
		/// <returns>The updated instance containing the event data.</returns>
		protected virtual TEventArgs TransformEventArguments(TEventArgs args)
		{
			return args;
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
