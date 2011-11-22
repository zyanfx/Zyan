using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		public static EventHandler<TEventArgs> Create<TEventArgs, TEventFilter>(EventHandler<TEventArgs> eventHandler, TEventFilter eventFilter)
			where TEventArgs : EventArgs
			where TEventFilter : IEventFilter
		{
			// convert to EventHandler<TEventArgs> implicitly
			return new FilteredEventHandler<TEventArgs>(eventHandler, eventFilter);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler.
		/// </summary>
		/// <typeparam name="TEventFilter">The type of the event filter.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		public static EventHandler Create<TEventFilter>(EventHandler eventHandler, TEventFilter eventFilter)
			where TEventFilter : IEventFilter
		{
			// convert to EventHandler<EventArgs> implicitly
			EventHandler<EventArgs> handler = new FilteredEventHandler<EventArgs>(new EventHandler<EventArgs>(eventHandler), eventFilter);

			// convert to EventHandler explicitly
			return new EventHandler(handler);
		}

		/// <summary>
		/// Creates non-standard filtered event handler of type TDelegate.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		public static TDelegate Create<TDelegate>(TDelegate eventHandler, IEventFilter eventFilter)
			where TDelegate : class
		{
			return new FilteredCustomHandler<TDelegate>(eventHandler, eventFilter);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler{TEventArgs}.
		/// </summary>
		/// <typeparam name="TEventArgs">The type of the event args.</typeparam>
		/// <typeparam name="TEventFilter">The type of the event filter.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		public static EventHandler<TEventArgs> AddFilter<TEventArgs, TEventFilter>(this EventHandler<TEventArgs> eventHandler, TEventFilter eventFilter)
			where TEventArgs : EventArgs
			where TEventFilter : IEventFilter
		{
			return Create(eventHandler, eventFilter);
		}

		/// <summary>
		/// Creates filtered event handler of type EventHandler.
		/// </summary>
		/// <typeparam name="TEventFilter">The type of the event filter.</typeparam>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		public static EventHandler AddFilter<TEventFilter>(this EventHandler eventHandler, TEventFilter eventFilter)
			where TEventFilter : IEventFilter
		{
			return Create(eventHandler, eventFilter);
		}
	}
}
