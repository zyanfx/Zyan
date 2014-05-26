using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Discovery
{
	/// <summary>
	/// Enables automatic <see cref="ZyanComponentHost"/> discovery in local area networks. Requires <see cref="DiscoveryClient"/>.
	/// </summary>
	public class DiscoveryServer : IDisposable
	{
		/// <summary>
		/// Starts listening for the incoming connections.
		/// </summary>
		public void StartListening()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Stops the listening.
		/// </summary>
		public void StopListening()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			StopListening();
		}
	}
}
