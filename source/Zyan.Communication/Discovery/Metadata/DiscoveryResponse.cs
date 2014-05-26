using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Zyan.Communication.Discovery.Metadata
{
	/// <summary>
	/// Discovery response metadata packet.
	/// </summary>
	public class DiscoveryResponse : DiscoveryMetadata
	{
		/// <summary>
		/// Packet signature.
		/// </summary>
		public const string SignatureString = "ZyanDiscoveryResponse";

		internal DiscoveryResponse()
			: base(SignatureString)
		{ 
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscoveryResponse"/> class.
		/// </summary>
		/// <param name="hostUrl">The host URL.</param>
		/// <param name="version">The version.</param>
		public DiscoveryResponse(string hostUrl, string version = null)
			: base(SignatureString)
		{
			HostUrl = hostUrl;
			Version = version;
		}

		/// <summary>
		/// Gets or sets the host URL.
		/// </summary>
		public string HostUrl
		{
			get { return Properties["HostUrl"]; }
			set { Properties["HostUrl"] = value; }
		}

		/// <summary>
		/// Gets or sets the server version.
		/// </summary>
		public string Version
		{
			get { return Properties["Version"]; }
			set { Properties["Version"] = value; }
		}

		/// <summary>
		/// Checks if the response packet matches the specified request packet.
		/// </summary>
		/// <param name="dp">The request packet.</param>
		public override bool Matches(DiscoveryMetadata dp)
		{
			var request = dp as DiscoveryRequest;
			if (request == null)
			{
				return false;
			}

			try
			{
				if (!Regex.IsMatch(HostUrl, request.NamePattern))
				{
					return false;
				}
			}
			catch (ArgumentException)
			{
				// invalid regular expression
				return false;
			}

			if (Version == null && request.Version != null)
			{
				return false;
			}

			return request.Version == null || Version == request.Version;
		}
	}
}
