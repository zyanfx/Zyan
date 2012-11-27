using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Toolbox
{
	public static partial class Extensions
	{
		/// <summary>
		/// Merges two sequences by using specified selector (missing in .NET Framework 3.5).
		/// </summary>
		/// <typeparam name="TFirst">First sequence element type.</typeparam>
		/// <typeparam name="TSecond">Second sequence element type.</typeparam>
		/// <typeparam name="TResult">Resulting sequence element type.</typeparam>
		/// <param name="first">First sequence.</param>
		/// <param name="second">Second sequence.</param>
		/// <param name="resultSelector">Selector function.</param>
		/// <returns></returns>
		public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(
			this IEnumerable<TFirst> first,
			IEnumerable<TSecond> second,
			Func<TFirst, TSecond, TResult> resultSelector)
		{
			if (first == null)
				throw new ArgumentNullException("first");

			if (second == null)
				throw new ArgumentNullException("second");

			if (resultSelector == null)
				throw new ArgumentNullException("resultSelector");

			using (var e1 = first.GetEnumerator())
			using (var e2 = second.GetEnumerator())
			{
				while (e1.MoveNext() && e2.MoveNext())
					yield return resultSelector(e1.Current, e2.Current);
			}
		}
	}
}
