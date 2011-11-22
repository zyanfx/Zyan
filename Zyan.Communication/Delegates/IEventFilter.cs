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
	}
}
