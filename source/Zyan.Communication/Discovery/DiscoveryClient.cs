using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Discovery
{
	/// <summary>
	/// Connects to <see cref="DiscoveryServer"/> to discover available <see cref="ZyanComponentHost"/> instances in local area networks.
	/// </summary>
	public class DiscoveryClient : IDisposable
	{
		/// <summary>
		/// Starts the discovery.
		/// </summary>
		public void StartDiscovery()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Stops the discovery.
		/// </summary>
		public void StopDiscovery()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			StopDiscovery();
		}

		/// <summary>
		/// Occurs when <see cref="DiscoveryServer"/> response is acquired.
		/// </summary>
		public event EventHandler Discovered;

		private void OnDiscovered()
		{
			var discovered = Discovered;
			if (discovered != null)
			{
				discovered(this, EventArgs.Empty);
			}
		}
	}
}
