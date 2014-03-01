using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Timers;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
	/// Server-side channel sink for encrypted communication. 
	/// </summary>
	/// <remarks>
	/// Requires the client-side CryptoClientChannelSink counterpart.
	/// </remarks>
	internal class CryptoServerChannelSink : BaseChannelSinkWithProperties, IServerChannelSink
	{
		#region Fields

		// Symmetric encryption algorithm name
		private readonly string _algorithm;

		// OAEP padding switch
		private readonly bool _oaep;

		// Maximal client connection lifetime, in seconds
		private readonly double _connectionAgeLimit;

		// Client connection sweeping interval, in seconds
		private readonly double _sweepFrequency;

		// Connection sweeping timer
		private System.Timers.Timer _sweepTimer = null;

		// Specifies whether the corresponding encryption channel sinks need to be present on the client side
		private readonly bool _requireCryptoClient;

		// Client IP addresses that don't require encryption
		private IPAddress[] _securityExemptionList;

		// List of active client connections
		private readonly Hashtable _connections = null;

		// Next sink in the sink chain
		private readonly IServerChannelSink _next = null;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoServerChannelSink"/> class.
		/// </summary>
		/// <param name="nextSink">The next sink.</param>
		/// <param name="algorithm">Symmetric encryption algorithm.</param>
		/// <param name="oaep">if set to <c>true</c>, OAEP padding is enabled.</param>
		/// <param name="connectionAgeLimit">Connection age limit.</param>
		/// <param name="sweeperFrequency">Connection sweeper frequency.</param>
		/// <param name="requireCryptoClient">if set to <c>true</c>, crypto client sink is required.</param>
		/// <param name="securityExemptionList">Security exemption list.</param>
		public CryptoServerChannelSink(IServerChannelSink nextSink, string algorithm, bool oaep, double connectionAgeLimit, double sweeperFrequency, bool requireCryptoClient, IPAddress[] securityExemptionList)
		{
			_algorithm = algorithm;
			_oaep = oaep;
			_connectionAgeLimit = connectionAgeLimit;
			_sweepFrequency = sweeperFrequency;
			_requireCryptoClient = requireCryptoClient;
			_securityExemptionList = securityExemptionList;
			_next = nextSink;
			_connections = new Hashtable(103, 0.5F);

			StartConnectionSweeper();
		}

		#endregion

		#region Synchronous processing

		/// <summary>
		/// Creates the shared key.
		/// </summary>
		/// <param name="transactID">Secure transaction identifier.</param>
		/// <param name="requestHeaders">Request transport headers.</param>
		/// <param name="responseMsg">The response message.</param>
		/// <param name="responseHeaders">The response headers.</param>
		/// <param name="responseStream">The response stream.</param>
		private ServerProcessing MakeSharedKey(Guid transactID, ITransportHeaders requestHeaders, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// save shared symmetric encryption key and initialization vector for the current client connection
			SymmetricAlgorithm symmetricProvider = CryptoTools.CreateSymmetricCryptoProvider(_algorithm);
			ClientConnectionData connectionData = new ClientConnectionData(transactID, symmetricProvider);

			// Store client data connection under the specified security transaction identifier
			lock (_connections.SyncRoot)
			{
				_connections[transactID.ToString()] = connectionData;
			}

			// Get client's RSA public key
			string publicKey = (string)requestHeaders[CommonHeaderNames.PUBLIC_KEY];
			if (string.IsNullOrEmpty(publicKey))
				throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_PublicKeyNotFound);

			// Initialize RSA cryptographic provider using the client's public key
			RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider();
			rsaProvider.FromXmlString(publicKey);

			// Encrypt shared key for the symmetric algorithm using the client's public key
			byte[] encryptedKey = rsaProvider.Encrypt(symmetricProvider.Key, _oaep);
			byte[] encryptedIV = rsaProvider.Encrypt(symmetricProvider.IV, _oaep);

			// Put the data to the response headers
			responseHeaders = new TransportHeaders();
			responseHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingSharedKey).ToString();
			responseHeaders[CommonHeaderNames.SHARED_KEY] = Convert.ToBase64String(encryptedKey);
			responseHeaders[CommonHeaderNames.SHARED_IV] = Convert.ToBase64String(encryptedIV);

			// There is no response message
			responseMsg = null;
			responseStream = new MemoryStream();
			return ServerProcessing.Complete;
		}

		/// <summary>
		/// Processes the encrypted message.
		/// </summary>
		/// <param name="transactID">Secure transaction identifier.</param>
		/// <param name="sinkStack">The sink stack.</param>
		/// <param name="requestMsg">Request message.</param>
		/// <param name="requestHeaders">Request transport headers.</param>
		/// <param name="requestStream">Request stream.</param>
		/// <param name="responseMsg">Response message.</param>
		/// <param name="responseHeaders">Response transport headers.</param>
		/// <param name="responseStream">Response stream.</param>
		public ServerProcessing ProcessEncryptedMessage(Guid transactID, IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// Get the client connection data
			ClientConnectionData connectionData;
			lock (_connections.SyncRoot)
			{
				connectionData = (ClientConnectionData)_connections[transactID.ToString()];
			}

			if (connectionData == null)
				throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_ClientConnectionInfoMissing);

			// Update the timestamp and indicate that method call is in progress
			connectionData.UpdateTimestamp();
			connectionData.BeginMethodCall();

			try
			{
				// Decrypt the data stream
				Stream decryptedStream = CryptoTools.GetDecryptedStream(requestStream, connectionData.CryptoProvider);
				requestStream.Close();

				// Pass decrypted message for further processing to the next channel sink
				ServerProcessing processingResult = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders, decryptedStream, out responseMsg, out responseHeaders, out responseStream);

				// Update secure transaction state
				responseHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingEncryptedResult).ToString();

				// Encrypt the response stream and close the original response stream now that we're done with it
				Stream encryptedStream = CryptoTools.GetEncryptedStream(responseStream, connectionData.CryptoProvider);
				responseStream.Close(); // 

				// Use encrypted data stream as a response stream
				responseStream = encryptedStream;
				return processingResult;
			}
			finally
			{
				// Method call is finished, so the connection data can be swept
				connectionData.EndMethodCall();
				connectionData.UpdateTimestamp();
			}
		}

		/// <summary>
		/// Determines whether the specified security transaction exists.
		/// </summary>
		/// <param name="transactID">Secure transaction identifier.</param>
		private bool IsExistingSecurityTransaction(Guid transactID)
		{
			lock (_connections.SyncRoot)
			{
				return (!transactID.Equals(Guid.Empty) && _connections[transactID.ToString()] != null);
			}
		}

		/// <summary>
		/// Creates an empty response message.
		/// </summary>
		/// <param name="sinkStack">The sink stack.</param>
		/// <param name="requestMsg">Request message.</param>
		/// <param name="requestHeaders">Request transport headers.</param>
		/// <param name="requestStream">Request stream.</param>
		/// <param name="transactionStage">Current secure transaction stage.</param>
		/// <param name="responseMsg">Response message.</param>
		/// <param name="responseHeaders">Response transport headers.</param>
		/// <param name="responseStream">Response stream.</param>
		/// <returns></returns>
		private ServerProcessing SendEmptyToClient(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, SecureTransactionStage transactionStage, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			responseMsg = null;
			requestStream = new MemoryStream();
			responseStream = new MemoryStream();

			responseHeaders = new TransportHeaders();
			responseHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)transactionStage).ToString();

			ServerProcessing processingResult = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream, out responseMsg, out responseHeaders, out responseStream);
			return processingResult;
		}

		/// <summary>
		/// Requests message processing from the current sink.
		/// </summary>
		/// <param name="sinkStack">A stack of channel sinks that called the current sink.</param>
		/// <param name="requestMsg">The message that contains the request.</param>
		/// <param name="requestHeaders">Headers retrieved from the incoming message from the client.</param>
		/// <param name="requestStream">The stream that needs to be to processed and passed on to the deserialization sink.</param>
		/// <param name="responseMsg">When this method returns, contains a <see cref="T:System.Runtime.Remoting.Messaging.IMessage" /> that holds the response message. This parameter is passed uninitialized.</param>
		/// <param name="responseHeaders">When this method returns, contains a <see cref="T:System.Runtime.Remoting.Channels.ITransportHeaders" /> that holds the headers that are to be added to return message heading to the client. This parameter is passed uninitialized.</param>
		/// <param name="responseStream">When this method returns, contains a <see cref="T:System.IO.Stream" /> that is heading back to the transport sink. This parameter is passed uninitialized.</param>
		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// Read secure transaction identifier from request headers
			string strTransactID = (string)requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_ID];
			Guid transactID = (strTransactID == null ? Guid.Empty : new Guid(strTransactID));

			// Read current transaction step and client IP address from request headers
			SecureTransactionStage transactionStage = (SecureTransactionStage)Convert.ToInt32((string)requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE]);
			IPAddress clientAddress = requestHeaders[CommonTransportKeys.IPAddress] as IPAddress;

			// Put current channel sink to the sink stack so AsyncProcessResponse can be called asynchronously if necessary order
			sinkStack.Push(this, null);
			ServerProcessing processingResult;

			try
			{
				switch (transactionStage)
				{
					case SecureTransactionStage.SendingPublicKey: // Client sends the public key to the server

						// Generate shared key and encrypt with the public key of the client
						processingResult = MakeSharedKey(transactID, requestHeaders, out responseMsg, out responseHeaders, out responseStream);

						break;

					case SecureTransactionStage.SendingEncryptedMessage: // Client sends the encrypted request message to the server

						if (IsExistingSecurityTransaction(transactID))
							processingResult = ProcessEncryptedMessage(transactID, sinkStack, requestMsg, requestHeaders, requestStream, out responseMsg, out responseHeaders, out responseStream);
						else
							throw new CryptoRemotingException(string.Format(LanguageResource.CryptoRemotingException_InvalidClientRequest, SecureTransactionStage.UnknownTransactionID));

						break;

					case SecureTransactionStage.Uninitialized: // Uninitialized, nothing has happened

						// Check if encryption is not required for this client address
						if (!RequireEncryption(clientAddress))
							processingResult = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream, out responseMsg, out responseHeaders, out responseStream);
						else
							throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_ServerRequiresEncryption);

						break;

					default:

						throw new CryptoRemotingException(string.Format(LanguageResource.CryptoRemotingException_InvalidClientRequest, transactionStage));
				}
			}
			catch (CryptoRemotingException)
			{
				processingResult = SendEmptyToClient(sinkStack, requestMsg, requestHeaders, requestStream,
					transactionStage, out responseMsg, out responseHeaders, out responseStream);
				requestMsg = null;
			}

			// Pop the current sink from the sink stack
			sinkStack.Pop(this);
			return processingResult;
		}

		private IServerChannelSink GetFormatter(IServerChannelSink sink)
		{
			var nextSink = sink.NextChannelSink;

			if (nextSink == null)
				return null;

			var sinkType = nextSink.GetType();

			if (sinkType.Name.Contains("FormatterSink"))
				return nextSink;
			else
				return GetFormatter(nextSink);
		}

		/// <summary>
		/// Checks whether the given IP address requires the encryption.
		/// </summary>
		/// <param name="clientAddress">The client address.</param>
		private bool RequireEncryption(IPAddress clientAddress)
		{
			if (clientAddress == null || _securityExemptionList == null || _securityExemptionList.Length == 0)
				return _requireCryptoClient;

			bool found = false;

			foreach (IPAddress address in _securityExemptionList)
			{
				if (clientAddress.Equals(address))
				{
					found = true;
					break;
				}
			}

			return found ? !_requireCryptoClient : _requireCryptoClient;
		}

		/// <summary>
		/// Gets the next server channel sink in the server sink chain.
		/// </summary>
		/// <returns>The next server channel sink in the server sink chain.</returns>
		public IServerChannelSink NextChannelSink
		{
			get { return _next; }
		}

		/// <summary>
		/// Returns the <see cref="T:System.IO.Stream" /> onto which the provided response message is to be serialized.
		/// </summary>
		/// <param name="sinkStack">A stack of sinks leading back to the server transport sink.</param>
		/// <param name="state">The state that has been pushed to the stack by this sink.</param>
		/// <param name="msg">The response message to serialize.</param>
		/// <param name="headers">The headers to put in the response stream to the client.</param>
		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers)
		{
			return null;
		}

		#endregion

		#region Asynchronous processing

		/// <summary>
		/// Requests processing from the current sink of the response from a method call sent asynchronously.
		/// </summary>
		/// <param name="sinkStack">A stack of sinks leading back to the server transport sink.</param>
		/// <param name="state">Information generated on the request side that is associated with this sink.</param>
		/// <param name="msg">The response message.</param>
		/// <param name="headers">The headers to add to the return message heading to the client.</param>
		/// <param name="stream">The stream heading back to the transport sink.</param>
		public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			sinkStack.AsyncProcessResponse(msg, headers, stream);
		}

		#endregion

		#region Inactive connection sweeper

		/// <summary>
		/// Starts the connection sweeper.
		/// </summary>
		private void StartConnectionSweeper()
		{
			if (_sweepTimer == null)
			{
				_sweepTimer = new System.Timers.Timer(_sweepFrequency * 1000);
				_sweepTimer.Elapsed += new ElapsedEventHandler(SweepConnections);
				_sweepTimer.Start();
			}
		}

		/// <summary>
		/// Sweeps the connections.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
		private void SweepConnections(object sender, ElapsedEventArgs e)
		{
			lock (_connections.SyncRoot)
			{
				// Connections to be swept
				ArrayList toDelete = new ArrayList(_connections.Count);

				foreach (DictionaryEntry entry in _connections)
				{
					ClientConnectionData connectionData = (ClientConnectionData)entry.Value;

					if (connectionData.Timestamp.AddSeconds(_connectionAgeLimit).CompareTo(DateTime.UtcNow) < 0 && !connectionData.CallInProgress)
					{
						toDelete.Add(entry.Key);
						((IDisposable)connectionData).Dispose();
					}
				}

				foreach (Object obj in toDelete)
				{
					_connections.Remove(obj);
				}
			}
		}

		#endregion
	}
}
