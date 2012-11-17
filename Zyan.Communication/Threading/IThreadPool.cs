using System;
using System.Threading;

namespace Zyan.Communication.Threading
{
	/// <summary>
	/// Simple thread pool interface.
	/// </summary>
	public interface IThreadPool : IDisposable
	{
		/// <summary>
		/// Queues a method for the execution, and specifies an object to be used by the method.
		/// </summary>
		/// <param name="work">A <see cref="WaitCallback"/> representing the method to execute.</param>
		/// <param name="obj">An object containing data to be used by the method.</param>
		void QueueUserWorkItem(WaitCallback work, object obj);
	}
}