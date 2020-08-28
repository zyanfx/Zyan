using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Simple wrapper for ConcurrentQueue that adds a high watermark limit.
	/// </summary>
	/// <typeparam name="T">The type of the queue item.</typeparam>
	internal class LimitedSizeQueue<T> : IEnumerable<T>
	{
		public LimitedSizeQueue(int limit)
		{
			Queue = new ConcurrentQueue<T>();
			Limit = limit;
		}

		private ConcurrentQueue<T> Queue { get; set; }

		public int Limit { get; }

		private object queueLock = new object();

		public int Count => Queue.Count;

		public IEnumerator<T> GetEnumerator() => Queue.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Queue.GetEnumerator();

		public bool TryDequeue(out T result) => Queue.TryDequeue(out result);

		public bool TryPeek(out T result) => Queue.TryPeek(out result);

		public bool TryEnqueue(T item)
		{
			if (Count < Limit)
			{
				lock (queueLock)
				{
					if (Count < Limit)
					{
						Queue.Enqueue(item);
						return true;
					}
				}
			}

			return false;
		}

		public void Clear()
		{
			Queue = new ConcurrentQueue<T>();
		}
	}
}
