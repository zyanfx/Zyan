using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Transport
{
    /// <summary>
    /// Server transport adapter interface.
    /// </summary>
    public interface IServerTransportAdapter
    {
        /// <summary>
        /// Gets the unique name of this transport adapter instance.
        /// </summary>
        string UniqueName { get; }

        /// <summary>
        /// Starts listening for requests.
        /// </summary>
        void StartListening();

        /// <summary>
        /// Stops listening for requests.
        /// </summary>
        void StopListening();

        /// <summary>
        /// Gets or sets a delegate which is called, when a request message ist received.
        /// </summary>
        Func<IRequestMessage, IResponseMessage> ReceiveRequest
        {
            get;
            set;
        }
    }
}
