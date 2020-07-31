using System;
using System.Collections.Concurrent;
using System.Security.Permissions;
using System.Threading;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Threading
{
	/// <summary>
	/// Thread pool with simple locking work item queue.
	/// </summary>
	/// <remarks>
	/// Written by Joe Duffy as a part of the «Building a custom thread pool» series:
	/// http://www.bluebytesoftware.com/blog/2008/07/29/BuildingACustomThreadPoolSeriesPart1.aspx
	/// </remarks>
	public sealed class SimpleLockThreadPool : IThreadPool
	{
		// Constructors--
		// Two things may be specified:
		//   ConcurrencyLevel == fixed # of threads to use
		//   FlowExecutionContext == whether to capture & flow ExecutionContexts for work items

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleLockThreadPool" /> class.
		/// </summary>
		public SimpleLockThreadPool() :
			this(Environment.ProcessorCount, true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleLockThreadPool" /> class.
		/// </summary>
		/// <param name="concurrencyLevel">The concurrency level.</param>
		public SimpleLockThreadPool(int concurrencyLevel) :
			this(concurrencyLevel, true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleLockThreadPool" /> class.
		/// </summary>
		/// <param name="flowExecutionContext">if set to <c>true</c> [flow execution context].</param>
		public SimpleLockThreadPool(bool flowExecutionContext) :
			this(Environment.ProcessorCount, flowExecutionContext)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleLockThreadPool" /> class.
		/// </summary>
		/// <param name="concurrencyLevel">The concurrency level.</param>
		/// <param name="flowExecutionContext">if set to <c>true</c> [flow execution context].</param>
		/// <exception cref="System.ArgumentOutOfRangeException"></exception>
		public SimpleLockThreadPool(int concurrencyLevel, bool flowExecutionContext)
		{
			if (concurrencyLevel <= 0)
				throw new ArgumentOutOfRangeException("concurrencyLevel");

			m_concurrencyLevel = concurrencyLevel;
			m_flowExecutionContext = flowExecutionContext;

#if !XAMARIN
			// If suppressing flow, we need to demand permissions.
			if (!flowExecutionContext)
				new SecurityPermission(SecurityPermissionFlag.Infrastructure).Demand();
#endif
		}

		/// <summary>
		/// Each work item consists of a closure: work + (optional) state obj + context.
		/// </summary>
		private struct WorkItem
		{
			internal WaitCallback m_work;
			internal object m_obj;
			internal ExecutionContext m_executionContext;

			internal WorkItem(WaitCallback work, object obj)
			{
				m_work = work;
				m_obj = obj;
				m_executionContext = null;
			}

			internal void Invoke()
			{
				// Run normally (delegate invoke) or under context, as appropriate.
				if (m_executionContext == null)
					m_work(m_obj);
				else
					ExecutionContext.Run(m_executionContext, ContextInvoke, null);
			}

			private void ContextInvoke(object obj)
			{
				m_work(m_obj);
			}
		}

		private readonly int m_concurrencyLevel;
		private readonly bool m_flowExecutionContext;
		private readonly ConcurrentQueue<WorkItem> m_queue = new ConcurrentQueue<WorkItem>();
		private volatile Thread[] m_threads;
		private int m_threadsWaiting;
		private bool m_shutdown;

		// Methods to queue work.

		/// <summary>
		/// Queues a method for the execution, and specifies an object to be used by the method.
		/// </summary>
		/// <param name="work">A <see cref="WaitCallback" /> representing the method to execute.</param>
		/// <param name="obj">An object containing data to be used by the method.</param>
		public void QueueUserWorkItem(WaitCallback work, object obj)
		{
			WorkItem wi = new WorkItem(work, obj);

			// If execution context flowing is on, capture the caller's context.
			if (m_flowExecutionContext)
				wi.m_executionContext = ExecutionContext.Capture();

			// Make sure the pool is started (threads created, etc).
			EnsureStarted();

			// Now insert the work item into the queue, possibly waking a thread.
			lock (m_queue)
			{
				m_queue.Enqueue(wi);
				if (m_threadsWaiting > 0)
					Monitor.Pulse(m_queue);
			}
		}

		// Ensures that threads have begun executing.

		private void EnsureStarted()
		{
			if (m_threads == null)
			{
				lock (m_queue)
				{
					if (m_threads == null)
					{
						m_threads = new Thread[m_concurrencyLevel];
						for (int i = 0; i < m_threads.Length; i++)
						{
							m_threads[i] = new Thread(DispatchLoop);
							m_threads[i].IsBackground = true;
							m_threads[i].Start();
						}
					}
				}
			}
		}

		// Each thread runs the dispatch loop.

		private void DispatchLoop()
		{
			while (true)
			{
				WorkItem wi = default(WorkItem);

				lock (m_queue)
				{
					// If shutdown was requested, exit the thread.
					if (m_shutdown)
						return;

					// Find a new work item to execute.
					while (m_queue.TryDequeue(out wi) == false)
					{
						m_threadsWaiting++;
						try { Monitor.Wait(m_queue); }
						finally { m_threadsWaiting--; }

						// If we were signaled due to shutdown, exit the thread.
						if (m_shutdown)
							return;
					}

					// We found a work item! Grab it ...
				}

				// ...and Invoke it. Note: exceptions will go unhandled (and crash).
				wi.Invoke();
			}
		}

		// Disposing will signal shutdown, and then wait for all threads to finish.

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Stop();

			if (m_threads != null)
			{
				for (int i = 0; i < m_threads.Length; i++)
					m_threads[i].Join();
			}
		}

		/// <summary>
		/// Stops dispatching the work items.
		/// Doesn't wait for the work threads to stop.
		/// </summary>
		public void Stop()
		{
			m_shutdown = true;
			lock (m_queue)
			{
				Monitor.PulseAll(m_queue);
			}
		}
	}
}