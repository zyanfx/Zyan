using System;
using System.IO;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
    /// <summary>
    /// Should be thrown when problems with sending or receiving a message occur.
    /// </summary>
    public class MessageException : IOException
    {
        /// <summary>
        /// Creates a new instance of the MessageException class.
        /// </summary>
        /// <param name="msg">Error message</param>
        /// <param name="innerException">Inner exception (can be null)</param>
        /// <param name="connection">Affected Duplex Channel Connection</param>
        public MessageException(string msg, Exception innerException, Connection connection)
            : base(msg, innerException)
        {
            Connection = connection;
        }

        /// <summary>
        /// Gets or sets the affected Duplex Channel Connection.
        /// </summary>
        public Connection Connection
        {
            get;
            private set;
        }
    }
}
