using System;
using System.Threading;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Performs the processing of a message asynchronously.
	/// </summary>
	public class Asynchronizer<T>
	{
		/// <summary>
		/// Action that is called for asynchronous message processing.
		/// </summary>
		public Action<T> Out { get; set; }

		/// <summary>
		/// Takes a message and outputs it asynchronously.
		/// </summary>
		/// <param name="message">The message to process.</param>
		public void In(T message)
		{
			ThreadPool.QueueUserWorkItem(x => this.Out(message));
		}

		/// <summary>
		/// Creates a new instance and wires up the input and output pins.
		/// </summary>
		/// <param name="inputPin">Input pin.</param>
		/// <returns>Output pin.</returns>
		public static Action<T> WireUp(Action<T> inputPin)
		{
			var instance = new Asynchronizer<T>
			{
				Out = inputPin
			};

			return instance.In;
		}
	}
}
