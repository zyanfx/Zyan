using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// Hash function signature.
	/// Computes the hash of the specified <see cref="string"/> values.
	/// </summary>
	/// <param name="values">The values.</param>
	public delegate SrpInteger SrpHash(params string[] values);

	/// <summary>
	/// Hashing algorithms for the SRP protocol.
	/// </summary>
	public static class SrpHash<T> where T : HashAlgorithm
	{
		/// <summary>
		/// Gets the hash function.
		/// </summary>
		public static SrpHash HashFunction { get; } = ComputeHash;

		/// <summary>
		/// Gets the size of the hash in bytes.
		/// </summary>
		public static int HashSizeBytes { get; } = CreateHasher().HashSize / 8;

		private static SrpInteger ComputeHash(params string[] values) =>
			ComputeHash(Combine(values.Select(v => GetBytes(v))));

		private static T CreateHasher()
		{
			var algorithm = typeof(T).FullName;
			return (T)CryptoConfig.CreateFromName(algorithm);
		}

		private static SrpInteger ComputeHash(byte[] data)
		{
			var hasher = CreateHasher();
			var hash = hasher.ComputeHash(data);

			// reverse the byte order for the little-endian encoding — doesn't take the sign into account
			//return SrpInteger.FromBytes(hash.Reverse().ToArray());

			// should yield the same result:
			var hex = hash.Aggregate(new StringBuilder(), (sb, b) => sb.Append(b.ToString("X2")), sb => sb.ToString());
			return SrpInteger.FromHex(hex);
		}

		private static byte[] EmptyBuffer = new byte[0];

		private static byte[] GetBytes(string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				return Encoding.UTF8.GetBytes(value);
			}

			return EmptyBuffer;
		}

		private static byte[] Combine(IEnumerable<byte[]> arrays)
		{
			var rv = new byte[arrays.Sum(a => a.Length)];
			var offset = 0;

			foreach (var array in arrays)
			{
				Buffer.BlockCopy(array, 0, rv, offset, array.Length);
				offset += array.Length;
			}

			return rv;
		}
	}
}
