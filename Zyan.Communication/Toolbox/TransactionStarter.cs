using System;
using System.Transactions;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Ensures that the specified message is processed within a transaction.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class TransactionStarter<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TransactionStarter{T}" /> class.
		/// </summary>
		public TransactionStarter()
		{
			// Standard settings
			IsolationLevel = IsolationLevel.ReadCommitted;
			ScopeOption = TransactionScopeOption.Required;
			Timeout = new TimeSpan(0, 0, 30);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TransactionStarter{T}" /> class.
		/// </summary>
		/// <param name="isolationLevel">The isolation level.</param>
		/// <param name="scopeOption">The scope option.</param>
		/// <param name="timeout">The timeout.</param>
		public TransactionStarter(IsolationLevel isolationLevel, TransactionScopeOption scopeOption, TimeSpan timeout)
		{
			IsolationLevel = isolationLevel;
			ScopeOption = scopeOption;
			Timeout = timeout;
		}

		/// <summary>
		/// Gets or sets the isolation level.
		/// </summary>
		public IsolationLevel IsolationLevel
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the timeout.
		/// </summary>
		public TimeSpan Timeout
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the transaction scope option.
		/// </summary>
		public TransactionScopeOption ScopeOption
		{
			get;
			set;
		}

		/// <summary>
		/// Input pin.
		/// </summary>
		/// <param name="message">The message</param>
		public void In(T message)
		{
			TransactionOptions options = new TransactionOptions();
			options.IsolationLevel = IsolationLevel;
			options.Timeout = Timeout;

			try
			{
				using (TransactionScope scope = new TransactionScope(ScopeOption, options))
				{
					// Pass the message to the output pin
					Out(message);

					// commit the transaction
					scope.Complete();
				}
			}
			catch (TransactionAbortedException)
			{
				// If the transaction abort pin is wired up...
				if (Out_TransactionAborted != null)
					Out_TransactionAborted();
			}
		}

		/// <summary>
		/// Output pin.
		/// </summary>
		public Action<T> Out;

		/// <summary>
		/// Output pin used when transaction is aborted.
		/// </summary>
		public Action Out_TransactionAborted;

		/// <summary>
		/// Creates a new instance and wires up the input and output pins.
		/// </summary>
		/// <param name="inputPin">Input pin.</param>
		/// <returns>Output pin.</returns>
		public static Action<T> WireUp(Action<T> inputPin)
		{
			var instance = new TransactionStarter<T>
			{
				Out = inputPin
			};

			return instance.In;
		}
	}
}
