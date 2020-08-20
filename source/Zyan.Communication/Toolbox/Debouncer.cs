using System;
using System.Threading;
using SysTimer = System.Timers.Timer;

namespace Zyan.Communication.Toolbox
{
	internal static class Debouncer
	{
		/// <summary>
		/// Default debounce interval, milliseconds.
		/// </summary>
		public const int DefaultDebounceInterval = 300;

		/// <summary>
		/// A delegate to represent a debounced method which's pending execution can be canceled.
		/// </summary>
		/// <param name="execute">
		/// If true, schedule the execution (this is the default, like normal debounce).
		/// If false, cancel pending execution.
		/// </param>
		public delegate void CancellableAction(bool execute = true);

		/// <summary>
		/// Creates a new debounced version of the given action.
		/// </summary>
		/// <param name="action">Action to debounce.</param>
		/// <param name="delayMs">Debounce interval in milliseconds.</param>
		/// <returns>The debounced version of the given action.</returns>
		/// <remarks>
		/// Based on these gists:
		/// https://gist.github.com/ca0v/73a31f57b397606c9813472f7493a940
		/// https://gist.github.com/fr-ser/ded7690b245223094cd876069456ed6c
		/// </remarks>
		public static Action Debounce(this Action action, int delayMs = DefaultDebounceInterval)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			if (delayMs <= 0)
			{
				return action;
			}

			var timer = default(IDisposable);

			return () =>
			{
				timer?.Dispose();
				timer = SetTimeout(action, delayMs);
			};
		}

		/// <summary>
		/// Creates a new cancellable debounced version of the given action.
		/// </summary>
		/// <param name="action">Action to debounce.</param>
		/// <param name="delayMs">Debounce interval in milliseconds.</param>
		/// <returns>The debounced version of the given action.</returns>
		public static CancellableAction CancellableDebounce(this Action action, int delayMs = DefaultDebounceInterval)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			if (delayMs <= 0)
			{
				return execute =>
				{
					if (execute)
					{
						action();
					}
				};
			}

			var timer = default(IDisposable);
			return execute =>
			{
				timer?.Dispose();
				timer = execute ? SetTimeout(action, delayMs) : null;
			};
		}

		/// <summary>
		/// Executes an action at specified intervals (in milliseconds), like setInterval in Javascript.
		/// </summary>
		/// <param name="action">The action to schedule.</param>
		/// <param name="delayMs">The delay in milliseconds.</param>
		/// <returns>
		/// The value that can be disposed to stop the timer.
		/// </returns>
		/// <remarks>
		/// Based on this gist:
		/// https://gist.github.com/CipherLab/10a40f7032be04f0aa6f
		/// </remarks>
		public static IDisposable SetInterval(Action action, int delayMs)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			var timer = new SysTimer(delayMs)
			{
				AutoReset = true
			};

			timer.Elapsed += (s, e) =>
			{
				action();
			};

			timer.Start();
			return timer;
		}

		/// <summary>
		/// Executes an action after a specified number of milliseconds.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		/// <param name="delayMs">The delay in milliseconds.</param>
		/// <returns>
		/// The value that can be disposed to cancel the execution.
		/// </returns>
		/// <remarks>
		/// Based on this gist:
		/// https://gist.github.com/CipherLab/10a40f7032be04f0aa6f
		/// </remarks>
		public static IDisposable SetTimeout(Action action, int delayMs)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			var timer = new SysTimer(delayMs)
			{
				AutoReset = false
			};

			timer.Elapsed += (s, e) =>
			{
				action();
				timer.Dispose();
			};

			timer.Start();
			return timer;
		}

		/// <summary>
		/// Returns the action that can be executed only by one thread at most.
		/// While there is a thread that currently runs the original action,
		/// all other threads will pass the execution.
		/// </summary>
		/// <param name="action">The action that should be executed by one thread at most.</param>
		/// <returns>The converted action that cannot be executed by multiple threads at once.</returns>
		public static Action ExecuteByOneThreadAtMost(this Action action)
		{
			var padlock = new object();
			return () =>
			{
				if (Monitor.TryEnter(padlock))
				{
					try
					{
						action();
					}
					finally
					{
						Monitor.Exit(padlock);
					}
				}
			};
		}
	}
}
