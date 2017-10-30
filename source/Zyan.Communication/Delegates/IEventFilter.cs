using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Interface for server-side event filtering.
	/// </summary>
	public interface IEventFilter
	{
		/// <summary>
		/// Determines whether this filter allows invoking the event handler.
		/// </summary>
		/// <param name="parameters">Event parameters (typically, object sender and EventArgs args).</param>
		/// <returns>
		///   <c>true</c> if invocation is allowed; otherwise, <c>false</c>.
		/// </returns>
		bool AllowInvocation(params object[] parameters);

		/// <summary>
		/// Determines whether this filter contains nested event filter of the given type.
		/// </summary>
		/// <typeparam name="TEventFilter">Event filter type.</typeparam>
		bool Contains<TEventFilter>() where TEventFilter : IEventFilter;
	}

	/// <summary>
	/// Interface for transforming the event arguments.
	/// </summary>
	/// <seealso cref="IEventFilter" />
	public interface IEventTransformFilter : IEventFilter
	{
		/// <summary>
		/// Transforms the event arguments before sending them across the wire.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		object[] TransformEventArguments(params object[] parameters);
	}
}
