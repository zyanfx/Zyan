using System;
using System.IO;
using System.Net;
using System.Timers;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace Cyan.Communication.ChannelSinks.Encryption
{
    /// <summary>
    /// Serverseitige Kanalsenke f�r verschl�sselte Kommunikation.
    /// <remarks>
    /// Ben�tigt auf der Clientseite CryptoClientChannelSink als Gegenst�ck!
    /// </remarks>
    /// </summary>
	internal class CryptoServerChannelSink : BaseChannelSinkWithProperties, IServerChannelSink
	{
		#region Deklarationen

        // Name des symmetrischen Verschl�sselungsalgorithmus
        private readonly string _algorithm;

        // Schalter f�r OAEP-Padding
        private readonly bool _oaep;
		        
        // Minimale Lebensdauer einer Cient-Verbindung in Sekunden
		private readonly double _connectionAgeLimit;
		
        // Intervall des Aufr�umvorgangs in Sekunden
		private readonly double _sweepFrequency;

        // Zeitgeber f�r den Aufr�umvorgang
        private System.Timers.Timer _sweepTimer = null;

        // Gibt an, ob clientseitig auch entsprechende Verschl�sselungs-Kanalsenken vorhanden sein m�ssen
		private readonly bool _requireCryptoClient;

        // Client-IP Ausnahmeliste
		private IPAddress [] _securityExemptionList;
		
        // Auflistung der aktiven Client-Verbindungen
		private readonly Hashtable _connections = null;
		
        // N�chste Kanalsenke in der Senkenkette
		private readonly IServerChannelSink _next = null;
		
        #endregion

		#region Konstruktoren

		/// <summary>Erstellt eine neue Instanz von CryptoServerChannelSink.</summary>
		/// <param name="nextSink">N�chste Kanalsenke in der Senkenkette</param>
        /// <param name="algorithm">Name des symmetrischen Verschl�sselungsalgorithmus</param>
        /// <param name="oaep">Gibt an, ob OAEP-Padding verwendet werden soll, oder nicht</param>
		/// <param name="connectionAgeLimit">Lebenszeit einer Client-Verbindung in Sekunden</param>
		/// <param name="sweeperFrequency">Intervall des Aufr�umvorgangs in Sekunden</param>
		/// <param name="requireCryptoClient">Gibt an, ob clientseitig eine Kanalsenke f�r verschl�sselte Kommunikation vorhanden sein muss</param>
		/// <param name="securityExemptionList">IP-Adressen Ausnahmeliste</param>
		public CryptoServerChannelSink(IServerChannelSink nextSink, string algorithm, bool oaep, double connectionAgeLimit, double sweeperFrequency, bool requireCryptoClient, IPAddress [] securityExemptionList)
		{
			// Werte �bernehmen
			_algorithm = algorithm;
			_oaep = oaep;
			_connectionAgeLimit = connectionAgeLimit;
			_sweepFrequency = sweeperFrequency;
			_requireCryptoClient = requireCryptoClient;
			_securityExemptionList = securityExemptionList;

			// N�chste Kanalsenke �bernehmen
			_next = nextSink;

			// Verbindungs-Auflistung erzeugen
			_connections = new Hashtable(103, 0.5F);
			
            // Aufr�umvorgang einrichten
            StartConnectionSweeper();
		}
		#endregion

		#region Synchrone Verarbeitung

		/// <summary>
        /// Erzeugt den gemeinsamen Schl�ssel und bereitet dessen �bertragung zum Client vor.
        /// </summary>
		/// <param name="secureTransactionID">Sicherheitstransaktionskennung</param>
		/// <param name="requestHeaders">Anfrage-Header vom Client</param>
		/// <param name="responseMsg">Antwortnachricht</param>
		/// <param name="responseHeaders">Antwort-Header</param>
		/// <param name="responseStream">Antwort-Datenstrom</param>
		/// <returns>Status</returns>		
		private ServerProcessing MakeSharedKey(Guid transactID, ITransportHeaders requestHeaders, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
        {
			// Gemeinsamer Schl�ssel und Inizialisierungsvektor
			SymmetricAlgorithm symmetricProvider = CryptoTools.CreateSymmetricCryptoProvider(_algorithm);

            // Clientverbindungsdaten erzeugen
            ClientConnectionData connectionData = new ClientConnectionData(transactID, symmetricProvider);
            
            lock (_connections.SyncRoot)
            {
                // Clientverbindungsdaten unter der angegebenen Sicherheitstransaktionskennung speichern
                _connections[transactID.ToString()] = connectionData;
            }
            // RSA Kryptografieanbieter erzeugen
            RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider();

            // �ffentlichen Schl�ssel (vom Client) aus den Anfrage-Headern lesen
            string publicKey = (string)requestHeaders[CommonHeaderNames.PUBLIC_KEY];

            // Wenn kein �ffentlicher Schl�ssel gefunden wurde ...
            if (string.IsNullOrEmpty(publicKey)) 
                throw new CryptoRemotingException("Kein �ffentlicher Schl�ssel gefunden.");
            		
			// �ffentlichen Schl�ssel in den Kryptografieanbieter laden
			rsaProvider.FromXmlString(publicKey);

            // Gemeinsamen Schl�ssel und dessen Inizialisierungsfaktor verschl�sseln
			byte [] encryptedKey = rsaProvider.Encrypt(symmetricProvider.Key, _oaep);
			byte [] encryptedIV = rsaProvider.Encrypt(symmetricProvider.IV, _oaep);

			// Antwort-Header zusammenstellen
			responseHeaders = new TransportHeaders();
			responseHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingSharedKey).ToString();
			responseHeaders[CommonHeaderNames.SHARED_KEY] = Convert.ToBase64String(encryptedKey);
			responseHeaders[CommonHeaderNames.SHARED_IV] = Convert.ToBase64String(encryptedIV);

			// Es wird keine Antwortnachricht ben�tigt
			responseMsg = null;
			responseStream = new MemoryStream();
			
			// Vollst�ndige Verarbeitung zur�ckmelden
			return ServerProcessing.Complete;
		}

		/// <summary>
        /// Entschl�sselt die eingehende Nachricht vom Client.
        /// </summary>
		/// <param name="secureTransactionID">Sicherheitstransaktionskennung</param>
		/// <param name="sinkStack">Senkenstapel</param>
		/// <param name="requestMsg">Anfrage-Nachricht vom Client</param>
		/// <param name="requestHeaders">Anfrage-Header vom Cient</param>
		/// <param name="requestStream">Anfrage-Datenstrom</param>
		/// <param name="responseMsg">Antwort-Nachricht</param>
		/// <param name="responseHeaders">Antwort-Header</param>
		/// <param name="responseStream">Antwort-Datenstrom</param>
		/// <returns>Verarbeitungsstatus</returns>
		public ServerProcessing ProcessEncryptedMessage(Guid transactID, IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
        {
            // Variable f�r Client-Verbindungsinformationen
            ClientConnectionData connectionData;
						
			lock(_connections.SyncRoot)
			{
                // Client-Verbindungsdaten �ber die angegebene Sicherheitstransaktionskennung abrufen
				connectionData = (ClientConnectionData)_connections[transactID.ToString()];
			}
            // Wenn keine Verbindungsdaten zu dieser Sicherheitstransaktionskennung gefunden wurden ...
			if (connectionData == null) 
                // Ausnahme werfen
                throw new CryptoRemotingException("Keine passenden Client-Verbindungsinformationen gefunden.");
			
            // Zeitstempel aktualisieren
            connectionData.UpdateTimestamp();

			// Datenstrom entschl�sseln
			Stream decryptedStream = CryptoTools.GetDecryptedStream(requestStream, connectionData.CryptoProvider);
			
            // Verschl�sselten-Quelldatenstrom schlie�en
            requestStream.Close(); 

			// Entschl�sselte Nachricht zur Weiterverarbeitung an die n�chste Kanalsenke weitergeben
			ServerProcessing processingResult = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders, decryptedStream, out responseMsg, out responseHeaders, out responseStream);

			// Status der Sicherheitstransaktion auf "verschl�sselte Atwortnachricht senden" einstellen
			responseHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)SecureTransactionStage.SendingEncryptedResult).ToString();
			
            // Antwortnachricht verschl�sseln
            Stream encryptedStream = CryptoTools.GetEncryptedStream(responseStream, connectionData.CryptoProvider);
			
            // Unverschl�sselten Quell-Datenstrom schlie�en
            responseStream.Close(); // close the plaintext stream now that we're done with it
			
            // Verschl�sselten Datenstrom als Antwort-Datenstrom verwenden
            responseStream = encryptedStream;

            // Verarbeitungsstatus zur�ckgeben
			return processingResult;
		}

		/// <summary>
        /// Pr�ft, ob eine bestimmte Sicherheitstransaktionskennung bereits bekannt ist.
        /// </summary>
		/// <param name="secureTransactionID">Sicherheitstransaktionskennung</param>
		/// <returns>Wahr, wenn die Sicherheitstransaktion bekannt ist, ansonsten Falsch</returns>
		private bool IsExistingSecurityTransaction(Guid transactID)
		{
			lock(_connections.SyncRoot)
			{
				return (!transactID.Equals(Guid.Empty) && _connections[transactID.ToString()] != null);
			}
		}

		/// <summary>
        /// Erzeugt eine leere Antwortnachricht.
        /// </summary>
        /// <param name="transactionStage">Art des aktuellen Transaktionsschritts</param>
		/// <param name="responseMsg">Antwort-Nachricht</param>
		/// <param name="responseHeaders">Antwort-Header</param>
		/// <param name="responseStream">Antwort-Datenstrom</param>
		/// <returns>Verarbeitungsstatus</returns>
		private ServerProcessing SendEmptyToClient(SecureTransactionStage transactionStage, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// Inizialisieren
			responseMsg = null;
			responseStream = new MemoryStream();
			responseHeaders = new TransportHeaders();
			
            // Aktuellen Transaktionsschritt als Antwort-Header schreiben
            responseHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE] = ((int)transactionStage).ToString();
			
            // Volst�ndige Verarbeitung zur�ckmelden
            return ServerProcessing.Complete;
		}

        /// <summary>
        /// Verarbeitet eine einzele Clientanfrage
        /// </summary>
        /// <param name="sinkStack">Aufrufstapel der Kanalsenken</param>
        /// <param name="requestMsg">Anfrage-nachricht</param>
        /// <param name="requestHeaders">Anfrage-Header</param>
        /// <param name="requestStream">Anfrage-Datenstrom</param>
        /// <param name="responseMsg">Antwort-Nachricht</param>
        /// <param name="responseHeaders">Antwort-Header</param>
        /// <param name="responseStream">Antwort-Datenstrom</param>
        /// <returns>Status serverseitigen Verarbeitung der Nachricht insgesamt</returns>
		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			// Sicherheitstransaktionskennung aus Anfrage-Header lesen
			string strTransactID = (string)requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_ID];
			
            // In Guid umwandeln
            Guid transactID = (strTransactID == null ? Guid.Empty : new Guid(strTransactID));
			
            // Aktuellen Transaktionsschritt aus Anfrage-Header lesen
            SecureTransactionStage transactionStage = (SecureTransactionStage)Convert.ToInt32((string)requestHeaders[CommonHeaderNames.SECURE_TRANSACTION_STATE]);

			// IP-Adresse des Clients aus Anfrage-Header lesen
			IPAddress clientAddress = requestHeaders[CommonTransportKeys.IPAddress] as IPAddress;

            // Aktuelle Kanalsenke auf den Senkenstapel legen, damit AsyncProcessResponse sp�ter ggf. asynchron aufgerufen werden kann
			sinkStack.Push(this, null);

            // Variable f�r Verarbeitungsstatus
            ServerProcessing processingResult;

			// Aktuellen Transaktionsschritt auswerten
			switch(transactionStage)
			{
                case SecureTransactionStage.SendingPublicKey: // Client sendet den �ffentlichen Schl�ssel an den Server

					// Gemeinsamen Schl�ssel erzeugen und mit dem �ffentlichen Schl�ssel des Clients verschl�sseln
                    processingResult = MakeSharedKey(transactID, requestHeaders, out responseMsg, out responseHeaders, out responseStream);
					
					break;

                case SecureTransactionStage.SendingEncryptedMessage: // Client sendet die verschl�sselte Anfragenachricht an den Server
             
                    // Wenn die Sicherheitstransaktionskennung des Clients bekannt ist ...
                    if (IsExistingSecurityTransaction(transactID)) 
					    // Verschl�sselte Nachricht verarbeiten
						processingResult = ProcessEncryptedMessage(transactID, sinkStack, requestMsg, requestHeaders, requestStream, out responseMsg, out responseHeaders, out responseStream);
					else 					
                        // Leere Nachricht an den Client senden und Transaktionsschritt auf "Unbekannte Sicherheitstransaktionskennung". setzen.
						processingResult = SendEmptyToClient(SecureTransactionStage.UnknownTransactionID, out responseMsg, out responseHeaders, out responseStream);
					
					break;

                case SecureTransactionStage.Uninitialized: // Uninizialisiert, noch nichts geschehen

					// Wenn f�r diesen Client Verschl�sselung nicht zwingend notwendig ist ...
					if (!RequireEncryption(clientAddress))
					    // Nachricht gleich an die n�chste Senke zur Weiterverarbeitung �bergeben
						processingResult = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream,out responseMsg, out responseHeaders, out responseStream);
					else 
                        // Ausnahme werfen
                        throw new CryptoRemotingException("Der Server ben�tigt eine verschl�ssekte Verbindung f�r diesen Client.");
					
                    break;

				default:
					
                    // Ausnahme werfen
					throw new CryptoRemotingException("Ung�ltige Anfrage vom Client: " + transactionStage + ".");
			}
			// Aktuelle Senke wieder vom Senkenstapel runternehmen
			sinkStack.Pop(this);

            // Veratbeitungsstatus zur�ckgeben
			return processingResult;
		}

		/// <summary>
        /// Pr�ft, ob f�r die Kommunikation mit einem bestimmten Client zwingend Verschl�sselung erforderlich ist, oder nicht.
        /// </summary>
		/// <param name="clientAddress">IP-Adresse des Clients</param>
		/// <returns>Wahr, wenn Verschl�sselung erforderlich ist, ansonsten Falsch</returns>
		private bool RequireEncryption(IPAddress clientAddress)
		{
		    // Wenn keine Ausnahmen definiert sind ...
			if (clientAddress == null || _securityExemptionList == null || _securityExemptionList.Length == 0) 
			    // Standardvorgabe zur�ckgeben
				return _requireCryptoClient;
			
			// Gefunden-Schalter
			bool found = false;

            // Alle IP-Adrssen der Ausnahmeliste durchgehen
			foreach(IPAddress address in _securityExemptionList)
			{
                // Wenn die aktuelle Adresse gefunden wurde ...
				if (clientAddress.Equals(address))
				{
                    // Gefunden-Schalter setzen
					found = true;
					
                    // Schleife abbrechen
                    break;
				}
			}
            // Pr�fergebnis bestimmen und zur�ckgeben
			return found ? !_requireCryptoClient : _requireCryptoClient;
		}

        /// <summary>
        /// Gibt die n�chste Kanalsenke in der Aufrufkette zur�ck.
        /// </summary>
        public IServerChannelSink NextChannelSink
        {
            get { return _next; }
        }

        /// <summary>
        /// Gibt den Antwort-Datenstrom zur�ck.
        /// </summary>
        /// <param name="sinkStack">Senkenstapel</param>
        /// <param name="state">Optionale Statusinformationen</param>
        /// <param name="msg">Remoting-Nachricht</param>
        /// <param name="headers">Header-Informationen</param>
        /// <returns>Antwort-Datenstrom</returns>
        public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers)
        {
            // Immer null zur�ckgeben
            return null;
        }

		#endregion

		#region Asynchrone Verarbeitung

        /// <summary>
        /// Fordert die Verarbeitung der Antwortnachricht von dieser Senke an, wenn die Anfragenachricht asynchron verarbeitet wurde.
        /// </summary>
        /// <param name="sinkStack">Senkenstapel</param>
        /// <param name="state">Zustand</param>
        /// <param name="msg">antwort-Nachricht</param>
        /// <param name="headers">Antwort-Header</param>
        /// <param name="stream">Antwort-Datenstrom</param>
        public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers, Stream stream)
        {
            // Antwortnachtenverarbeitung der verbleibenden Senken im Stapel aufrufen
            sinkStack.AsyncProcessResponse(msg, headers, stream);
        }

		#endregion

		#region Aufr�umvorgang f�r Verbindungsinformationen

		/// <summary>
        /// Startet den Zeitgeber f�r den Aufr�umvorgang.
        /// </summary>
		private void StartConnectionSweeper()
		{
			// Wenn noch kein Zeitgeber eingerichtet wurde ...
			if (_sweepTimer == null) 
			{
                // Neuen Zeitgeber erzeugen und Intervall laut Konfiguration einstellen
				_sweepTimer = new System.Timers.Timer(_sweepFrequency*1000);
				
                // Ereignis bei Zeitgeberintervall abonnieren
                _sweepTimer.Elapsed += new ElapsedEventHandler(SweepConnections);
				
                // Zeitgeber starten
                _sweepTimer.Start();
			}
		}

		/// <summary>
        /// R�umt abgelaufende Client-Verbindungen weg.
        /// </summary>
		private void SweepConnections(object sender, ElapsedEventArgs e)
		{			
			lock (_connections.SyncRoot) 
			{
                // Liste f�r L�schungen erzeugen
				ArrayList toDelete = new ArrayList(_connections.Count);

				// Alle Verbindungen durchlaufen
				foreach(DictionaryEntry entry in _connections) 
				{
                    // Daten der aktuell durchlaufenen Verbindung abrufen
					ClientConnectionData connectionData = (ClientConnectionData)entry.Value;

                    // Wenn die Verbindung bereits das Zeitlimit �berschritten hat (abgelaufen ist) ...
					if (connectionData.Timestamp.AddSeconds(_connectionAgeLimit).CompareTo(DateTime.UtcNow) < 0) 
					{
                        // Sicherheitstransaktionskennung der Verbindung zur L�schliste zuf�gen
						toDelete.Add(entry.Key);

                        // Verbindung entsorgen
						((IDisposable)connectionData).Dispose();
					}
				}
				// L�schliste durchlaufen
                foreach (Object obj in toDelete)
                {
                    // Verbindung l�schen
                    _connections.Remove(obj);
                }
			}
		}

		#endregion
	}
}
