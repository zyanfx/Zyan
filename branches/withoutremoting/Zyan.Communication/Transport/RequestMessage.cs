using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication.Delegates;

namespace Zyan.Communication.Transport
{
    /// <summary>
    /// Zyan request messages.
    /// </summary>
    [Serializable]
    public class RequestMessage : IRequestMessage
    {
        /// <summary>
        /// Gets or sets the request type.
        /// </summary>
        public RequestType RequestType { get; set; }

        /// <summary>
        /// Gets or sets the address of the remote Zyan server.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the response address.
        /// <remarks>
        /// May be left empty to send response to calling client.
        /// </remarks>
        /// </summary>
        public string ResponseAddress { get; set; }

        /// <summary>
        /// Gets or sets the interface name of remote component to be called.
        /// </summary>
        public string InterfaceName { get; set; }

        /// <summary>
        /// Gets or sets a unique ID for call tracking.
        /// </summary>
        public Guid TrackingID { get; set; }

        /// <summary>
        /// Gets or sets the delegate correlation information.
        /// </summary>
        public List<DelegateCorrelationInfo> DelegateCorrelationSet { get; set; }

        /// <summary>
        /// Gets or sets the name of the method to be called.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Gets or sets the generic argument types of the method.
        /// </summary>
        public Type[] GenericArguments { get; set; }

        /// <summary>
        /// Gets or sets the parameter types.
        /// <remarks>
        /// Must be ordered by occurrance.
        /// </remarks>
        /// </summary>
        public Type[] ParameterTypes { get; set; }

        /// <summary>
        /// Gets or sets the parameter values.
        /// <remarks>
        /// Must be ordered by occurrance.
        /// </remarks>
        /// </summary>
        public object[] ParameterValues { get; set; }

        /// <summary>
        /// Gets or sets call context data.
        /// </summary>
        public LogicalCallContextData CallContext { get; set; }
    }
}
