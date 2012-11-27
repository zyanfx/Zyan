﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Represents filtered event handler of type System.EventHandler.
	/// </summary>
	internal class FilteredSystemEventHandler : IFilteredEventHandler
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredSystemEventHandler"/> class.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public FilteredSystemEventHandler(EventHandler eventHandler, IEventFilter eventFilter, bool filterLocally)
		{
			IEventFilter sourceFilter;
			ExtractSourceHandler(eventHandler, out eventHandler, out sourceFilter);

			EventHandler = eventHandler;
			EventFilter = eventFilter.Combine(sourceFilter);
			FilterLocally = filterLocally;
		}

		private void ExtractSourceHandler(EventHandler eventHandler, out EventHandler sourceHandler, out IEventFilter sourceFilter)
		{
			sourceHandler = eventHandler;
			sourceFilter = default(IEventFilter);

			while (sourceHandler.Target is IFilteredEventHandler)
			{
				var filtered = sourceHandler.Target as IFilteredEventHandler;
				sourceHandler = filtered.EventHandler as EventHandler;
				sourceFilter = filtered.EventFilter.Combine(sourceFilter);
			}
		}

		private void Invoke(object sender, EventArgs args)
		{
			if (FilterLocally)
			{
				// filter event at the client-side, if invoked locally
				if (EventFilter != null && !EventFilter.AllowInvocation(sender, args))
				{
					return;
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
		public EventHandler EventHandler { get; private set; }

		/// <summary>
		/// Gets the event filter.
		/// </summary>
		public IEventFilter EventFilter { get; private set; }

		/// <summary>
		/// Performs an implicit conversion from <see cref="Zyan.Communication.Delegates.FilteredSystemEventHandler"/>
		/// to <see cref="System.EventHandler"/>.
		/// </summary>
		/// <param name="filteredEventHandler">The filtered event handler.</param>
		/// <returns>
		/// The result of the conversion.
		/// </returns>
		public static implicit operator EventHandler(FilteredSystemEventHandler filteredEventHandler)
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
