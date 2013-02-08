using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Transport
{
    /// <summary>
    /// Zyan response message.
    /// </summary>
    [Serializable]
    public class ResponseMessage : IResponseMessage
    {
        /// <summary>
        /// Gets or sets the return value.
        /// </summary>
        public object ReturnValue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an exception.
        /// </summary>
        public Exception Exception
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a unique ID for call tracking.
        /// </summary>
        public Guid TrackingID { get; set; }

        /// <summary>
        /// Gets or sets the address of the caller.
        /// </summary>
        public string Address { get; set; }
    }
}
