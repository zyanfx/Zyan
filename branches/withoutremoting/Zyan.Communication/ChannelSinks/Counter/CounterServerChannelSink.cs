using System;
using System.IO;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Collections;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.ChannelSinks.Counter
{
	/// <summary>
	/// Serverseitige Kanalsenke f�r gez�hlte Kommunikation.
	/// </summary>
	internal class CounterServerChannelSink : BaseChannelSinkWithProperties, IServerChannelSink
	{
		#region Deklarationen

		// N�chste Kanalsenke in der Senkenkette
		private readonly IServerChannelSink _next = null;

		// Sperr-Objekt (f�r Thread-Synchronisierung)
		private readonly object _lockObject = null;

		#endregion

		#region Konstruktoren

		/// <summary>Erstellt eine neue Instanz von CounterServerChannelSink.</summary>
		/// <param name="nextSink">N�chste Kanalsenke in der Senkenkette</param>
		public CounterServerChannelSink(IServerChannelSink nextSink)
		{
			//Lock objekt erstellen
			_lockObject = new object();

			// N�chste Kanalsenke �bernehmen
			_next = nextSink;
		}
		#endregion

		#region Synchrone Verarbeitung
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

			// Aktuelle Kanalsenke auf den Senkenstapel legen, damit AsyncProcessResponse sp�ter ggf. asynchron aufgerufen werden kann
			sinkStack.Push(this, null);

			//Threadsave
			lock (_lockObject)
			{
				//Verarbeite die Nachricht
				DoProcessMessageBefore(requestMsg, requestHeaders, requestStream);
			}

			// Variable f�r Verarbeitungsstatus
			ServerProcessing processingResult;

			// Nachricht gleich an die n�chste Senke zur Weiterverarbeitung �bergeben
			processingResult = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream, out responseMsg, out responseHeaders, out responseStream);

			//Threadsave
			lock (_lockObject)
			{
				//Verarbeite die Nachricht
				DoProcessMessageAfter(responseMsg, responseHeaders, responseStream, requestHeaders);
			}

			// Aktuelle Senke wieder vom Senkenstapel runternehmen
			sinkStack.Pop(this);

			return processingResult;
		}

		/// <summary>
		/// Threadsafe DoProcess Message Methode, Hier passiert die Verarbeitung vom Request
		/// </summary>
		/// <param name="msg">Original Remotingnachricht</param>
		/// <param name="requestHeaders">Original Anfrage-Header</param>
		/// <param name="requestStream">Original Anfrage-Datenstrom (unverschl�sselt)</param>
		private void DoProcessMessageBefore(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream)
		{
			long length = 0;
			if (requestStream != null)
			{
				try
				{
					length = requestStream.Length;
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

			//Request z�hlen vom Server ausgeben
			Trace.WriteLine(String.Format("Request: - ServerCounterBefore - ClientID - {0} - IP - {1} - Methode - {2} - {3} Bytes", requestHeaders[CommonTransportKeys.ConnectionId], requestHeaders[CommonTransportKeys.IPAddress], myIDictionary != null ? myIDictionary["__MethodName"] : "Unknown", length));
		}

		/// <summary>
		/// Threadsafe DoProcess Message Methode, Hier passiert die Verarbeitung vom Response
		/// </summary>
		/// <param name="msg">Antwort Remotingnachricht</param>
		/// <param name="responseHeaders">Antwort Anfrage-Header</param>
		/// <param name="responseStream">Antwort Anfrage-Datenstrom (unverschl�sselt)</param>
		/// <param name="requestHeaders">Request headers.</param>
		private void DoProcessMessageAfter(IMessage msg, ITransportHeaders responseHeaders, Stream responseStream, ITransportHeaders requestHeaders)
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

			//Request z�hlen vom Server ausgeben
			Trace.WriteLine(String.Format("Response: - ServerCounterAfter - ClientID - {0} - IP - {1} - Methode - {2} - {3} Bytes", requestHeaders[CommonTransportKeys.ConnectionId], requestHeaders[CommonTransportKeys.IPAddress], myIDictionary != null ? myIDictionary["__MethodName"] : "Unknown", length));

			//Wenn es sich um einen Invoke handelt
			if (myIDictionary != null && myIDictionary["__MethodName"].ToString().Equals("Invoke"))
			{
				Trace.WriteLine("{{");
				foreach (object header in requestHeaders)
				{
					DictionaryEntry en = (DictionaryEntry)header;
					if (en.Key.ToString().StartsWith("COUNTER_ARG"))
					{
						Trace.WriteLine("\tArgs: {0}", en.Value);
					}
				}
				Trace.WriteLine("}}");
			}
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
	}
}
