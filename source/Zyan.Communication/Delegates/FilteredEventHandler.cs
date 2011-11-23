using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Helper class for filtered event handlers.
	/// </summary>
	public static class FilteredEventHandler
	{
		/// <summary>
		/// Creates filtered event handler of type EventHandler{TEventArgs}.
		/// </summary>
		/// <typeparam name="TEventArgs">The type of the event args.</typeparam>
		/// <typeparam name="TEventFilter">The type of the event filter.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler<TEventArgs> Create<TEventArgs, TEventFilter>(EventHandler<TEventArgs> eventHandler, TEventFilter eventFilter, bool filterLocally = true)
			where TEventArgs : EventArgs
			where TEventFilter : IEventFilter
		{
			// convert to EventHandler<TEventArgs> implicitly
			return new FilteredEventHandler<TEventArgs>(eventHandler, eventFilter, filterLocally);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler.
		/// </summary>
		/// <typeparam name="TEventFilter">The type of the event filter.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler Create<TEventFilter>(EventHandler eventHandler, TEventFilter eventFilter, bool filterLocally = true)
			where TEventFilter : IEventFilter
		{
			// convert to EventHandler<EventArgs> explicitly
			var typedEventHandler = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), eventHandler.Target, eventHandler.Method) as EventHandler<EventArgs>;
			var filteredEventHandler = new FilteredEventHandler<EventArgs>(typedEventHandler, eventFilter, filterLocally);

			// convert back to EventHandler
			return new EventHandler(filteredEventHandler.Invoke);
		}

		/// <summary>
		/// Creates non-standard filtered event handler of type TDelegate.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static TDelegate Create<TDelegate>(TDelegate eventHandler, IEventFilter eventFilter, bool filterLocally = true)
			where TDelegate : class
		{
			return new FilteredCustomHandler<TDelegate>(eventHandler, eventFilter, filterLocally);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler{TEventArgs} using the specified filter predicate.
		/// </summary>
		/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="expression">The predicate expression.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler<TEventArgs> Create<TEventArgs>(EventHandler<TEventArgs> eventHandler, Expression<Func<object, TEventArgs, bool>> expression, bool filterLocally)
			where TEventArgs : EventArgs
		{
			return new FilteredEventHandler<TEventArgs>(eventHandler, new FlexibleEventFilter<TEventArgs>(expression), filterLocally);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler{TEventArgs}.
		/// </summary>
		/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
		/// <typeparam name="TEventFilter">The type of the event filter.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler<TEventArgs> AddFilter<TEventArgs, TEventFilter>(this EventHandler<TEventArgs> eventHandler, TEventFilter eventFilter, bool filterLocally = true)
			where TEventArgs : EventArgs
			where TEventFilter : IEventFilter
		{
			return Create(eventHandler, eventFilter, filterLocally);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler.
		/// </summary>
		/// <typeparam name="TEventFilter">The type of the event filter.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler AddFilter<TEventFilter>(this EventHandler eventHandler, TEventFilter eventFilter, bool filterLocally = true)
			where TEventFilter : IEventFilter
		{
			return Create(eventHandler, eventFilter, filterLocally);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler{TEventArgs}.
		/// </summary>
		/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="expression">The predicate expression.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler<TEventArgs> AddFilter<TEventArgs>(this EventHandler<TEventArgs> eventHandler,
			Expression<Func<object, TEventArgs, bool>> expression, bool filterLocally = true)
			where TEventArgs : EventArgs
		{
			return Create(eventHandler, expression, filterLocally);
		}
	}
}
