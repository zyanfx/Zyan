using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Interface for connection-based remoting channels.
	/// </summary>
	public interface IConnectionNotification
	{
		/// <summary>
		/// Occurs when connection is established or restored.
		/// </summary>
		event EventHandler ConnectionEstablished;
	}
}
