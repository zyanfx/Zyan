using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Represents filtered event handler of a non-standard type.
	/// </summary>
	/// <typeparam name="TDelegate">The type of the event handler delegate.</typeparam>
	public class FilteredEventHandler<TDelegate> : IFilteredEventHandler
		where TDelegate : class
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredEventHandler&lt;TDelegate&gt;"/> class.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		public FilteredEventHandler(TDelegate eventHandler, IEventFilter eventFilter)
		{
			if (!(eventHandler is Delegate))
			{
				throw new ArgumentOutOfRangeException("eventHandler");
			}

			EventHandler = eventHandler;
			EventFilter = eventFilter;

			// create strong-typed invoke method
			TypedInvoke = DynamicWireFactory.BuildDelegate<TDelegate>(InvokeMethodInfo, this);
		}

		/// <summary>
		/// Untyped Invoke() method.
		/// </summary>
		private object Invoke(params object[] args)
		{
			// filter event at the client-side, if invoked locally
			if (EventFilter != null && !EventFilter.AllowInvocation(args))
			{
				return null;
			}

			// invoke client handler
			var eventHandler = (this as IFilteredEventHandler).EventHandler;
			if (eventHandler != null)
			{
				return eventHandler.DynamicInvoke(args);
			}

			return null;
		}

		/// <summary>
		/// Strong-typed Invoke() method, built dynamically.
		/// Calls the untyped Invoke() method to do the real job.
		/// </summary>
		private TDelegate TypedInvoke { get; set; }

		/// <summary>
		/// MethodInfo for the private Invoke(args) method.
		/// </summary>
		private static MethodInfo InvokeMethodInfo = typeof(FilteredEventHandler<TDelegate>).GetMethod("Invoke",
			BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(object[]) }, null);

		/// <summary>
		/// Gets the event handler.
		/// </summary>
		public TDelegate EventHandler { get; private set; }

		/// <summary>
		/// Gets the event filter.
		/// </summary>
		public IEventFilter EventFilter { get; private set; }

		/// <summary>
		/// Performs an implicit conversion from <see cref="Zyan.Communication.Delegates.FilteredEventHandler&lt;TEventFilter&gt;"/>
		/// to <see cref="System.EventHandler"/>.
		/// </summary>
		/// <param name="filteredEventHandler">The filtered event handler.</param>
		/// <returns>
		/// The result of the conversion.
		/// </returns>
		public static implicit operator TDelegate(FilteredEventHandler<TDelegate> filteredEventHandler)
		{
			return filteredEventHandler.TypedInvoke;
		}

		Delegate IFilteredEventHandler.EventHandler
		{
			get { return (Delegate)(object)EventHandler; }
		}

		IEventFilter IFilteredEventHandler.EventFilter
		{
			get { return EventFilter; }
		}
	}
}
