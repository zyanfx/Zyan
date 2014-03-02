using System;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
	/// Exception that is thrown when encryption or decryption of the remoting communication goes wrong.
	/// </summary>
	[Serializable]
	public class CryptoRemotingException : RemotingException, ISerializable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoRemotingException"/> class.
		/// </summary>
		public CryptoRemotingException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoRemotingException"/> class.
		/// </summary>
		/// <param name="message">The error message that explains why the exception occurred.</param>
		public CryptoRemotingException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoRemotingException"/> class.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception. 
		/// If the <paramref name="innerException" /> parameter is not a null reference (Nothing in Visual Basic), 
		/// the current exception is raised in a catch block that handles the inner exception.</param>
		public CryptoRemotingException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoRemotingException"/> class.
		/// </summary>
		/// <param name="info">The object that holds the serialized object data.</param>
		/// <param name="context">The contextual information about the source or destination of the exception.</param>
		protected CryptoRemotingException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		/// Sets the <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}
	}
}
