using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.ChannelSinks.Counter
{
	/// <summary>
	/// Clientseitige Kanalsenke für Zähler.
	/// </summary>
	internal class CounterClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink
	{
		#region Deklarationen

		// Maximale Anzahl der Verarbeitungsversuche
		private readonly int _maxAttempts;

		// Nächste Kanalsenke
		private readonly IClientChannelSink _next;

		// Sperr-Objekt (für Thread-Synchronisierung)
		private readonly object _lockObject = null;

		// Standard-Ausnahmetext
		private const string DEFAULT_EXCEPTION_TEXT = "Die clientseitige Kanalsenke konnte sich nicht einreihen und eine Verbindung zum Server herstellen.";

		#endregion

		#region Konstruktoren

		/// <summary>Erstellt eine neue Instanz von CounterClientChannelSink</summary>
		/// <param name="nextSink">Nächste Kanalsenke in der Senkenkette</param>
		/// <param name="maxAttempts">Maximale Anzahl der Verarbeitungsversuche</param>
		public CounterClientChannelSink(IClientChannelSink nextSink, int maxAttempts)
		{
			// Werte übernehmen
			_next = nextSink;
			_maxAttempts = maxAttempts;

			// Sperrobjekt erzeugen
			_lockObject = new object();

		}

		#endregion

		#region Synchrone Verarbeitung

		/// <summary>
		/// Verarbeitet die Nachricht.
		/// </summary>
		/// <param name="msg">Original Remotingnachricht</param>
		/// <param name="requestHeaders">Original Anfrage-Header</param>
		/// <param name="requestStream">Original Anfrage-Datenstrom (unverschlüsselt)</param>
		/// <param name="responseHeaders">Antwort-Header</param>
		/// <param name="responseStream">Antwort-Datenstrom (unverschlüsselt nach Verarbeitung!)</param>
		/// <returns>Wahr, wenn Verarbeitung erfolgreich, ansonsten Falsch</returns>
		private bool ProcessQueueMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			lock (_lockObject)
			{

				//Verarbeite die Nachricht
				DoProcessMessageBefore(msg, requestHeaders, requestStream);
			}

			// Nachricht weiterleiten zum Server
			_next.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);


			lock (_lockObject)
			{
				//Verarbeite die Nachricht
				DoProcessMessageAfter(msg, responseHeaders, responseStream);
			}

			// Zurückgeben, ob ein Antwort-Datenstrom empfangen wurde, oder nicht
			return responseStream != null;
		}

		/// <summary>
		/// Threadsafe DoProcess Message Methode, Hier passiert die Verarbeitung des Requests
		/// </summary>
		/// <param name="msg">Original Remotingnachricht</param>
		/// <param name="requestHeaders">Original Anfrage-Header</param>
		/// <param name="requestStream">Original Anfrage-Datenstrom (unverschlüsselt)</param>
		private void DoProcessMessageBefore(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream)
		{
			//Stream länge auf 0 setzen
			long length = 0;

			//Wenn ein Stream verfügbar ist
			if (requestStream != null)
			{
				try
				{
					//Versuche länge zu lesen
					length = requestStream.Length;
				}
				catch
				{
					//Nicht erfolgreich
					length = -1;
				}

			}

			IDictionary myIDictionary = null;
			if (msg != null)
			{
				//Dictionary ermitteln wenn möglich
				myIDictionary = msg.Properties as IDictionary;
			}

			//Request zählen vom Server ausgeben
			Trace.WriteLine("Request: - ClientCounterBefore - Methode - {0} - {1} Bytes", myIDictionary != null ? myIDictionary["__MethodName"] : "Unknown", length);

			//Wenn es sich um einen Invoke handelt
			if (myIDictionary != null && myIDictionary["__MethodName"].ToString().Equals("Invoke"))
			{
				//Argumente ermitteln
				object[] args = myIDictionary["__Args"] as object[];
				if (args != null)
				{
					//Argumente mit Klammer trennen
					Trace.WriteLine("{{");

					//Zähler für Argumente
					int i = 0;
					foreach (object arg in args)
					{
						//Wenn das Arg nicht leer ist
						if (arg != null)
						{
							//Arg als String in den Header Packen.
							requestHeaders[String.Concat("COUNTER_ARG_", i++)] = arg.ToString();
						}
						//Ausgeben
						Trace.WriteLine("\tArgs: {0}", arg);
					}

					//Anzahl Elemente Festhalten
					requestHeaders["COUNTER_TOTAL"] = i.ToString();

					//Ende Klammern
					Trace.WriteLine("}}\n");
				}
			}

		}

		/// <summary>
		/// Threadsafe DoProcess Message Methode, Hier passiert die Verarbeitung der Antwort
		/// </summary>
		/// <param name="msg">Original Remotingnachricht</param>
		/// <param name="responseHeaders">Antwort Anfrage-Header</param>
		/// <param name="responseStream">Antwort Anfrage-Datenstrom (unverschlüsselt)</param>
		private void DoProcessMessageAfter(IMessage msg, ITransportHeaders responseHeaders, Stream responseStream)
		{
			long length = 0;
			if (responseStream != null)
			{
				try
				{
					length = responseStream.Length;
				}
				catch
				{
					length = -1;
				}

			}

			IDictionary myIDictionary = null;
			if (msg != null)
			{
				//Dictionary ermitteln wenn möglich
				myIDictionary = msg.Properties as IDictionary;
			}

			//Response Zählen vom Server ausgeben
			Trace.WriteLine(String.Format("Response: - ClientCounterAfter - Methode - {0} - {1} Bytes", myIDictionary != null ? myIDictionary["__MethodName"] : "Unknown", length));

			//Wenn es sich um einen Invoke handelt
			if (myIDictionary != null && myIDictionary["__MethodName"].ToString().Equals("Invoke"))
			{
				//Argumente ermitteln
				object[] args = myIDictionary["__Args"] as object[];
				if (args != null)
				{
					//Argumente mit Klammer trennen
					Trace.WriteLine("{{");

					//Zähler für Argumente
					int i = 0;
					foreach (object arg in args)
					{
						//Wenn das Arg nicht leer ist
						if (arg != null)
						{
							//Arg als String in den Header Packen.
							responseHeaders[String.Concat("COUNTER_ARG_", i++)] = arg.ToString();
						}
						Trace.WriteLine("\tArgs: {0}", arg);
					}

					//Anzahl Elemente Festhalten
					responseHeaders["COUNTER_TOTAL"] = i.ToString();

					//Ende Klammern
					Trace.WriteLine("}}\n");
				}
			}
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
				for (int i = 0; i < _maxAttempts; i++)
				{
					// Wenn die Nachricht erfolgreich verarbeitet werden konnte ...
					if (ProcessQueueMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream))
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
				throw new CounterRemotingException(DEFAULT_EXCEPTION_TEXT);
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

			/// <summary>Kopiert einen bestimmten Datenstrom</summary>
			/// <param name="stream">Datenstrom</param>
			/// <returns>Kopie des Datenstroms</returns>
			private Stream DuplicateStream(ref Stream stream)
			{
				// Variablen für Speicherdatenströme
				MemoryStream memStream1 = new MemoryStream();
				MemoryStream memStream2 = new MemoryStream();

				// 1 KB Puffer erzeugen
				byte[] buffer = new byte[1024];

				// Anzahl der gelesenen Bytes
				int readBytes;

				// Eingabe-Datenstrom durchlaufen
				while ((readBytes = stream.Read(buffer, 0, buffer.Length)) > 0)
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

			lock (_lockObject)
			{

				// Asynchronen Verarbeitungsstatus erzeugen
				state = new AsyncProcessingState(msg, headers, ref stream, new Guid());

				//Methode aufrufen die Verarbeitung übernimmt
				DoProcessMessageBefore(msg, headers, stream);

			}
			// Aktuelle Senke auf den Senkenstapel legen (Damit ggf. die Verarbeitung der Antwort später asynchron aufgerufen werden kann)
			sinkStack.Push(this, state);

			// Nächste Kanalsenke aufrufen
			_next.AsyncProcessRequest(sinkStack, msg, headers, stream);
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

				lock (_lockObject)
				{
					//Todo: Was ist hier zu tun?
				}

			}
			finally
			{
			}

			ProcessMessage(asyncState.Message, asyncState.Headers, asyncState.Stream, out headers, out stream);

			asyncState.Stream.Close();

			// Verarbeitung in der nächsten Kanalsenke fortsetzen
			sinkStack.AsyncProcessResponse(headers, stream);
		}

		#endregion
	}
}
