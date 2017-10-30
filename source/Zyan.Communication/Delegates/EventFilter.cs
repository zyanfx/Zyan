using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Helper class for event filters.
	/// </summary>
	public static class EventFilter
	{
		/// <summary>
		/// Combines several event filters into one filter.
		/// </summary>
		/// <param name="first">The first filter.</param>
		/// <param name="others">Other event filters.</param>
		public static IEventFilter Combine(this IEventFilter first, params IEventFilter[] others)
		{
			if (others == null || others.Length < 1)
			{
				return first;
			}

			var filters = Enumerable.Repeat(first, 1).Concat(others).Where(f => f != null).ToArray();
			if (filters.Length == 1)
			{
				return filters.First();
			}

			return new CombinedEventFilter(filters);
		}

		/// <summary>
		/// Combines several event filters into one.
		/// </summary>
		[Serializable]
		private class CombinedEventFilter : IEventTransformFilter
		{
			public CombinedEventFilter(params IEventFilter[] filters)
			{
				EventFilters = filters ?? new IEventFilter[0];
			}

			public IEventFilter[] EventFilters { get; set; }

			public bool AllowInvocation(params object[] parameters)
			{
				foreach (var filter in EventFilters)
				{
					if (!filter.AllowInvocation(parameters))
					{
						return false;
					}
				}

				return true;
			}

			public object[] TransformEventArguments(params object[] parameters)
			{
				var newParameters = parameters;

				foreach (var filter in EventFilters.OfType<IEventTransformFilter>())
				{
					if (!filter.AllowInvocation(parameters))
					{
						return newParameters;
					}

					newParameters = filter.TransformEventArguments(parameters);
				}

				return newParameters;
			}

			public bool Contains<TEventFilter>() where TEventFilter : IEventFilter
			{
				return EventFilters.Any(ef => ef.Contains<TEventFilter>());
			}
		}
	}
}
