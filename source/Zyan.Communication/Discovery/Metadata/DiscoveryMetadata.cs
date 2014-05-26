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
		/// Initializes the <see cref="DiscoveryMetadata"/> class.
		/// </summary>
		static DiscoveryMetadata()
		{
			// register standard discovery types
			DiscoveryMetadataFactories = new Dictionary<string, Func<DiscoveryMetadata>>();
			RegisterDiscoveryMetadataFactory(DiscoveryRequest.SignatureString, () => new DiscoveryRequest());
			RegisterDiscoveryMetadataFactory(DiscoveryResponse.SignatureString, () => new DiscoveryResponse());
		}

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

		private static Dictionary<string, Func<DiscoveryMetadata>> DiscoveryMetadataFactories { get; set; }

		/// <summary>
		/// Registers the discovery metadata factory to create metadata packets given their signatures.
		/// </summary>
		/// <param name="signature">The signature.</param>
		/// <param name="factory">The factory.</param>
		public static void RegisterDiscoveryMetadataFactory(string signature, Func<DiscoveryMetadata> factory)
		{
			DiscoveryMetadataFactories[signature] = factory;
		}

		private static DiscoveryMetadata CreateDiscoveryMetadata(string signature)
		{
			Func<DiscoveryMetadata> factory = null;
			if (DiscoveryMetadataFactories.TryGetValue(signature, out factory))
			{
				return factory();
			}

			return null;
		}

		/// <summary>
		/// Escapes the specified string.
		/// </summary>
		/// <param name="s">The string to escape.</param>
		public static string Escape(string s)
		{
			if (s == null)
			{
				return "@";
			}

			return s.Replace(@"\", @"\s").Replace(@"|", @"\p").Replace(@"=", @"\q").Replace("@", @"\a");
		}

		/// <summary>
		/// Unescapes the specified string.
		/// </summary>
		/// <param name="s">Escaped string to unescape.</param>
		public static string Unescape(string s)
		{
			if (s == null || s == "@")
			{
				return null;
			}

			return s.Replace(@"\a", "@").Replace(@"\q", @"=").Replace(@"\p", "|").Replace(@"\s", @"\");
		}

		/// <summary>
		/// Encodes the specified discovery metadata packet into byte array.
		/// </summary>
		/// <param name="dp">The packet to encode.</param>
		public static byte[] Encode(DiscoveryMetadata dp)
		{
			// convert the packet into string
			var sb = new StringBuilder();
			sb.AppendFormat("{0}|", Escape(dp.Signature));
			foreach (var entry in dp.Properties)
			{
				sb.AppendFormat("{0}={1}|", Escape(entry.Key), Escape(entry.Value));
			}

			// calculate checksum and merge
			var content = Encoding.UTF8.GetBytes(sb.ToString());
			var crc = new Crc32Calculator();
			crc.UpdateWithBlock(content, 0, content.Length);
			var checksum = BitConverter.GetBytes(crc.Crc32);
			var contentLength = BitConverter.GetBytes(content.Length);

			// packet contents: length, checksum, content
			return contentLength.Concat(checksum).Concat(content).ToArray();
		}

		/// <summary>
		/// Decodes the specified discovery metadata packet.
		/// </summary>
		/// <param name="packet">The packet to decode.</param>
		public static DiscoveryMetadata Decode(byte[] packet)
		{
			// packet contains a length and a checksum at very least
			if (packet.Length < sizeof(Int32) * 2)
			{
				return null;
			}

			// check content length
			var contentLength = BitConverter.ToInt32(packet, 0);
			if (packet.Length != contentLength + sizeof(Int32) * 2) // content + length + checksum
			{
				return null;
			}

			// validate checksum
			var checksum = BitConverter.ToUInt32(packet, sizeof(Int32));
			var crc = new Crc32Calculator();
			crc.UpdateWithBlock(packet, sizeof(Int32) * 2, contentLength);
			if (checksum != crc.Crc32)
			{
				return null;
			}

			// decode contents
			var contents = packet.Skip(sizeof(Int32) * 2).ToArray();
			var data = Encoding.UTF8.GetString(contents).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			if (data.Length < 1)
			{
				return null;
			}

			// extract signature and create the packet
			var signature = Unescape(data.First());
			var dp = CreateDiscoveryMetadata(signature);
			if (dp == null)
			{
				return null;
			}

			// populate properties
			foreach (var pair in data.Skip(1))
			{
				var entry = pair.Split('=');
				dp.Properties[Unescape(entry.First())] = Unescape(entry.Last());
			}

			return dp;
		}
	}
}
