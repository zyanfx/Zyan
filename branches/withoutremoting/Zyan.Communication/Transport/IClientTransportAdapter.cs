using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Transport
{
    /// <summary>
    /// Client transport adapter interface.
    /// </summary>
    public interface IClientTransportAdapter
    {
        /// <summary>
        /// Gets the unique name of this transport adapter instance.
        /// </summary>
        string UniqueName { get; }

        /// <summary>
        /// Gets, if the transport adapter is ready for processing requests.
        /// </summary>
        bool Ready { get; }

        /// <summary>
        /// Sends a request and wait for the response.
        /// </summary>
        /// <param name="request">Request message</param>
        /// <returns>Response message</returns>
        IResponseMessage SendRequest(IRequestMessage request);

        /// <summary>
        /// Sends a request and call a method asynchronously on response.
        /// </summary>
        /// <param name="request">Request message</param>
        /// <param name="responseHandler">Handler for response message</param>
        void SendRequestAsync(IRequestMessage request, Action<IResponseMessage> responseHandler);
    }
}
