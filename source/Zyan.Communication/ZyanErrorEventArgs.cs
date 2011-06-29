using System;
using System.Runtime.Remoting.Messaging;

namespace Zyan.Communication
{
	/// <summary>
	/// Possible actions for cought error.
	/// </summary>
	public enum ZyanErrorAction : short
	{
		/// <summary>
		/// Throws the exception.
		/// </summary>
		ThrowException = 0,
		/// <summary>
		/// Retry the request.
		/// </summary>        
		Retry,
		/// <summary>
		/// Ignore the error.
		/// </summary>
		Ignore
	}

	/// <summary>
	/// Provides specific data for ZyanError-Events.
	/// </summary>
	[Serializable]
	public class ZyanErrorEventArgs : EventArgs
	{
		/// <summary>
		/// Creates a new instance of the ZyanErrorEventArgs class.
		/// </summary>
		public ZyanErrorEventArgs()
		{
			// Throw exceptions by default
			Action = ZyanErrorAction.ThrowException;
		}

		/// <summary>
		/// Gets the exception that occured.
		/// </summary>
		public Exception Exception
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets or sets, the action to handle this error.
		/// </summary>
		public ZyanErrorAction Action
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the remoting message of the request.
		/// </summary>
		public IMethodCallMessage RemotingMessage
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets the type of the called server component.
		/// </summary>
		public Type ServerComponentType
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets the name of the remote called member.
		/// </summary>
		public string RemoteMemberName
		{
			get;
			internal set;
		}
	}
}
