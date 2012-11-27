using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Represents filtered event handler of a custom delegate type.
	/// </summary>
	/// <typeparam name="TDelegate">The type of the event handler delegate.</typeparam>
	internal class FilteredCustomHandler<TDelegate> : IFilteredEventHandler where TDelegate : class
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredCustomHandler&lt;TDelegate&gt;"/> class.
		/// </summary>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="eventFilter">The event filter.</param>
		/// <param name="filterLocally">Whether the filter should also work when used locally.</param>
		public FilteredCustomHandler(TDelegate eventHandler, IEventFilter eventFilter, bool filterLocally)
		{
			if (!(eventHandler is Delegate))
			{
				throw new ArgumentOutOfRangeException("eventHandler");
			}

			IEventFilter sourceFilter;
			ExtractSourceHandler(eventHandler, out eventHandler, out sourceFilter);

			EventHandler = eventHandler;
			EventFilter = eventFilter.Combine(sourceFilter);

			// create strong-typed invoke method
			TypedInvoke = DynamicWireFactory.BuildInstanceDelegate<TDelegate>(InvokeMethodInfo, this);
			FilterLocally = filterLocally;
		}

		private void ExtractSourceHandler(TDelegate eventHandler, out TDelegate sourceHandler, out IEventFilter sourceFilter)
		{
			sourceHandler = eventHandler;
			sourceFilter = default(IEventFilter);
			var sourceDelegate = (Delegate)(object)sourceHandler;

			while (sourceDelegate.Target is IFilteredEventHandler)
			{
				var filtered = sourceDelegate.Target as IFilteredEventHandler;
				sourceHandler = filtered.EventHandler as TDelegate;
				sourceFilter = filtered.EventFilter.Combine(sourceFilter);
				sourceDelegate = (Delegate)(object)sourceHandler;
			}
		}

		/// <summary>
		/// Untyped Invoke() method.
		/// </summary>
		private object Invoke(params object[] args)
		{
			if (FilterLocally)
			{
				// filter event at the client-side, if invoked locally
				if (EventFilter != null && !EventFilter.AllowInvocation(args))
				{
					return null;
				}
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
		private static MethodInfo InvokeMethodInfo = typeof(FilteredCustomHandler<TDelegate>).GetMethod("Invoke",
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
		/// Performs an implicit conversion from <see cref="Zyan.Communication.Delegates.FilteredCustomHandler&lt;TEventFilter&gt;"/>
		/// to <see cref="System.EventHandler"/>.
		/// </summary>
		/// <param name="filteredEventHandler">The filtered event handler.</param>
		/// <returns>
		/// The result of the conversion.
		/// </returns>
		public static implicit operator TDelegate(FilteredCustomHandler<TDelegate> filteredEventHandler)
		{
			return filteredEventHandler.TypedInvoke;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this event filter should also work locally.
		/// </summary>
		public bool FilterLocally { get; set; }

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
