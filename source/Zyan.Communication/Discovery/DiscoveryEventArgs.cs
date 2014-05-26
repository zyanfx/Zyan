using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication.Discovery.Metadata;

namespace Zyan.Communication.Discovery
{
	/// <summary>
	/// Discovery event arguments.
	/// </summary>
	[Serializable]
	public class DiscoveryEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiscoveryEventArgs"/> class.
		/// </summary>
		/// <param name="metadata">Discovery metadata.</param>
		public DiscoveryEventArgs(DiscoveryMetadata metadata)
		{
			Metadata = metadata;
		}

		/// <summary>
		/// Gets the discovery metadata.
		/// </summary>
		public DiscoveryMetadata Metadata { get; private set; }
	}
}
