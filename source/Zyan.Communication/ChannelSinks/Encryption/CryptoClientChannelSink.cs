using System;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
	/// Clientseitige Kanalsenke f�r verschl�sselte Kommunikation.
    /// <remarks>
	/// Ben�tigt auf der Serverseite CryptoServerChannelSink als Gegenst�ck!
    /// </remarks>
	/// </summary>
	internal class CryptoClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink
    {
        #region Deklarationen

        // Name des symmetrischen Verschl�sselungsalgorithmus
		private readonly string _algorithm;
		
        // Schalter f�r OAEP-Padding
		private readonly bool _oaep;
		
        // Maximale Anzahl der Verarbeitungsversuche
		private readonly int _maxAttempts;
		
        // N�chste Kanalsenke
		private readonly IClientChannelSink _next;

        // Eindeutige Kennung der Sicherheitstransaktion
		private Guid _secureTransactionID = Guid.Empty;
		
        // Symmetrischer Verschl�sselungsanbieter
		private volatile SymmetricAlgorithm _provider = null;
		
        // Anbieter f�r asymmetrische Verschl�sselung
		private volatile RSACryptoServiceProvider _rsaProvider = null;
		
        // Sperr-Objekt (f�r Thread-Synchronisierung)
		private readonly object _lockObject = null;
		
        // Standard-Ausnahmetext
        private const string DEFAULT_EXCEPTION_TEXT = "Die clientseitige Kanalsenke konnte keine verschl�sselte Verbindung zum Server herstellen.";
		
        #endregion

		#region Konstruktoren

		/// <summary>Erstellt eine neue Instanz von CryptoClientChannelSink</summary>
        /// <param name="nextSink">N�chste Kanalsenke in der Senkenkette</param>
        /// <param name="algorithm">Name des symmetrischen Verschl�sselungsalgorithmus</param>
        /// <param name="oaep">Gibt an, ob OAEP-Padding verwendet werden soll, oder nicht</param>
        /// <param name="maxAttempts">Maximale Anzahl der Verarbeitungsversuche</param>
		public CryptoClientChannelSink(IClientChannelSink nextSink, string algorithm, bool oaep, int maxAttempts)
		{ 
            // Werte �bernehmen
			_algorithm = algorithm;
			_oaep = oaep;
			_next = nextSink;
			_maxAttempts = maxAttempts;

            // Sperrobjekt erzeugen
			_lockObject = new object();
			
            // Asymmetrischen Verschl�sselungsanbieter erzeugen
            _rsaProvider = new RSACryptoServiceProvider();
		}

		#endregion

		#region Synchrone Verarbeitung

        /// <summary>Startet eine neue Sicherheitstransaktion</summary>
        /// <param name="msg">Remoting-Nachricht</param>
        /// <param name="requestHeaders">Anfrage-Header-Auflistung</param>
        /// <returns>Eindeutige Kennung der Sicherheitstransaktion</returns>		
        private Guid StartSecureTransaction(IMessage msg, ITransportHeaders requestHeaders)
        {
            // Wenn noch keine kein Sicherheitstransaktion l�uft ...
            if (_provider == null || _secureTransactionID.Equals(Guid.Empty))
            {
                // Neue eindeutige Kennung generieren
                _secureTransactionID = Guid.NewGuid();

                // Gemeinsamen Schl�ssel vom Server anfordern
                _provider = ObtainSharedKey(msg);
            }
            return _secureTransactionID;
        }

        /// <summary>
        /// Fordert einen gemeinsamen Schl�ssel vom Server an.
        /// </summary>
        /// <param name="msg">Original-Remoting-Nachricht</param>
        /// <returns>Verschl�sselungsanbieter</returns>
        private SymmetricAlgorithm ObtainSharedKey(IMessage msg)
        {
            // Anfrage-Transport-Header-Auflistung erzeugen
            TransportHeaders requestHeaders = new TransportHeaders();

            // Anfrage-Datenstrom erzeugen
            MemoryStream requestStream = new MemoryStream();

            // Variable f�r Antwort-Header-Auflistung
            ITransportHeaders responseHeaders;

            // Variable f�r Antwort-Datenstrom
            Stream responseStream;

            // Anfrage nach Gemeinsamen Schl�ssel erzeugen
            CreateSharedKeyRequest(requestHeaders);

            // Anforderungsnachricht f�r gemeinsamen Schl�ssel �ber die Senkenkette versenden
            _next.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);

            // Antwort vom Server verarbeiten
            return ProcessSharedKeyResponse(responseHeaders);
        }

        /// <summary>
        /// L�scht den gemeinsamen Schl�ssel und die eindeutige Sicherheitstransaktionskennung.
        /// </summary>		
        private void ClearSharedKey()
        {
            // Verschl�sselungsanbieter zur�cksetzen
            _provider = null;

            // Eindeutige Sicherheitstransaktionskennung zur�cksetzen
            _secureTransactionID = Guid.Empty;
        }

        /// <summary>Erzeugt eine Anfrage nach einem gemeinsamen Schl�ssel</summary>
		/// <param name="requestHeaders">Transport-Header-Auflistung</param>
		private void CreateSharedKeyRequest(ITransportHeaders requestHeaders)
		{
			// Gemeinsamen RSA-Schl�ssel erzeugen
			string rsaKey = _rsaProvider.ToXmlString(false);

			// Gemeinsamen Schl�ssel und Zusatzinformationen �ber die aktuelle Sicherheitstransaktion der Transport-Header-Auflistung zuf�gen
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingPublicKey).ToString();
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_ID] = _secureTransactionID.ToString();
			requestHeaders[CommonHeaderNames.PUBLIC_KEY] = rsaKey;
		}

		/// <summary>Entschl�sselt den eingehenden Antwort-Datenstrom</summary>
		/// <param name="responseStream">Antwort-Datenstrom</param>
		/// <param name="responseHeaders">Antwort-Transportheader</param>
		/// <returns>Entschl�sselter Datenstrom (oder null, wenn die Verschl�sselung fehlgeschlagen ist)</returns>
		private Stream DecryptResponse(Stream responseStream, ITransportHeaders responseHeaders)
		{
			try 
			{								
                // Wenn laut Header verschl�sselte Daten vom Server zur�ckgesendet wurden ...
                if (responseHeaders != null && (SecureTransactionStage)Convert.ToInt32((string)responseHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE]) == SecureTransactionStage.SendingEncryptedResult) 
				{
                    // Antwort-Datenstrom entschl�sseln
					Stream decryptedStream = CryptoTools.GetDecryptedStream(responseStream, _provider);
					responseStream.Close(); // close the old stream as we won't be using it anymore
					
                    // Entschl�sselten Datenstrom zur�ckgeben
                    return decryptedStream;
				} 
			} 
			catch{}

            // Nichts zur�ckgeben
			return null;
		}

		/// <summary>Verarbeitet die Antwort der Servers einer Anfrage nach einen Gemeinsamen Schl�ssel</summary>
		/// <param name="responseHeaders">Transport-Header-Auflistung</param>
        /// <returns>Verschl�sselungsanbieter</returns>
		private SymmetricAlgorithm ProcessSharedKeyResponse(ITransportHeaders responseHeaders)
		{
			// Gemeinsamen Schl�ssel und Inizialisierungsvektor asu den Antwort-Headern lesen
			string encryptedKey = (string)responseHeaders[CommonHeaderNames.SHARED_KEY];
			string encryptedIV = (string)responseHeaders[CommonHeaderNames.SHARED_IV];

            // Wenn kein gemeinsamer Schl�ssel �bermittelt wurde ...
            if (encryptedKey == null || encryptedKey == string.Empty) 
                // Ausnahme werfen
                throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_KeyChanged);
			
            // Wenn kein Inizialisierungsvektor �bermittelt wurde ...
            if (encryptedIV == null || encryptedIV == string.Empty)
                // Ausnahme werfen
                throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_IVMissing);

			// Gemeinsamen Schl�ssel und Inizialisierungsvektor entschl�sseln
			SymmetricAlgorithm sharedProvider = CryptoTools.CreateSymmetricCryptoProvider(_algorithm);
			sharedProvider.Key = _rsaProvider.Decrypt(Convert.FromBase64String(encryptedKey), _oaep);
			sharedProvider.IV = _rsaProvider.Decrypt(Convert.FromBase64String(encryptedIV), _oaep);

            // Verschl�sselungsanbieter zur�ckgeben
            return sharedProvider;
		}
        
		/// <summary>Verschl�sselt eine bestimmte Remoting-Nachricht</summary>
		/// <param name="requestHeaders">Anfrage-Transport-Header-Auflistung</param>
		/// <param name="requestStream">Anfrage-Datenstrom</param>
		/// <returns>Verschl�sselter Datenstrom</returns>
		private Stream EncryptMessage(ITransportHeaders requestHeaders, Stream requestStream)
		{
			// Nachricht verschl�sseln
			requestStream = CryptoTools.GetEncryptedStream(requestStream, _provider);

			// Statusinformationen �ber die Sicherheitstransaktion in die Header-Auflistung schreiben
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingEncryptedMessage).ToString();
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_ID] = _secureTransactionID.ToString();

			// Verschl�sselten Datenstrom zur�ckgeben
			return requestStream;
		}
		
		/// <summary>
		/// Verarbeitet die verschl�sselte Nachricht.
		/// </summary>
		/// <param name="msg">Original Remotingnachricht</param>
		/// <param name="requestHeaders">Original Anfrage-Header</param>
		/// <param name="requestStream">Original Anfrage-Datenstrom (unverschl�sselt)</param>
		/// <param name="responseHeaders">Antwort-Header</param>
		/// <param name="responseStream">Antwort-Datenstrom (unverschl�sselt nach Verarbeitung!)</param>
		/// <returns>Wahr, wenn Verarbeitung erfolgreich, ansonsten Falsch</returns>
		private bool ProcessEncryptedMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, 	out ITransportHeaders responseHeaders, out Stream responseStream)
		{	
            // Variable f�r Sicherheitstransaktionskennung
            Guid secureTransactionID;

			lock(_lockObject) 
			{
                // Neue Sicherheitstransaktion starten und Kennung speichern
				secureTransactionID = StartSecureTransaction(msg, requestHeaders);
				
                // Nachricht verschl�sseln
                requestStream = EncryptMessage(requestHeaders, requestStream);
			}
            // Verschl�sselte Anfragenachricht zum Server senden
			_next.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);
            			
			lock(_lockObject)
			{
                // Antwort-Datenstrom entschl�sselm
				responseStream = DecryptResponse(responseStream, responseHeaders);
				
                // Wenn kein Antwort-Datenstrom f�r diese Sicherheitstransaktion empfangen wurde ...
                if (responseStream == null && secureTransactionID.Equals(_secureTransactionID)) 
                    // Gemeinsamen Schl�ssel und Sitzungsinformationen der Sicherheitstransaktion l�schen
                    ClearSharedKey();
			}
			// Zur�ckgeben, ob ein Antwort-Datenstrom empfangen wurde, oder nicht
			return responseStream != null;
		}

		/// <summary>Verarbeitet eine bestimmte Remoting-Nachricht</summary>
		/// <param name="msg">Remoting-Nachricht</param>
		/// <param name="requestHeaders">Anfrage-Header-Auflistung</param>
		/// <param name="requestStream">Anfrage-Datenstrom</param>
		/// <param name="responseHeaders">Antwort-Header-Auflistung</param>
		/// <param name="responseStream">Antwort-Datenstrom</param>		
		public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			try 
			{
				// Aktuelle Position des Datenstroms speichern
				long initialStreamPos = requestStream.CanSeek ? requestStream.Position : -1;

				// Ggf. mehrere Male versuchen (h�chstens bis maximal konfigurierte Anzahl)
				for(int i=0; i<_maxAttempts; i++) 
				{
					// Wenn die verschl�sselte Nachricht erfolgreich verarbeitet werden konnte ...
					if (ProcessEncryptedMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream)) 
                        // Prozedur verlassen                        
                        return;

					// Wenn der Datenstrom noch zugreifbar ist ...
					if (requestStream.CanSeek) 
                        // Datenstrom aus gespeicherte Anfangsposition zur�cksetzen
                        requestStream.Position = initialStreamPos;
                    else 
                        break;
				}
				// Ausnahme werfen
				throw new CryptoRemotingException(DEFAULT_EXCEPTION_TEXT);
			}
			finally
			{
				// Anfrage-Datenstrom schlie�en
				requestStream.Close();
			}
		}

        /// <summary>
        /// Gibt den Anfrage-Datenstrom zur�ck.
        /// </summary>
        /// <param name="msg">Remoting-Nachricht</param>
        /// <param name="headers">Header-Informationen</param>
        /// <returns>Anfrage-Datenstrom</returns>
        public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
        {
            // Immer null zur�ckgeben (Diese Funktion wird nicht ben�tigt)
            return null;
        }

		/// <summary>
        /// Gibt die n�chste Kanalsenke in der Senkenkette zur�ck.
		/// </summary>
		public IClientChannelSink NextChannelSink
		{
			get { return _next; }
		}

		#endregion

		#region Asynchrone Verarbeitung

		/// <summary>
        /// Speichert Informationen �ber den asynchronen Verarbeitungsstatus.
        /// </summary>
		private class AsyncProcessingState
		{
			// Eingabe-Datenstrom
			private Stream _stream;
			
            // Header-Auflistung
			private ITransportHeaders _headers;
			
            // Remoting-Nachricht
			private IMessage _msg;
			
            // Eindeutige Kennung der Sicherheitstransaktion
			private Guid _secureTransactionID;

            /// <summary>Erzeugt eine neue Instanz von AsyncProcessingState</summary>
			/// <param name="msg">Remoting-Nachricht</param>
			/// <param name="headers">Header-Auflistung</param>
			/// <param name="stream">Eingabe-Datenstrom</param>
            /// <param name="id">Eindeutige Kennung der Sicherheitstransaktion</param>
			public AsyncProcessingState(IMessage msg, ITransportHeaders headers, ref Stream stream, Guid id)
			{
                // Werte �bernehmen
				_msg = msg;
				_headers = headers;
				_stream = DuplicateStream(ref stream); // Datenstrom kopieren
				_secureTransactionID = id;
			}
            
			/// <summary>
			/// Gibt den Eingabedatenstrom zur�ck.
			/// </summary>
			public Stream Stream { get { return _stream; } }
			
            /// <summary>
            /// Gibt die Header-Auflistung zur�ck.
            /// </summary>
			public ITransportHeaders Headers { get { return _headers; } }
			
            /// <summary>
            /// Gibt die Remoting-Nachricht zur�ck.
            /// </summary>
			public IMessage Message { get { return _msg; } }
			
            /// <summary>
            /// Gibt die eindeutige Kennung der Sicherheitstransaktion zur�ck.
            /// </summary>
			public Guid SecureTransactionID { get { return _secureTransactionID; } }
						
            /// <summary>Kopiert einen bestimmten Datenstrom</summary>
			/// <param name="stream">Datenstrom</param>
			/// <returns>Kopie des Datenstroms</returns>			
			private Stream DuplicateStream(ref Stream stream)
			{				
				// Variablen f�r Speicherdatenstr�me
				MemoryStream memStream1 = new MemoryStream();
				MemoryStream memStream2 = new MemoryStream();
				
				// 1 KB Puffer erzeugen
				byte [] buffer = new byte[1024];
				
                // Anzahl der gelesenen Bytes
                int readBytes;

                // Eingabe-Datenstrom durchlaufen
				while((readBytes = stream.Read(buffer, 0, buffer.Length)) > 0) 
				{
                    // Puffer in beide Speicherdatenstr�me kopieren
					memStream1.Write(buffer, 0, readBytes);
					memStream2.Write(buffer, 0, readBytes);
				}
                // Eingabe-Datenstrom schlie�en
				stream.Close();

				// Position der Speicherdatenstr�me zur�cksetzen
				memStream1.Position = 0;
				memStream2.Position = 0;

				// Original-Datenstrom durch 1. Kopie ersetzen
				stream = memStream1;
				
                // 2. Kopie zur�ckgeben
                return memStream2;
			}			
		}

		/// <summary>
        /// Verarbeitet eine Anfragenachricht asynchron.
        /// </summary>
		/// <param name="sinkStack">Senkenstapel</param>
		/// <param name="msg">Remoting-Nachricht</param>
		/// <param name="headers">Anfrage-Header-Auflistung</param>
		/// <param name="stream">Anfrage-Datenstrom</param>
		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream)
		{
            // Asynchroner Verarbeitungsstatus
			AsyncProcessingState state = null;
			
            // Verschl�sselter Datenstrom
            Stream encryptedStream = null;
			
            // Eindeutige Kennung der Sicherheitstransaktion
            Guid _secureTransactionID;

			lock(_lockObject)
			{
				// Sicherheitstransaktion starten
				_secureTransactionID = StartSecureTransaction(msg, headers);
			
				// Asynchronen Verarbeitungsstatus erzeugen
				state = new AsyncProcessingState(msg, headers, ref stream, _secureTransactionID);
			
				// Nachricht verschl�sseln
				encryptedStream = EncryptMessage(headers, stream);
			}
            // Aktuelle Senke auf den Senkenstapel legen (Damit ggf. die Verarbeitung der Antwort sp�ter asynchron aufgerufen werden kann)
			sinkStack.Push(this, state);

            // N�chste Kanalsenke aufrufen
			_next.AsyncProcessRequest(sinkStack, msg, headers, encryptedStream);
		}

		/// <summary>
        /// Verarbeitet eine Antwort-Nachricht asynchron.
        /// </summary>
        /// <param name="sinkStack">Senkenstapel</param>
		/// <param name="state">Asynchroner Verarbeitungsstatus</param>
        /// <param name="headers">Anfrage-Header-Auflistung</param>
        /// <param name="stream">Anfrage-Datenstrom</param>
		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
		{
			// Asychronen Verarbeitungsstatus abrufen
			AsyncProcessingState asyncState = (AsyncProcessingState)state;

			try
			{
				// Aktuellen Verarbeitungsschritt der Sicherheitstransaktion ermitteln
				SecureTransactionStage currentStage = (SecureTransactionStage)Convert.ToInt32((string)headers[CommonHeaderNames.SECURE_TRANSACTION_STATE]);
				
                // Verarbeitungsschritt auswerten
                switch(currentStage) 
				{
                    case SecureTransactionStage.SendingEncryptedResult: // Verschl�sselte Daten vom Server eingtroffen
						
                        lock(_lockObject)
						{
                            // Wenn die Antwort auch tats�chlich zur aktuellen Sicherheitstransaktion geh�rt ...
							if (asyncState.SecureTransactionID.Equals(_secureTransactionID)) 
                                // Datenstrom entschl�sseln
                                stream = DecryptResponse(stream, headers);
							// Andernfalls ...
                            else 
                                // Ausnahme werfen
                                throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_KeyChanged);
						}
						break;

					case SecureTransactionStage.UnknownTransactionID: // Unbekannte Transaktionskennung
						
                        // Ausnahme werfen
                        throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_InvalidTransactionID);

					default:
					
                    case SecureTransactionStage.Uninitialized: // Keine Sicherheitstransaktion eingerichtet
						break;
				}
			}
			catch(CryptoRemotingException)
			{				
				lock(_lockObject)
				{
                    // Wenn die gesendete Transaktionskennung mit der lokalen �bereinstimmt ...
					if (_provider == null || asyncState.SecureTransactionID.Equals(_secureTransactionID)) 
                        // Gemeinamen Schl�ssel l�schen
                        ClearSharedKey();
					
                    // Nachricht weiterverarbeiten
                    ProcessMessage(asyncState.Message, asyncState.Headers, asyncState.Stream,out headers, out stream);
				}
			}
			finally
			{
				// Datenstrom schlie�en
				asyncState.Stream.Close();
			}
			// Verarbeitung in der n�chsten Kanalsenke fortsetzen
			sinkStack.AsyncProcessResponse(headers, stream);
		}

		#endregion
	}
}