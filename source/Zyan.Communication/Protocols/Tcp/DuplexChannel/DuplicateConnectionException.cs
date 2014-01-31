using System;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	/// <summary>
	/// This exception should be thrown, when an attempt to create a duplicate connection is detected.
	/// </summary>
	[Serializable]
	internal class DuplicateConnectionException : Exception
	{
		/// <summary>
		/// Creates a new instance of the DuplicateConnectionException class.
		/// </summary>
		/// <param name="channelID">Unique channel identifier</param>
		public DuplicateConnectionException(Guid channelID)
		{
			ChannelID = channelID;
		}

		/// <summary>
		/// Gets the unique channel identifier.
		/// </summary>
		public Guid ChannelID
		{
			get;
			private set;
		}
	}
}
