using System;
using System.Linq.Expressions;
using Zyan.InterLinq.Expressions;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Expression-based event filter.
	/// </summary>
	/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
	[Serializable]
	public class FlexibleEventFilter<TEventArgs> : EventFilterBase<TEventArgs> where TEventArgs: EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FlexibleEventFilter{TEventArgs}"/> class.
		/// </summary>
		public FlexibleEventFilter(Expression<Func<object, TEventArgs, bool>> expression)
		{
			Expression = expression.MakeSerializable();
		}

		private SerializableExpression Expression { get; set; }

		[NonSerialized]
		private Func<object, TEventArgs, bool> predicate;

		private Func<object, TEventArgs, bool> Predicate
		{
			get
			{
				if (predicate == null)
				{
					var deserialized = Expression.Deserialize();
					var expression = deserialized as Expression<Func<object, TEventArgs, bool>>;
					predicate = expression.Compile();
				}

				return predicate;
			}
		}

		/// <summary>
		/// Returns true if event handler invocation is allowed.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="args">The instance containing the event data.</param>
		protected override bool AllowInvocation(object sender, TEventArgs args)
		{
			return Predicate(sender, args);
		}
	}
}
