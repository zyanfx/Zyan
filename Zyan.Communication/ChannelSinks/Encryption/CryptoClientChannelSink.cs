using System;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
	/// Clientseitige Kanalsenke für verschlüsselte Kommunikation.
    /// <remarks>
	/// Benötigt auf der Serverseite CryptoServerChannelSink als Gegenstück!
    /// </remarks>
	/// </summary>
	internal class CryptoClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink
    {
        #region Deklarationen

        // Name des symmetrischen Verschlüsselungsalgorithmus
		private readonly string _algorithm;
		
        // Schalter für OAEP-Padding
		private readonly bool _oaep;
		
        // Maximale Anzahl der Verarbeitungsversuche
		private readonly int _maxAttempts;
		
        // Nächste Kanalsenke
		private readonly IClientChannelSink _next;

        // Eindeutige Kennung der Sicherheitstransaktion
		private Guid _secureTransactionID = Guid.Empty;
		
        // Symmetrischer Verschlüsselungsanbieter
		private volatile SymmetricAlgorithm _provider = null;
		
        // Anbieter für asymmetrische Verschlüsselung
		private volatile RSACryptoServiceProvider _rsaProvider = null;
		
        // Sperr-Objekt (für Thread-Synchronisierung)
		private readonly object _lockObject = null;
		
        // Standard-Ausnahmetext
        private const string DEFAULT_EXCEPTION_TEXT = "Die clientseitige Kanalsenke konnte keine verschlüsselte Verbindung zum Server herstellen.";
		
        #endregion

		#region Konstruktoren

		/// <summary>Erstellt eine neue Instanz von CryptoClientChannelSink</summary>
        /// <param name="nextSink">Nächste Kanalsenke in der Senkenkette</param>
        /// <param name="algorithm">Name des symmetrischen Verschlüsselungsalgorithmus</param>
        /// <param name="oaep">Gibt an, ob OAEP-Padding verwendet werden soll, oder nicht</param>
        /// <param name="maxAttempts">Maximale Anzahl der Verarbeitungsversuche</param>
		public CryptoClientChannelSink(IClientChannelSink nextSink, string algorithm, bool oaep, int maxAttempts)
		{ 
            // Werte übernehmen
			_algorithm = algorithm;
			_oaep = oaep;
			_next = nextSink;
			_maxAttempts = maxAttempts;

            // Sperrobjekt erzeugen
			_lockObject = new object();
			
            // Asymmetrischen Verschlüsselungsanbieter erzeugen
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
            // Wenn noch keine kein Sicherheitstransaktion läuft ...
            if (_provider == null || _secureTransactionID.Equals(Guid.Empty))
            {
                // Neue eindeutige Kennung generieren
                _secureTransactionID = Guid.NewGuid();

                // Gemeinsamen Schlüssel vom Server anfordern
                _provider = ObtainSharedKey(msg);
            }
            return _secureTransactionID;
        }

        /// <summary>
        /// Fordert einen gemeinsamen Schlüssel vom Server an.
        /// </summary>
        /// <param name="msg">Original-Remoting-Nachricht</param>
        /// <returns>Verschlüsselungsanbieter</returns>
        private SymmetricAlgorithm ObtainSharedKey(IMessage msg)
        {
            // Anfrage-Transport-Header-Auflistung erzeugen
            TransportHeaders requestHeaders = new TransportHeaders();

            // Anfrage-Datenstrom erzeugen
            MemoryStream requestStream = new MemoryStream();

            // Variable für Antwort-Header-Auflistung
            ITransportHeaders responseHeaders;

            // Variable für Antwort-Datenstrom
            Stream responseStream;

            // Anfrage nach Gemeinsamen Schlüssel erzeugen
            CreateSharedKeyRequest(requestHeaders);

            // Anforderungsnachricht für gemeinsamen Schlüssel über die Senkenkette versenden
            _next.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);

            // Antwort vom Server verarbeiten
            return ProcessSharedKeyResponse(responseHeaders);
        }

        /// <summary>
        /// Löscht den gemeinsamen Schlüssel und die eindeutige Sicherheitstransaktionskennung.
        /// </summary>		
        private void ClearSharedKey()
        {
            // Verschlüsselungsanbieter zurücksetzen
            _provider = null;

            // Eindeutige Sicherheitstransaktionskennung zurücksetzen
            _secureTransactionID = Guid.Empty;
        }

        /// <summary>Erzeugt eine Anfrage nach einem gemeinsamen Schlüssel</summary>
		/// <param name="requestHeaders">Transport-Header-Auflistung</param>
		private void CreateSharedKeyRequest(ITransportHeaders requestHeaders)
		{
			// Gemeinsamen RSA-Schlüssel erzeugen
			string rsaKey = _rsaProvider.ToXmlString(false);

			// Gemeinsamen Schlüssel und Zusatzinformationen über die aktuelle Sicherheitstransaktion der Transport-Header-Auflistung zufügen
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingPublicKey).ToString();
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_ID] = _secureTransactionID.ToString();
			requestHeaders[CommonHeaderNames.PUBLIC_KEY] = rsaKey;
		}

		/// <summary>Entschlüsselt den eingehenden Antwort-Datenstrom</summary>
		/// <param name="responseStream">Antwort-Datenstrom</param>
		/// <param name="responseHeaders">Antwort-Transportheader</param>
		/// <returns>Entschlüsselter Datenstrom (oder null, wenn die Verschlüsselung fehlgeschlagen ist)</returns>
		private Stream DecryptResponse(Stream responseStream, ITransportHeaders responseHeaders)
		{
			try 
			{								
                // Wenn laut Header verschlüsselte Daten vom Server zurückgesendet wurden ...
                if (responseHeaders != null && (SecureTransactionStage)Convert.ToInt32((string)responseHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE]) == SecureTransactionStage.SendingEncryptedResult) 
				{
                    // Antwort-Datenstrom entschlüsseln
					Stream decryptedStream = CryptoTools.GetDecryptedStream(responseStream, _provider);
					responseStream.Close(); // close the old stream as we won't be using it anymore
					
                    // Entschlüsselten Datenstrom zurückgeben
                    return decryptedStream;
				} 
			} 
			catch{}

            // Nichts zurückgeben
			return null;
		}

		/// <summary>Verarbeitet die Antwort der Servers einer Anfrage nach einen Gemeinsamen Schlüssel</summary>
		/// <param name="responseHeaders">Transport-Header-Auflistung</param>
        /// <returns>Verschlüsselungsanbieter</returns>
		private SymmetricAlgorithm ProcessSharedKeyResponse(ITransportHeaders responseHeaders)
		{
			// Gemeinsamen Schlüssel und Inizialisierungsvektor asu den Antwort-Headern lesen
			string encryptedKey = (string)responseHeaders[CommonHeaderNames.SHARED_KEY];
			string encryptedIV = (string)responseHeaders[CommonHeaderNames.SHARED_IV];

            // Wenn kein gemeinsamer Schlüssel übermittelt wurde ...
            if (encryptedKey == null || encryptedKey == string.Empty) 
                // Ausnahme werfen
                throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_KeyChanged);
			
            // Wenn kein Inizialisierungsvektor übermittelt wurde ...
            if (encryptedIV == null || encryptedIV == string.Empty)
                // Ausnahme werfen
                throw new CryptoRemotingException(LanguageResource.CryptoRemotingException_IVMissing);

			// Gemeinsamen Schlüssel und Inizialisierungsvektor entschlüsseln
			SymmetricAlgorithm sharedProvider = CryptoTools.CreateSymmetricCryptoProvider(_algorithm);
			sharedProvider.Key = _rsaProvider.Decrypt(Convert.FromBase64String(encryptedKey), _oaep);
			sharedProvider.IV = _rsaProvider.Decrypt(Convert.FromBase64String(encryptedIV), _oaep);

            // Verschlüsselungsanbieter zurückgeben
            return sharedProvider;
		}
        
		/// <summary>Verschlüsselt eine bestimmte Remoting-Nachricht</summary>
		/// <param name="requestHeaders">Anfrage-Transport-Header-Auflistung</param>
		/// <param name="requestStream">Anfrage-Datenstrom</param>
		/// <returns>Verschlüsselter Datenstrom</returns>
		private Stream EncryptMessage(ITransportHeaders requestHeaders, Stream requestStream)
		{
			// Nachricht verschlüsseln
			requestStream = CryptoTools.GetEncryptedStream(requestStream, _provider);

			// Statusinformationen über die Sicherheitstransaktion in die Header-Auflistung schreiben
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingEncryptedMessage).ToString();
			requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_ID] = _secureTransactionID.ToString();

			// Verschlüsselten Datenstrom zurückgeben
			return requestStream;
		}
		
		/// <summary>
		/// Verarbeitet die verschlüsselte Nachricht.
		/// </summary>
		/// <param name="msg">Original Remotingnachricht</param>
		/// <param name="requestHeaders">Original Anfrage-Header</param>
		/// <param name="requestStream">Original Anfrage-Datenstrom (unverschlüsselt)</param>
		/// <param name="responseHeaders">Antwort-Header</param>
		/// <param name="responseStream">Antwort-Datenstrom (unverschlüsselt nach Verarbeitung!)</param>
		/// <returns>Wahr, wenn Verarbeitung erfolgreich, ansonsten Falsch</returns>
		private bool ProcessEncryptedMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, 	out ITransportHeaders responseHeaders, out Stream responseStream)
		{	
            // Variable für Sicherheitstransaktionskennung
            Guid secureTransactionID;

			lock(_lockObject) 
			{
                // Neue Sicherheitstransaktion starten und Kennung speichern
				secureTransactionID = StartSecureTransaction(msg, requestHeaders);
				
                // Nachricht verschlüsseln
                requestStream = EncryptMessage(requestHeaders, requestStream);
			}
            // Verschlüsselte Anfragenachricht zum Server senden
			_next.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);
            			
			lock(_lockObject)
			{
                // Antwort-Datenstrom entschlüsselm
				responseStream = DecryptResponse(responseStream, responseHeaders);
				
                // Wenn kein Antwort-Datenstrom für diese Sicherheitstransaktion empfangen wurde ...
                if (responseStream == null && secureTransactionID.Equals(_secureTransactionID)) 
                    // Gemeinsamen Schlüssel und Sitzungsinformationen der Sicherheitstransaktion löschen
                    ClearSharedKey();
			}
			// Zurückgeben, ob ein Antwort-Datenstrom empfangen wurde, oder nicht
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

				// Ggf. mehrere Male versuchen (höchstens bis maximal konfigurierte Anzahl)
				for(int i=0; i<_maxAttempts; i++) 
				{
					// Wenn die verschlüsselte Nachricht erfolgreich verarbeitet werden konnte ...
					if (ProcessEncryptedMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream)) 
                        // Prozedur verlassen                        
                        return;

					// Wenn der Datenstrom noch zugreifbar ist ...
					if (requestStream.CanSeek) 
                        // Datenstrom aus gespeicherte Anfangsposition zurücksetzen
                        requestStream.Position = initialStreamPos;
                    else 
                        break;
				}
				// Ausnahme werfen
				throw new CryptoRemotingException(DEFAULT_EXCEPTION_TEXT);
			}
			finally
			{
				// Anfrage-Datenstrom schließen
				requestStream.Close();
			}
		}

        /// <summary>
        /// Gibt den Anfrage-Datenstrom zurück.
        /// </summary>
        /// <param name="msg">Remoting-Nachricht</param>
        /// <param name="headers">Header-Informationen</param>
        /// <returns>Anfrage-Datenstrom</returns>
        public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
        {
            // Immer null zurückgeben (Diese Funktion wird nicht benötigt)
            return null;
        }

		/// <summary>
        /// Gibt die nächste Kanalsenke in der Senkenkette zurück.
		/// </summary>
		public IClientChannelSink NextChannelSink
		{
			get { return _next; }
		}

		#endregion

		#region Asynchrone Verarbeitung

		/// <summary>
        /// Speichert Informationen über den asynchronen Verarbeitungsstatus.
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
                // Werte übernehmen
				_msg = msg;
				_headers = headers;
				_stream = DuplicateStream(ref stream); // Datenstrom kopieren
				_secureTransactionID = id;
			}
            
			/// <summary>
			/// Gibt den Eingabedatenstrom zurück.
			/// </summary>
			public Stream Stream { get { return _stream; } }
			
            /// <summary>
            /// Gibt die Header-Auflistung zurück.
            /// </summary>
			public ITransportHeaders Headers { get { return _headers; } }
			
            /// <summary>
            /// Gibt die Remoting-Nachricht zurück.
            /// </summary>
			public IMessage Message { get { return _msg; } }
			
            /// <summary>
            /// Gibt die eindeutige Kennung der Sicherheitstransaktion zurück.
            /// </summary>
			public Guid SecureTransactionID { get { return _secureTransactionID; } }
						
            /// <summary>Kopiert einen bestimmten Datenstrom</summary>
			/// <param name="stream">Datenstrom</param>
			/// <returns>Kopie des Datenstroms</returns>			
			private Stream DuplicateStream(ref Stream stream)
			{				
				// Variablen für Speicherdatenströme
				MemoryStream memStream1 = new MemoryStream();
				MemoryStream memStream2 = new MemoryStream();
				
				// 1 KB Puffer erzeugen
				byte [] buffer = new byte[1024];
				
                // Anzahl der gelesenen Bytes
                int readBytes;

                // Eingabe-Datenstrom durchlaufen
				while((readBytes = stream.Read(buffer, 0, buffer.Length)) > 0) 
				{
                    // Puffer in beide Speicherdatenströme kopieren
					memStream1.Write(buffer, 0, readBytes);
					memStream2.Write(buffer, 0, readBytes);
				}
                // Eingabe-Datenstrom schließen
				stream.Close();

				// Position der Speicherdatenströme zurücksetzen
				memStream1.Position = 0;
				memStream2.Position = 0;

				// Original-Datenstrom durch 1. Kopie ersetzen
				stream = memStream1;
				
                // 2. Kopie zurückgeben
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
			
            // Verschlüsselter Datenstrom
            Stream encryptedStream = null;
			
            // Eindeutige Kennung der Sicherheitstransaktion
            Guid _secureTransactionID;

			lock(_lockObject)
			{
				// Sicherheitstransaktion starten
				_secureTransactionID = StartSecureTransaction(msg, headers);
			
				// Asynchronen Verarbeitungsstatus erzeugen
				state = new AsyncProcessingState(msg, headers, ref stream, _secureTransactionID);
			
				// Nachricht verschlüsseln
				encryptedStream = EncryptMessage(headers, stream);
			}
            // Aktuelle Senke auf den Senkenstapel legen (Damit ggf. die Verarbeitung der Antwort später asynchron aufgerufen werden kann)
			sinkStack.Push(this, state);

            // Nächste Kanalsenke aufrufen
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
                    case SecureTransactionStage.SendingEncryptedResult: // Verschlüsselte Daten vom Server eingtroffen
						
                        lock(_lockObject)
						{
                            // Wenn die Antwort auch tatsächlich zur aktuellen Sicherheitstransaktion gehört ...
							if (asyncState.SecureTransactionID.Equals(_secureTransactionID)) 
                                // Datenstrom entschlüsseln
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
                    // Wenn die gesendete Transaktionskennung mit der lokalen übereinstimmt ...
					if (_provider == null || asyncState.SecureTransactionID.Equals(_secureTransactionID)) 
                        // Gemeinamen Schlüssel löschen
                        ClearSharedKey();
					
                    // Nachricht weiterverarbeiten
                    ProcessMessage(asyncState.Message, asyncState.Headers, asyncState.Stream,out headers, out stream);
				}
			}
			finally
			{
				// Datenstrom schließen
				asyncState.Stream.Close();
			}
			// Verarbeitung in der nächsten Kanalsenke fortsetzen
			sinkStack.AsyncProcessResponse(headers, stream);
		}

		#endregion
	}
}