using System;
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
		/// Creates a new debounced version of the passed action.
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
			var timer = default(IDisposable);

			return () =>
			{
				timer?.Dispose();
				timer = SetTimeout(action, delayMs);
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
	}
}
