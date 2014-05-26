using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Discovery.Metadata
{
	/// <summary>
	/// Base discovery metadata packet class.
	/// </summary>
	public abstract class DiscoveryMetadata
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiscoveryMetadata"/> class.
		/// </summary>
		/// <param name="signature">The signature.</param>
		public DiscoveryMetadata(string signature)
		{
			Signature = signature;
			Properties = new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the signature of the metadata packet.
		/// </summary>
		public string Signature { get; private set; }

		/// <summary>
		/// Gets the properties of the metadata packet.
		/// </summary>
		public Dictionary<string, string> Properties { get; private set; }

		/// <summary>
		/// Checks if the packet matches the specified request.
		/// </summary>
		/// <param name="request">The request.</param>
		public virtual bool Matches(DiscoveryMetadata request)
		{
			return false;
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != GetType())
			{
				return false;
			}

			var dp = obj as DiscoveryMetadata;
			return Signature == dp.Signature &&
				Properties.Count == dp.Properties.Count &&
				Properties.All(p => dp.Properties[p.Key] == p.Value);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			return Properties.Aggregate(Signature.GetHashCode(), (hash, prop) =>
				hash ^ prop.Key.GetHashCode() ^ (prop.Value + string.Empty).GetHashCode());
		}
	}
}
