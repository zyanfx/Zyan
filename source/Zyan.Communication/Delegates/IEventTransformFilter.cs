using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Delegates
{
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
