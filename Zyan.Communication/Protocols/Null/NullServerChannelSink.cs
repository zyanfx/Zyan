using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using Zyan.Communication.Toolbox;
using IDictionary = System.Collections.IDictionary;

namespace Zyan.Communication.Protocols.Null
{
	/// <summary>
	/// Server channel sink for the <see cref="NullChannel"/>.
	/// </summary>
	public class NullServerChannelSink : IServerChannelSink
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NullServerChannelSink"/>
		/// </summary>
		/// <param name="nextSink">Next channel sink in the sink chain.</param>
		public NullServerChannelSink(IServerChannelSink nextSink)
		{
			NextChannelSink = nextSink;
		}

		/// <summary>
		/// Gets or sets the next sink in the sink chain.
		/// </summary>
		public IServerChannelSink NextChannelSink { get; private set; }

		/// <summary>
		/// Gets sink-specific properties.
		/// </summary>
		public IDictionary Properties { get { return null; } }

		/// <summary>
		/// Processes the <see cref="IMessage"/> synchronously.
		/// </summary>
		/// <param name="sinkStack"><see cref="IServerChannelSinkStack"/> for message processing.</param>
		/// <param name="requestMsg">Request <see cref="IMessage"/>.</param>
		/// <param name="requestHeaders">Request <see cref="ITransportHeaders"/>.</param>
		/// <param name="requestStream">Request <see cref="Stream"/>.</param>
		/// <param name="responseMsg">Response <see cref="IMessage"/>.</param>
		/// <param name="responseHeaders">Response <see cref="ITransportHeaders"/>.</param>
		/// <param name="responseStream">Response <see cref="Stream"/>.</param>
		/// <returns><see cref="ServerProcessing"/> enumeration member.</returns>
		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Processes the <see cref="IMessage"/> asynchronously.
		/// </summary>
		/// <param name="sinkStack"><see cref="IServerResponseChannelSinkStack"/> for message processing.</param>
		/// <param name="state">State object.</param>
		/// <param name="msg">Response <see cref="IMessage"/>.</param>
		/// <param name="headers">Response <see cref="ITransportHeaders"/>.</param>
		/// <param name="stream">Response <see cref="Stream"/>.</param>
		public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the response <see cref="Stream"/>.
		/// </summary>
		/// <param name="sinkStack"><see cref="IServerResponseChannelSinkStack"/> for message processing.</param>
		/// <param name="state">State object.</param>
		/// <param name="msg">Response <see cref="IMessage"/>.</param>
		/// <param name="headers">Response <see cref="ITransportHeaders"/>.</param>
		/// <returns>Response <see cref="Stream"/>.</returns>
		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers)
		{
			return null;
		}

		private volatile bool stopped;

		internal bool Stopped
		{ 
			get { return stopped; }
			set { stopped = value; }
		}

		internal void Listen(string channelName)
		{
			Stopped = false;

			while (!Stopped)
			{
				NullMessages.RequestMessage requestMessage;

 				// get next request message from the channel queue
				if (NullMessages.TryGetRequestMessage(channelName, out requestMessage))
				{
					// queue user work item to process the message
					ThreadPool.QueueUserWorkItem(ProcessMessage, requestMessage);
				}
			}
		}

		private void ProcessMessage(object waitCallbackState)
		{
			var requestMessage = (NullMessages.RequestMessage)waitCallbackState;

			// replace full url with object url
			var url = requestMessage.RequestHeaders[CommonTransportKeys.RequestUri].ToString();
			string objectUri;
			NullChannel.ParseUrl(url, out objectUri);
			objectUri = objectUri ?? url;
			requestMessage.RequestHeaders[CommonTransportKeys.RequestUri] = objectUri;
			requestMessage.RequestHeaders["__CustomErrorsEnabled"] = CustomErrorsEnabled.Value;
			requestMessage.Message.Properties["__Uri"] = objectUri;

			IMessage responseMsg;
			ITransportHeaders responseHeaders;
			Stream responseStream;

			// create sink stack to process request message
			var stack = new ServerChannelSinkStack();
			stack.Push(this, null);

			// process request message
			ServerProcessing serverProcessing;
			if (NextChannelSink != null)
			{
				// full processing mode, with deserialization
				serverProcessing = NextChannelSink.ProcessMessage(stack, null,
					requestMessage.RequestHeaders, requestMessage.RequestStream,
					out responseMsg, out responseHeaders, out responseStream);
			}
			else
			{
				// fast processing mode, bypassing deserialization
				serverProcessing = ChannelServices.DispatchMessage(stack, requestMessage.Message, out responseMsg);
				responseHeaders = null;
				responseStream = null;
			}

			// send back the reply
			switch (serverProcessing)
			{
				case ServerProcessing.Complete:
					stack.Pop(this);
					NullMessages.AddResponse(requestMessage, new NullMessages.ResponseMessage
					{
						Message = responseMsg,
						ResponseHeaders = responseHeaders,
						ResponseStream = responseStream
					});
					break;

				case ServerProcessing.Async:
					stack.StoreAndDispatch(NextChannelSink, null);
					break;

				case ServerProcessing.OneWay:
					stack.Pop(this);
					break;
			}
		}

		private Lazy<bool> CustomErrorsEnabled = new Lazy<bool>(() =>
		{
			if (MonoCheck.IsRunningOnMono)
				return false;

			return RemotingConfiguration.CustomErrorsMode == CustomErrorsModes.On;
		});
	}
}
