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
	/// Clientseitige Kanalsenke f�r Z�hler.
	/// </summary>
	internal class CounterClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink
	{
		#region Deklarationen

		// Maximale Anzahl der Verarbeitungsversuche
		private readonly int _maxAttempts;

		// N�chste Kanalsenke
		private readonly IClientChannelSink _next;

		// Sperr-Objekt (f�r Thread-Synchronisierung)
		private readonly object _lockObject = null;

		// Standard-Ausnahmetext
		private const string DEFAULT_EXCEPTION_TEXT = "Die clientseitige Kanalsenke konnte sich nicht einreihen und eine Verbindung zum Server herstellen.";

		#endregion

		#region Konstruktoren

		/// <summary>Erstellt eine neue Instanz von CounterClientChannelSink</summary>
		/// <param name="nextSink">N�chste Kanalsenke in der Senkenkette</param>
		/// <param name="maxAttempts">Maximale Anzahl der Verarbeitungsversuche</param>
		public CounterClientChannelSink(IClientChannelSink nextSink, int maxAttempts)
		{
			// Werte �bernehmen
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
		/// <param name="requestStream">Original Anfrage-Datenstrom (unverschl�sselt)</param>
		/// <param name="responseHeaders">Antwort-Header</param>
		/// <param name="responseStream">Antwort-Datenstrom (unverschl�sselt nach Verarbeitung!)</param>
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

			// Zur�ckgeben, ob ein Antwort-Datenstrom empfangen wurde, oder nicht
			return responseStream != null;
		}

		/// <summary>
		/// Threadsafe DoProcess Message Methode, Hier passiert die Verarbeitung des Requests
		/// </summary>
		/// <param name="msg">Original Remotingnachricht</param>
		/// <param name="requestHeaders">Original Anfrage-Header</param>
		/// <param name="requestStream">Original Anfrage-Datenstrom (unverschl�sselt)</param>
		private void DoProcessMessageBefore(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream)
		{
			//Stream l�nge auf 0 setzen
			long length = 0;

			//Wenn ein Stream verf�gbar ist
			if (requestStream != null)
			{
				try
				{
					//Versuche l�nge zu lesen
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
				//Dictionary ermitteln wenn m�glich
				myIDictionary = msg.Properties as IDictionary;
			}

			//Request z�hlen vom Server ausgeben
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

					//Z�hler f�r Argumente
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
		/// <param name="responseStream">Antwort Anfrage-Datenstrom (unverschl�sselt)</param>
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
				//Dictionary ermitteln wenn m�glich
				myIDictionary = msg.Properties as IDictionary;
			}

			//Response Z�hlen vom Server ausgeben
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

					//Z�hler f�r Argumente
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

				// Ggf. mehrere Male versuchen (h�chstens bis maximal konfigurierte Anzahl)
				for (int i = 0; i < _maxAttempts; i++)
				{
					// Wenn die Nachricht erfolgreich verarbeitet werden konnte ...
					if (ProcessQueueMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream))
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
				throw new CounterRemotingException(DEFAULT_EXCEPTION_TEXT);
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

			/// <summary>Kopiert einen bestimmten Datenstrom</summary>
			/// <param name="stream">Datenstrom</param>
			/// <returns>Kopie des Datenstroms</returns>
			private Stream DuplicateStream(ref Stream stream)
			{
				// Variablen f�r Speicherdatenstr�me
				MemoryStream memStream1 = new MemoryStream();
				MemoryStream memStream2 = new MemoryStream();

				// 1 KB Puffer erzeugen
				byte[] buffer = new byte[1024];

				// Anzahl der gelesenen Bytes
				int readBytes;

				// Eingabe-Datenstrom durchlaufen
				while ((readBytes = stream.Read(buffer, 0, buffer.Length)) > 0)
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

			lock (_lockObject)
			{

				// Asynchronen Verarbeitungsstatus erzeugen
				state = new AsyncProcessingState(msg, headers, ref stream, new Guid());

				//Methode aufrufen die Verarbeitung �bernimmt
				DoProcessMessageBefore(msg, headers, stream);

			}
			// Aktuelle Senke auf den Senkenstapel legen (Damit ggf. die Verarbeitung der Antwort sp�ter asynchron aufgerufen werden kann)
			sinkStack.Push(this, state);

			// N�chste Kanalsenke aufrufen
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

			// Verarbeitung in der n�chsten Kanalsenke fortsetzen
			sinkStack.AsyncProcessResponse(headers, stream);
		}

		#endregion
	}
}
