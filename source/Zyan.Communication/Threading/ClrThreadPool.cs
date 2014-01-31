using System;
using System.Threading;

namespace Zyan.Communication.Threading
{
	/// <summary>
	/// Built-in CLR thread pool implementation of the <see cref="IThreadPool"/> interface.
	/// </summary>
	public sealed class ClrThreadPool : IThreadPool
	{
		/// <summary>
		/// Queues a method for the execution, and specifies an object to be used by the method.
		/// </summary>
		/// <param name="work">A <see cref="WaitCallback" /> representing the method to execute.</param>
		/// <param name="obj">An object containing data to be used by the method.</param>
		public void QueueUserWorkItem(WaitCallback work, object obj)
		{
			ThreadPool.QueueUserWorkItem(work, obj);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
		}
	}
}