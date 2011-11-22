using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Represents filtered event handler of type EventHandler{TEventArgs}.
	/// </summary>
	/// <typeparam name="TEventArgs">The type of the event args.</typeparam>
	internal class FilteredEventHandler<TEventArgs> : IFilteredEventHandler where TEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredEventHandler&lt;TEventArgs&gt;"/> class.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		public FilteredEventHandler(EventHandler<TEventArgs> eventHandler, IEventFilter eventFilter)
		{
			IEventFilter sourceFilter;
			ExtractSourceHandler(eventHandler, out eventHandler, out sourceFilter);

			EventHandler = eventHandler;
			EventFilter = eventFilter.Combine(sourceFilter);
		}

		private void ExtractSourceHandler(EventHandler<TEventArgs> eventHandler, out EventHandler<TEventArgs> sourceHandler, out IEventFilter sourceFilter)
		{
			sourceHandler = eventHandler;
			sourceFilter = default(IEventFilter);

			while (sourceHandler.Target is IFilteredEventHandler)
			{
				var filtered = sourceHandler.Target as IFilteredEventHandler;
				sourceHandler = filtered.EventHandler as EventHandler<TEventArgs>;
				sourceFilter = filtered.EventFilter.Combine(sourceFilter);
			}
		}

		private void Invoke(object sender, TEventArgs args)
		{
			// filter event at the client-side, if invoked locally
			if (EventFilter != null && !EventFilter.AllowInvocation(sender, args))
			{
				return;
			}

			// invoke client handler
			if (EventHandler != null)
			{
				EventHandler(sender, args);
			}
		}

		/// <summary>
		/// Gets the event handler.
		/// </summary>
		public EventHandler<TEventArgs> EventHandler { get; private set; }

		/// <summary>
		/// Gets the event filter.
		/// </summary>
		public IEventFilter EventFilter { get; private set; }

		/// <summary>
		/// Performs an implicit conversion from <see cref="Zyan.Communication.Delegates.FilteredEventHandler&lt;TEventArgs&gt;"/>
		/// to <see cref="System.EventHandler&lt;TEventArgs&gt;"/>.
		/// </summary>
		/// <param name="filteredEventHandler">The filtered event handler.</param>
		/// <returns>
		/// The result of the conversion.
		/// </returns>
		public static implicit operator EventHandler<TEventArgs>(FilteredEventHandler<TEventArgs> filteredEventHandler)
		{
			return filteredEventHandler.Invoke;
		}

		Delegate IFilteredEventHandler.EventHandler
		{
			get { return EventHandler; }
		}

		IEventFilter IFilteredEventHandler.EventFilter
		{
			get { return EventFilter; }
		}
	}
}
