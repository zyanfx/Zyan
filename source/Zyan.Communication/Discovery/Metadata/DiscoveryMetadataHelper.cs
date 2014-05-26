using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Discovery.Metadata
{
	/// <summary>
	/// Helper methods for discovery metadata packets.
	/// </summary>
	public static class DiscoveryMetadataHelper
	{
		/// <summary>
		/// Initializes the <see cref="DiscoveryMetadataHelper"/> class.
		/// </summary>
		static DiscoveryMetadataHelper()
		{
			// register standard discovery types
			DiscoveryMetadataFactories = new Dictionary<string, Func<DiscoveryMetadata>>();
			RegisterDiscoveryMetadataFactory(DiscoveryRequest.SignatureString, () => new DiscoveryRequest());
			RegisterDiscoveryMetadataFactory(DiscoveryResponse.SignatureString, () => new DiscoveryResponse());
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

		/// <summary>
		/// Creates the discovery metadata packed based on the given packet signature.
		/// </summary>
		/// <param name="signature">The signature.</param>
		public static DiscoveryMetadata CreateDiscoveryMetadata(string signature)
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
