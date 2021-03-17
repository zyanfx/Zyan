using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Tests
{
	public class SampleMethodSource : ISampleMethodSource, IDisposable
	{
		public void Dispose() => IsDisposed = true;

		public bool IsDisposed { get; private set; }

		private IEnumerable<string> GetStrings()
		{
			foreach (var s in new[] { "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog" })
			{
				if (IsDisposed)
				{
					throw new ObjectDisposedException(nameof(SampleMethodSource));
				}

				yield return s;
			}
		}

		public IQueryable<T> GetTable<T>() where T : class
		{
			if (typeof(T) == typeof(string))
			{
				return GetStrings().OfType<object>().OfType<T>().AsQueryable();
			}

			throw new NotSupportedException();
		}
	}
}
