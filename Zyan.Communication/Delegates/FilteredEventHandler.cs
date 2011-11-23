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
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler<TEventArgs> Create<TEventArgs>(EventHandler<TEventArgs> eventHandler, IEventFilter eventFilter, bool filterLocally = true)
			where TEventArgs : EventArgs
		{
			// convert to EventHandler<TEventArgs> implicitly
			return new FilteredSystemEventHandler<TEventArgs>(eventHandler, eventFilter, filterLocally);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler Create(EventHandler eventHandler, IEventFilter eventFilter, bool filterLocally = true)
		{
			// convert to EventHandler implicitly
			return new FilteredSystemEventHandler(eventHandler, eventFilter, filterLocally);
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
			return new FilteredSystemEventHandler<TEventArgs>(eventHandler, new FlexibleEventFilter<TEventArgs>(expression), filterLocally);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler{TEventArgs}.
		/// </summary>
		/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler<TEventArgs> AddFilter<TEventArgs>(this EventHandler<TEventArgs> eventHandler, IEventFilter eventFilter, bool filterLocally = true)
			where TEventArgs : EventArgs
		{
			return Create(eventHandler, eventFilter, filterLocally);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public static EventHandler AddFilter(this EventHandler eventHandler, IEventFilter eventFilter, bool filterLocally = true)
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
