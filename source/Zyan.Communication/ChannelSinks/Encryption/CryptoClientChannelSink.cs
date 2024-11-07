using System;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
	/// Client-side channel sink for the encrypted communication.
	/// </summary>
	/// <remarks>
	/// Requires a <see cref="CryptoServerChannelSink"/> on the server side.
	/// </remarks>
	internal class CryptoClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink
	{
		#region Fields

		// Symmetric encryption algorithm name
		private readonly string _algorithm;

		// OAEP padding switch
		private readonly bool _oaep;

		// Maximal number of attempts
		private readonly int _maxAttempts;

		// Next channel sink
		private readonly IClientChannelSink _next;

		// Unique identifier of the secure transaction
		private Guid _secureTransactionID = Guid.Empty;

		// Symmetric encryption algorithm
		private volatile SymmetricAlgorithm _provider = null;

		// Asymmetric encryption algorithm used for symmetric key exchange
		private volatile RSACryptoServiceProvider _rsaProvider = null;

		// Locking object for thread synchronization
		private readonly object _lockObject = null;

		// Default exception text
		private const string DEFAULT_EXCEPTION_TEXT = "The client-side channel sink could not establish encrypted connection to the server.";

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoClientChannelSink"/> class.
		/// </summary>
		/// <param name="nextSink">The next channel sink.</param>
		/// <param name="algorithm">Symmetric encryption algorithm na,e.</param>
		/// <param name="oaep">OAEP padding switch.</param>
		/// <param name="maxAttempts">The maximum number of attempts.</param>
		public CryptoClientChannelSink(IClientChannelSink nextSink, string algorithm, bool oaep, int maxAttempts)
		{
			_algorithm = algorithm;
			_oaep = oaep;
			_next = nextSink;
			_maxAttempts = maxAttempts;
			_lockObject = new object();

			// Initialize asymmetric encryption algorithm
			_rsaProvider = new RSACryptoServiceProvider();
		}

		#endregion

		#region Synchronous processing

		/// <summary>
		/// Starts a new secure transaction.
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="requestHeaders">Request transport headers.</param>
		/// <returns>New transaction unique identifier.</returns>
		private Guid StartSecureTransaction(IMessage msg, ITransportHeaders requestHeaders)
		{
			if (_provider == null || _secureTransactionID.Equals(Guid.Empty))
			{
				_secureTransactionID = Guid.NewGuid();
				_provider = ObtainSharedKey(msg);
			}

			return _secureTransactionID;
		}

		/// <summary>
		/// Requests the shared key for symmetrical encryption algorithm from the server.
		/// </summary>
		/// <param name="msg">The message.</param>
		private SymmetricAlgorithm ObtainSharedKey(IMessage msg)
		{
			TransportHeaders requestHeaders = new TransportHeaders();
			MemoryStream requestStream = new MemoryStream();
			ITransportHeaders responseHeaders;
			Stream responseStream;

			// Generate a request for the shared key
			CreateSharedKeyRequest(requestHeaders);

			// Retry as needed
			for (var i = 0; i < _maxAttempts; i++)
			{
				// Send the request to the next sink to communicate with the CryptoServerChannelSink across the wire
				if (NextSinkProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream))
				{
					// Process server's response
					return ProcessSharedKeyResponse(responseHeaders);
				}
			}

			throw new CryptoRemotingException(DEFAULT_EXCEPTION_TEXT);
		}

		/// <summary>
		/// Clears the shared key and starts a new secure transaction.
		/// </summary>
		private void ClearSharedKey()
		{
			_provider = null;
			_secureTransactionID = Guid.Empty;
		}

		/// <summary>
		/// Creates the shared key request.
		/// </summary>
		/// <param name="requestHeaders">Request transport headers.</param>
		private void CreateSharedKeyRequest(ITransportHeaders requestHeaders)
		{
			// Get the public RSA key for encryption
			string rsaKey = _rsaProvider.ToXmlString(false);

			// Pass the public key and secure transaction identifier to the server
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingPublicKey).ToString();
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_ID] = _secureTransactionID.ToString();
			requestHeaders[CommonHeaderNames.PUBLIC_KEY] = rsaKey;
		}

		/// <summary>
		/// Decrypts the incoming response stream.
		/// </summary>
		/// <param name="responseStream">The response stream.</param>
		/// <param name="responseHeaders">The response headers.</param>
		private Stream DecryptResponse(Stream responseStream, ITransportHeaders responseHeaders)
		{
			try
			{
				if (responseHeaders != null && (SecureTransactionStage)Convert.ToInt32((string)responseHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE]) == SecureTransactionStage.SendingEncryptedResult)
				{
					// Decrypt the response stream and close it as we won't be using it anymore
					Stream decryptedStream = CryptoTools.GetDecryptedStream(responseStream, _provider);
					responseStream.Close();
					return decryptedStream;
				}
			}
			catch { }

			// Failed to decrypt server's response
			return null;
		}

		/// <summary>
		/// Processes the shared key response.
		/// </summary>
		/// <param name="responseHeaders">Response transport headers.</param>
		private SymmetricAlgorithm ProcessSharedKeyResponse(ITransportHeaders responseHeaders)
		{
			// Get the encrypted key and initialization vector from the transport headers sent by the server
			string encryptedKey = (string)responseHeaders[CommonHeaderNames.SHARED_KEY];
			string encryptedIV = (string)responseHeaders[CommonHeaderNames.SHARED_IV];

			if (encryptedKey == null || encryptedKey == string.Empty)
				throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_KeyChanged);

			if (encryptedIV == null || encryptedIV == string.Empty)
				throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_IVMissing);

			// Create symmetric algorithm and set shared key and initialization vector
			SymmetricAlgorithm sharedProvider = CryptoTools.CreateSymmetricCryptoProvider(_algorithm);
			sharedProvider.Key = _rsaProvider.Decrypt(Convert.FromBase64String(encryptedKey), _oaep);
			sharedProvider.IV = _rsaProvider.Decrypt(Convert.FromBase64String(encryptedIV), _oaep);

			// Return the encryption provider
			return sharedProvider;
		}

		/// <summary>
		/// Encrypts the message.
		/// </summary>
		/// <param name="requestHeaders">Request transport headers.</param>
		/// <param name="requestStream">Request stream.</param>
		private Stream EncryptMessage(ITransportHeaders requestHeaders, Stream requestStream)
		{
			// Encrypt message using the symmetric encryption algorithm
			requestStream = CryptoTools.GetEncryptedStream(requestStream, _provider);

			// Send the current secure transaction stage and identifier in the transport headers
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingEncryptedMessage).ToString();
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_ID] = _secureTransactionID.ToString();

			// Return the encrypted data stream
			return requestStream;
		}

		private bool NextSinkProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			try
			{
				// Pass the encrypted message to the next sink
				_next.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);
				return true;
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Exception in CryptoClientChannelSink: {0}", ex.ToString());

				// ProcessMessage shouldn't throw exceptions
				responseStream = null;
				responseHeaders = null;
				return false;
			}
		}


		/// <summary>
		/// Processes the encrypted message.
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="requestHeaders">Request transport headers.</param>
		/// <param name="requestStream">Request stream.</param>
		/// <param name="responseHeaders">Response transport headers.</param>
		/// <param name="responseStream">Response stream.</param>
		private bool ProcessEncryptedMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			Guid secureTransactionID;

			lock (_lockObject)
			{
				// Start a new security transaction and save its identifier
				secureTransactionID = StartSecureTransaction(msg, requestHeaders);

				// Enctypt the message
				requestStream = EncryptMessage(requestHeaders, requestStream);
			}

			// Pass the encrypted message to the next sink
			NextSinkProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);

			lock (_lockObject)
			{
				// Decrypt the response data stream
				responseStream = DecryptResponse(responseStream, responseHeaders);

				// If decryption failed, clear the shared key to re-establish a new secure transaction
				if (responseStream == null && secureTransactionID.Equals(_secureTransactionID))
					ClearSharedKey();
			}

			// Processing failed if the stream wasn't decrypted
			return responseStream != null;
		}

		/// <summary>
		/// Requests message processing from the current sink.
		/// </summary>
		/// <param name="msg">The message to process.</param>
		/// <param name="requestHeaders">The headers to add to the outgoing message heading to the server.</param>
		/// <param name="requestStream">The stream headed to the transport sink.</param>
		/// <param name="responseHeaders">When this method returns, contains a <see cref="T:System.Runtime.Remoting.Channels.ITransportHeaders" /> interface that holds the headers that the server returned. This parameter is passed uninitialized.</param>
		/// <param name="responseStream">When this method returns, contains a <see cref="T:System.IO.Stream" /> coming back from the transport sink. This parameter is passed uninitialized.</param>
		/// <exception cref="Zyan.Communication.ChannelSinks.Encryption.CryptoRemotingException"></exception>
		public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			try
			{
				// Save the current stream position
				long initialStreamPos = requestStream.CanSeek ? requestStream.Position : -1;

				// Try several times if necessary
				for (int i = 0; i < _maxAttempts; i++)
				{
					if (ProcessEncryptedMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream))
						return;

					// Reset the input stream position and retry
					if (requestStream.CanSeek)
						requestStream.Position = initialStreamPos;
					else
						break;
				}

				throw new CryptoRemotingException(DEFAULT_EXCEPTION_TEXT);
			}
			finally
			{
				requestStream.Close();
			}
		}

		/// <summary>
		/// Returns the <see cref="T:System.IO.Stream" /> onto which the provided message is to be serialized.
		/// </summary>
		/// <param name="msg">The <see cref="T:System.Runtime.Remoting.Messaging.IMethodCallMessage" /> containing details about the method call.</param>
		/// <param name="headers">The headers to add to the outgoing message heading to the server.</param>
		/// <returns>
		/// The <see cref="T:System.IO.Stream" /> onto which the provided message is to be serialized.
		/// </returns>
		public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
		{
			// we don't use it
			return null;
		}

		/// <summary>
		/// Gets the next client channel sink in the client sink chain.
		/// </summary>
		/// <returns>The next client channel sink in the client sink chain.</returns>
		public IClientChannelSink NextChannelSink
		{
			get { return _next; }
		}

		#endregion

		#region Asynchronous processing

		/// <summary>
		/// Asynchronous processing state information.
		/// </summary>
		private class AsyncProcessingState
		{
			// Input data stream
			private Stream _stream;

			// Transport headers
			private ITransportHeaders _headers;

			// Remoting message
			private IMessage _msg;

			// Unique identifier of the secure transaction
			private Guid _secureTransactionID;

			/// <summary>
			/// Initializes a new instance of the <see cref="AsyncProcessingState"/> class.
			/// </summary>
			/// <param name="msg">The message.</param>
			/// <param name="headers">Transport headers.</param>
			/// <param name="stream">Input stream.</param>
			/// <param name="id">Secure transaction identifier.</param>
			public AsyncProcessingState(IMessage msg, ITransportHeaders headers, ref Stream stream, Guid id)
			{
				_msg = msg;
				_headers = headers;
				_stream = DuplicateStream(ref stream); // Copy the input stream
				_secureTransactionID = id;
			}

			/// <summary>
			/// Gets the input stream.
			/// </summary>
			public Stream Stream { get { return _stream; } }

			/// <summary>
			/// Gets the transport headers.
			/// </summary>
			public ITransportHeaders Headers { get { return _headers; } }

			/// <summary>
			/// Gets the remoting message.
			/// </summary>
			public IMessage Message { get { return _msg; } }

			/// <summary>
			/// Gets the secure transaction identifier.
			/// </summary>
			public Guid SecureTransactionID { get { return _secureTransactionID; } }

			/// <summary>
			/// Duplicates the given stream.
			/// </summary>
			/// <param name="stream">The input stream.</param>
			/// <returns>Stream copy.</returns>
			private Stream DuplicateStream(ref Stream stream)
			{
				MemoryStream memStream1 = new MemoryStream();
				MemoryStream memStream2 = new MemoryStream();

				byte[] buffer = new byte[1024];
				int readBytes;

				while ((readBytes = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					memStream1.Write(buffer, 0, readBytes);
					memStream2.Write(buffer, 0, readBytes);
				}

				// Close input stream
				stream.Close();

				// Reset the stream positions
				memStream1.Position = 0;
				memStream2.Position = 0;

				// Replace the original stream with the first copy
				stream = memStream1;

				// Return the second copy
				return memStream2;
			}
		}

		/// <summary>
		/// Requests asynchronous processing of a method call on the current sink.
		/// </summary>
		/// <param name="sinkStack">A stack of channel sinks that called this sink.</param>
		/// <param name="msg">The message to process.</param>
		/// <param name="headers">The headers to add to the outgoing message heading to the server.</param>
		/// <param name="stream">The stream headed to the transport sink.</param>
		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			AsyncProcessingState state = null;
			Stream encryptedStream = null;
			Guid _secureTransactionID;

			lock (_lockObject)
			{
				// Start the secure transaction and save its identifier
				_secureTransactionID = StartSecureTransaction(msg, headers);

				// Prepare asynchronous state
				state = new AsyncProcessingState(msg, headers, ref stream, _secureTransactionID);

				// Encrypt the message
				encryptedStream = EncryptMessage(headers, stream);
			}

			// Push the current sink onto the sink stack so the processing of the response can be invoked asynchronously later
			sinkStack.Push(this, state);

			// Pass the message on to the next sink
			_next.AsyncProcessRequest(sinkStack, msg, headers, encryptedStream);
		}

		/// <summary>
		/// Requests asynchronous processing of a response to a method call on the current sink.
		/// </summary>
		/// <param name="sinkStack">A stack of sinks that called this sink.</param>
		/// <param name="state">Information generated on the request side that is associated with this sink.</param>
		/// <param name="headers">The headers retrieved from the server response stream.</param>
		/// <param name="stream">The stream coming back from the transport sink.</param>
		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
		{
			// Gets the asynchronous processing state
			AsyncProcessingState asyncState = (AsyncProcessingState)state;

			try
			{
				SecureTransactionStage currentStage = (SecureTransactionStage)Convert.ToInt32((string)headers[CommonHeaderNames.SECURE_TRANSACTION_STATE]);
				switch (currentStage)
				{
					case SecureTransactionStage.SendingEncryptedResult: // Get the encrypted response from the server

						lock (_lockObject)
						{
							if (asyncState.SecureTransactionID.Equals(_secureTransactionID))
								stream = DecryptResponse(stream, headers);
							else
								throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_KeyChanged);
						}
						break;

					case SecureTransactionStage.UnknownTransactionID: // Bad transaction identifier

						throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_InvalidTransactionID);

					default:

					case SecureTransactionStage.Uninitialized: // Secure transaction is not yet set up
						break;
				}
			}
			catch (CryptoRemotingException)
			{
				lock (_lockObject)
				{
					// If remote transaction identifier matches the local secure transaction identifier, reset the shared key
					if (_provider == null || asyncState.SecureTransactionID.Equals(_secureTransactionID))
						ClearSharedKey();

					ProcessMessage(asyncState.Message, asyncState.Headers, asyncState.Stream, out headers, out stream);
				}
			}
			finally
			{
				// Close the input stream
				asyncState.Stream.Close();
			}

			// Pass on to the next sink to continue processing
			sinkStack.AsyncProcessResponse(headers, stream);
		}

		#endregion
	}
}