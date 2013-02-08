using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication.Delegates;

namespace Zyan.Communication.Transport
{
    /// <summary>
    /// Interface for Zyan request messages.
    /// </summary>
    public interface IRequestMessage
    {
        /// <summary>
        /// Gets or sets the request type.
        /// </summary>
        RequestType RequestType { get; set; }

        /// <summary>
        /// Gets or sets the address of the remote Zyan server.
        /// </summary>
        string Address { get; set; }

        /// <summary>
        /// Gets or sets the response address.
        /// <remarks>
        /// May be left empty to send response to calling client.
        /// </remarks>
        /// </summary>
        string ResponseAddress { get; set; }

        /// <summary>
        /// Gets or sets the interface name of remote component to be called.
        /// </summary>
        string InterfaceName { get; set; }

        /// <summary>
        /// Gets or sets a unique ID for call tracking.
        /// </summary>
        Guid TrackingID { get; set; }

        /// <summary>
        /// Gets or sets the delegate correlation information.
        /// </summary>
        List<DelegateCorrelationInfo> DelegateCorrelationSet { get; set; }

        /// <summary>
        /// Gets or sets the name of the method to be called.
        /// </summary>
        string MethodName { get; set; }

        /// <summary>
        /// Gets or sets the generic argument types of the method.
        /// </summary>
        Type[] GenericArguments { get; set; }

        /// <summary>
        /// Gets or sets the parameter types.
        /// <remarks>
        /// Must be ordered by occurrance.
        /// </remarks>
        /// </summary>
        Type[] ParameterTypes { get; set; }

        /// <summary>
        /// Gets or sets the parameter values.
        /// <remarks>
        /// Must be ordered by occurrance.
        /// </remarks>
        /// </summary>
        object[] ParameterValues { get; set; }

        /// <summary>
        /// Gets or sets call context data.
        /// </summary>
        LogicalCallContextData CallContext { get; set; }
    }

    /// <summary>
    /// Enumeration of supported request types.
    /// </summary>
    public enum RequestType : byte
    {
        /// <summary>
        /// System operation
        /// </summary>
        SystemOperation=1,        
        /// <summary>
        /// Remote method call
        /// </summary>        
        RemoteMethodCall
    }
}
