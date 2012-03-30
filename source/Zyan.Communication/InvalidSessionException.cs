using System;
using System.Runtime.Serialization;

namespace Zyan.Communication
{
	/// <summary>
	/// Implements a Exception to be thrown if a session is invalid.
	/// </summary>
	[Serializable]
	public class InvalidSessionException : Exception, ISerializable
	{
		/// <summary>
		/// Creates a new instance of the InvalidSessionException class.
		/// </summary>
		public InvalidSessionException()
		{
		}

		/// <summary>
        /// Creates a new instance of the InvalidSessionException class.
		/// </summary>
		/// <param name="message">Error message</param>
		public InvalidSessionException(string message)
			: base(message)
		{
		}

		/// <summary>
        /// Creates a new instance of the InvalidSessionException class.
		/// </summary>
        /// <param name="message">Error message</param>
		/// <param name="innerException">Inner exception</param>
		public InvalidSessionException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
        /// Creates a new instance of the InvalidSessionException class.
		/// </summary>
		/// <param name="info">Serialization info</param>
		/// <param name="context">Streaming context for serialization</param>
		protected InvalidSessionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		/// Returns object data for serialization.
		/// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context for serialization</param>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}
	}
}
