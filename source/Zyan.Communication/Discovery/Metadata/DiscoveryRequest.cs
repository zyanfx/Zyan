using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Discovery.Metadata
{
	/// <summary>
	/// Discovery request metadata packet.
	/// </summary>
	public class DiscoveryRequest : DiscoveryMetadata
	{
		/// <summary>
		/// Packet signature.
		/// </summary>
		public const string SignatureString = "ZyanDiscoveryRequest";

		internal DiscoveryRequest()
			: base(SignatureString)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscoveryRequest"/> class.
		/// </summary>
		/// <param name="namePattern"><see cref="ZyanComponentHost"/> name pattern (regular expression).</param>
		/// <param name="version">The version.</param>
		public DiscoveryRequest(string namePattern, string version = null)
			: base(SignatureString)
		{
			NamePattern = namePattern;
			Version = version;
		}

		/// <summary>
		/// Gets or sets the <see cref="ZyanComponentHost"/> name pattern.
		/// </summary>
		public string NamePattern
		{
			get { return Properties["NamePattern"]; }
			set { Properties["NamePattern"] = value; }
		}

		/// <summary>
		/// Gets or sets the required server version.
		/// </summary>
		public string Version
		{
			get { return Properties["Version"]; }
			set { Properties["Version"] = value; }
		}
	}
}
