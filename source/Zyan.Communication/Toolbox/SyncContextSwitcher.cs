using System;
using System.Threading;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Ensures that the processing of messages is performed in the original thread.
	/// </summary>
	public class SyncContextSwitcher<T>
	{
		// The current synchronization context is stored at the creation time
		private readonly SynchronizationContext syncContext = SynchronizationContext.Current;

		/// <summary>
		/// Action to be performed when a message is to be processed.
		/// </summary>
		public Action<T> Out;

		/// <summary>
		/// Processes a message using the original synchronization context.
		/// </summary>
		/// <param name="message">The message.</param>
		public void In(T message)
		{
			// If the synchronization context is known, send the message to it
			if (syncContext != null)
			{
				syncContext.Send(x => this.Out(message), null);
				return;
			}

			// Execute action directly
			Out(message);
		}

		/// <summary>
		/// Creates a new instance and wires up the pins.
		/// </summary>
		/// <param name="inputPin">Input pin</param>
		/// <returns>Output pin</returns>
		public static Action<T> WireUp(Action<T> inputPin)
		{
			var instance = new SyncContextSwitcher<T>
			{
				Out = inputPin
			};

			return instance.In;
		}
	}
}
