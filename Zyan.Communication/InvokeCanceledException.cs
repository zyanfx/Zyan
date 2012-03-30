using System;
using System.Runtime.Serialization;

namespace Zyan.Communication
{
	/// <summary>
    /// Implements a Exception to be thrown if remote call was canceled.
	/// </summary>
	[Serializable]
	public class InvokeCanceledException : Exception, ISerializable
	{
		/// <summary>
		/// Creates a new instance of the InvokeCanceledException class.
		/// </summary>
		public InvokeCanceledException()
		{
		}

		/// <summary>
        /// Creates a new instance of the InvokeCanceledException class.
		/// </summary>
		/// <param name="message">Error message</param>
		public InvokeCanceledException(string message)
			: base(message)
		{
		}

		/// <summary>
        /// Creates a new instance of the InvokeCanceledException class.
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="innerException">Inner exception</param>
		public InvokeCanceledException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
        /// Creates a new instance of the InvokeCanceledException class.
		/// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context for serialization</param>
		protected InvokeCanceledException(SerializationInfo info, StreamingContext context)
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
