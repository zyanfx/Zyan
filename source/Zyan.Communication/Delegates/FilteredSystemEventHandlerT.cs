using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Represents filtered event handler of type System.EventHandler{TEventArgs}.
	/// </summary>
	/// <typeparam name="TEventArgs">The type of the event args.</typeparam>
	internal class FilteredSystemEventHandler<TEventArgs> : IFilteredEventHandler where TEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredSystemEventHandler&lt;TEventArgs&gt;"/> class.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public FilteredSystemEventHandler(EventHandler<TEventArgs> eventHandler, IEventFilter eventFilter, bool filterLocally)
		{
			IEventFilter sourceFilter;
			ExtractSourceHandler(eventHandler, out eventHandler, out sourceFilter);

			EventHandler = eventHandler;
			EventFilter = eventFilter.Combine(sourceFilter);
			FilterLocally = filterLocally;
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

		internal void Invoke(object sender, TEventArgs args)
		{
			if (FilterLocally)
			{
				// filter event at the client-side, if invoked locally
				if (EventFilter != null && !EventFilter.AllowInvocation(sender, args))
				{
					return;
				}

				// transform event at the client side, if invoked locally
				var transformer = EventFilter as IEventTransformFilter;
				if (transformer != null)
				{
					var newArgs = transformer.TransformEventArguments(sender, args);
					if (newArgs != null && newArgs.Length > 1)
					{
						sender = newArgs[0];
						args = newArgs[1] as TEventArgs;
					}
				}
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
		/// Performs an implicit conversion from <see cref="Zyan.Communication.Delegates.FilteredSystemEventHandler&lt;TEventArgs&gt;"/>
		/// to <see cref="System.EventHandler&lt;TEventArgs&gt;"/>.
		/// </summary>
		/// <param name="filteredEventHandler">The filtered event handler.</param>
		/// <returns>
		/// The result of the conversion.
		/// </returns>
		public static implicit operator EventHandler<TEventArgs>(FilteredSystemEventHandler<TEventArgs> filteredEventHandler)
		{
			return filteredEventHandler.Invoke;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this event filter should also work locally.
		/// </summary>
		public bool FilterLocally { get; set; }

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
