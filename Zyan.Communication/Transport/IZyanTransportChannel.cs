using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Transport
{
    /// <summary>
    /// Transport channel interface.
    /// </summary>
    public interface IZyanTransportChannel
    {
        /// <summary>
        /// Gets the unique name of this channel instance.
        /// </summary>
        string ChannelName { get; }

        /// <summary>
        /// Gets or sets the dispatcher for this channel instance.
        /// </summary>
        IZyanDispatcher Dispatcher { get; set; }

        /// <summary>
        /// Starts listening for requests.
        /// </summary>
        void StartListening();

        /// <summary>
        /// Stops listening for requests.
        /// </summary>
        void StopListening();

        /// <summary>
		/// Gets a list of all stages of the send pipeline.
		/// </summary>
		List<ISendPipelineStage> SendPipeline { get; }

		/// <summary>
        /// Gets a list of all stages of the receive pipeline.
		/// </summary>
		List<IReceivePipelineStage> ReceivePipeline { get; }

        /// <summary>
        /// Sends a request and wait for the response.
        /// </summary>
        /// <param name="request">Request message</param>
        /// <returns>Response message</returns>
        IZyanResponseMessage SendRequest(IZyanRequestMessage request);

        /// <summary>
        /// Sends a request and call a method asynchronously on response.
        /// </summary>
        /// <param name="request">Request message</param>
        /// <param name="responseHandler">Handler for response message</param>
        void SendRequestAsync(IZyanRequestMessage request, Action<IZyanResponseMessage> responseHandler);
    }
}
