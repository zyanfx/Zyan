using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols.Null
{
	/// <summary>
	/// Transport layer for the <see cref="NullChannel"/>.
	/// </summary>
	internal static class NullMessages
	{
		static NullMessages()
		{
			RequestEvents = new ConcurrentDictionary<string, AutoResetEvent>();
			Requests = new ConcurrentDictionary<string, ConcurrentQueue<RequestMessage>>();
			Responses = new ConcurrentDictionary<int, ResponseMessage>();
		}

		// request waiting timeout, milliseconds
		private const int RequestWaitTimeout = 10;

		// ChannelName -> AutoResetEvent
		private static ConcurrentDictionary<string, AutoResetEvent> RequestEvents;

		// ChannelName -> Request queue
		private static ConcurrentDictionary<string, ConcurrentQueue<RequestMessage>> Requests;

		// RequestMessage.Identity -> ResponseMessage
		private static ConcurrentDictionary<int, ResponseMessage> Responses;

		/// <summary>
		/// Tries to get the next request message for the specified channel.
		/// </summary>
		/// <param name="channelName">Channel name.</param>
		/// <param name="requestMessage">Next <see cref="RequestMessage"/> for the specified channel.</param>
		/// <returns>True if successful, otherwise, false.</returns>
		public static bool TryGetRequestMessage(string channelName, out RequestMessage requestMessage)
		{
			var resetEvent = RequestEvents.GetOrAdd(channelName, s => new AutoResetEvent(false));
			var queue = Requests.GetOrAdd(channelName, s => new ConcurrentQueue<RequestMessage>());

			// wait for incoming request
			resetEvent.WaitOne(RequestWaitTimeout);
			return queue.TryDequeue(out requestMessage);
		}

		/// <summary>
		/// Adds the request message for the specified channel and waits for the response message.
		/// </summary>
		/// <param name="channelName">Channel name.</param>
		/// <param name="requestMessage">Source <see cref="RequestMessage"/>.</param>
		/// <returns><see cref="ResponseMessage"/> with the reply.</returns>
		public static ResponseMessage ProcessRequest(string channelName, RequestMessage requestMessage)
		{
			// enqueue the request message
			var queue = Requests.GetOrAdd(channelName, s => new ConcurrentQueue<RequestMessage>());
			queue.Enqueue(requestMessage);

			// signal
			var resetEvent = RequestEvents.GetOrAdd(channelName, s => new AutoResetEvent(false));
			resetEvent.Set();

			// wait for the response
			requestMessage.ResetEvent.WaitOne();

			// return response message
			ResponseMessage responseMessage;
			Responses.TryRemove(requestMessage.Identity, out responseMessage);
			return responseMessage;
		}

		/// <summary>
		/// Adds the response to the specified request message.
		/// </summary>
		/// <param name="requestMessage">Request message.</param>
		/// <param name="responseMessage">Response message.</param>
		public static void AddResponse(RequestMessage requestMessage, ResponseMessage responseMessage)
		{
			Responses.TryAdd(requestMessage.Identity, responseMessage);
			requestMessage.ResetEvent.Set();
		}

		/// <summary>
		/// Request message for the <see cref="NullChannel"/>.
		/// </summary>
		public class RequestMessage
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="RequestMessage"/> class.
			/// </summary>
			public RequestMessage()
			{
				Identity = Interlocked.Increment(ref counter);
				ResetEvent = new ManualResetEvent(false);
			}

			private static int counter;

			/// <summary>
			/// Gets or sets message identity.
			/// </summary>
			public int Identity { get; set; }

			/// <summary>
			/// Gets or sets request <see cref="IMessage"/>.
			/// </summary>
			/// <remarks>Used in fast processing mode (serialization bypassed).</remarks>
			public IMessage Message { get; set; }
 
			/// <summary>
			/// Gets or sets message headers.
			/// </summary>
			/// <remarks>Used in full processing mode (serialization enabled).</remarks>
			public ITransportHeaders RequestHeaders { get; set; }

			/// <summary>
			/// Gets or sets request <see cref="Stream"/>.
			/// </summary>
			/// <remarks>Used in full processing mode (serialization enabled).</remarks>
			public Stream RequestStream { get; set; }

			/// <summary>
			/// Gets or sets the <see cref="ManualResetEvent"/> for synchronization.
			/// </summary>
			public ManualResetEvent ResetEvent { get; set; }
		}

		/// <summary>
		/// Response message for the <see cref="NullChannel"/>.
		/// </summary>
		public class ResponseMessage
		{
			/// <summary>
			/// Gets or sets response <see cref="IMessage"/>.
			/// </summary>
			/// <remarks>Used in fast processing mode (serialization bypassed).</remarks>
			public IMessage Message { get; set; }

			/// <summary>
			/// Gets or sets the <see cref="ITransportHeaders"/> for the response message.
			/// </summary>
			/// <remarks>Used in full processing mode (serialization enabled).</remarks>
			public ITransportHeaders ResponseHeaders { get; set; }

			/// <summary>
			/// Gets or sets the response <see cref="Stream"/>.
			/// </summary>
			/// <remarks>Used in full processing mode (serialization enabled).</remarks>
			public Stream ResponseStream { get; set; }
		}
	}
}
