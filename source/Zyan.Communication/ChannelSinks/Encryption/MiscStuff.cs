using System;
using System.Security.Cryptography;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
	/// Common transport header names.
	/// </summary>
	internal class CommonHeaderNames
	{
		/// <summary>
		/// Unique identifier of the secure transaction.
		/// </summary>
		public const string SECURE_TRANSACTION_ID = "X-CY_SECURE_TRANSACTION_ID";

		/// <summary>
		/// Status of the secure transaction.
		/// </summary>
		public const string SECURE_TRANSACTION_STATE = "X-CY_SECURE_TRANSACTION_STATE";

		/// <summary>
		/// RSA public key.
		/// </summary>
		public const string PUBLIC_KEY = "X-CY_PUBLIC_KEY";

		/// <summary>
		/// Encrypted shared key.
		/// </summary>
		public const string SHARED_KEY = "X-CY_SHARED_KEY";

		/// <summary>
		/// Encrypted shared initialization vector.
		/// </summary>
		public const string SHARED_IV = "X-CY_SHARED_IV";
	}

	/// <summary>
	/// Security transaction stages.
	/// </summary>
	internal enum SecureTransactionStage
	{
		/// <summary>
		/// Uninitialized, nothing happened yet.
		/// </summary>
		Uninitialized = 0,

		/// <summary>
		/// The client is sending his public key to the server.
		/// </summary>
		SendingPublicKey,

		/// <summary>
		/// The server is sending shared key encrypted with the client's public key.
		/// </summary>
		SendingSharedKey,

		/// <summary>
		/// The client is sending a message encrypted with the shared key.
		/// </summary>
		SendingEncryptedMessage,

		/// <summary>
		/// The server is sending an encrypted response message to the client.
		/// </summary>
		SendingEncryptedResult,

		/// <summary>
		/// Unknown secure transaction identifier.
		/// </summary>
		UnknownTransactionID
	}

	/// <summary>
	/// Contains client connection information for a secure transaction.
	/// </summary>
	internal class ClientConnectionData : IDisposable
	{
		#region Declarations

		// Unique security transaction ID of the client
		private Guid _secureTransactionID;

		// Cryptographic algorithm for symmetric encryption
		private SymmetricAlgorithm _cryptoProvider;

		// The time of last communication
		private DateTime _timestamp;

		// Indicates whether the object has already been disposed of
		private bool _disposed = false;

		// When reentrancy level is greater than zero, it means that the server call is in progress
		private int reentrancyLevel;

		#endregion

		#region Constructor and destructor

		/// <summary>
		/// Initializes a new instance of the <see cref="ClientConnectionData"/> class.
		/// </summary>
		/// <param name="secureTransactionID">The secure transaction identifier.</param>
		/// <param name="cryptoProvider">The cryptographic provider.</param>
		public ClientConnectionData(Guid secureTransactionID, SymmetricAlgorithm cryptoProvider)
		{
			if (cryptoProvider == null)
				throw new ArgumentNullException("cryptoProvider");

			_secureTransactionID = secureTransactionID;
			_cryptoProvider = cryptoProvider;
			_timestamp = DateTime.UtcNow;
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="ClientConnectionData"/> class.
		/// </summary>
		~ClientConnectionData()
		{
			Dispose(false);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Updates the timestamp.
		/// </summary>
		public void UpdateTimestamp()
		{
			CheckDisposed();
			_timestamp = DateTime.UtcNow;
		}

		/// <summary>
		/// Begins the method call.
		/// </summary>
		public void BeginMethodCall()
		{
			CheckDisposed();
			System.Threading.Interlocked.Increment(ref reentrancyLevel);
		}

		/// <summary>
		/// Ends the method call.
		/// </summary>
		public void EndMethodCall()
		{
			CheckDisposed();
			System.Threading.Interlocked.Decrement(ref reentrancyLevel);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		void IDisposable.Dispose()
		{
			// Release resources and make sure that the destructor is not called
			Dispose(true);
		}

		/// <summary>
		/// Releases unmanaged and — optionally — managed resources.
		/// </summary>
		/// <param name="disposing">
		/// <c>true</c> to release both managed and unmanaged resources;
		/// <c>false</c> to release only unmanaged resources.
		/// </param>
		protected void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				if (_cryptoProvider != null)
					((IDisposable)_cryptoProvider).Dispose();

				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Throws an exception if the object has already been disposed of.
		/// </summary>
		private void CheckDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException("ClientConnectionData");
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the secure transaction identifier.
		/// </summary>
		public Guid SecureTransactionID
		{
			get
			{
				CheckDisposed();
				return _secureTransactionID;
			}
		}

		/// <summary>
		/// Gets the cryptographic provider used for symmetric encryption.
		/// </summary>
		public SymmetricAlgorithm CryptoProvider
		{
			get
			{
				CheckDisposed();
				return _cryptoProvider;
			}
		}

		/// <summary>
		/// Gets the timestamp of the last communication.
		/// </summary>
		public DateTime Timestamp
		{
			get
			{
				CheckDisposed();
				return _timestamp;
			}
		}

		/// <summary>
		/// Gets a value indicating whether a call is in progress.
		/// </summary>
		public bool CallInProgress
		{
			get
			{
				CheckDisposed();
				return reentrancyLevel > 0;
			}
		}

		#endregion
	}
}
