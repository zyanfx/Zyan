using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Transport
{
    /// <summary>
    /// Interface for Zyan response messages.
    /// </summary>
    public interface IResponseMessage
    {
        /// <summary>
        /// Gets or sets the return value.
        /// </summary>
        object ReturnValue { get; set; }

        /// <summary>
        /// Gets or sets an exception.
        /// </summary>
        Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets a unique ID for call tracking.
        /// </summary>
        Guid TrackingID { get; set; }

        /// <summary>
        /// Gets or sets the address of the caller.
        /// </summary>
        string Address { get; set; }
    }
}
