using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Toolbox
{
	internal static class ChecksumHelper
	{
		public static string ComputeHash(IEnumerable<Guid> guids) =>
			ComputeHash<SHA256>(guids);

		public static string ComputeHash<T>(IEnumerable<Guid> guids)
			where T : HashAlgorithm
		{
			var algorithm = typeof(T).FullName;
			var hasher = (T)CryptoConfig.CreateFromName(algorithm);
			var chunks = guids.EmptyIfNull().OrderBy(i => i).Select(i => i.ToByteArray());

			foreach (var chunk in chunks)
			{
				hasher.TransformBlock(chunk, 0, chunk.Length, chunk, 0);
			}

			hasher.TransformFinalBlock(new byte[0], 0, 0);
			return string.Concat(hasher.Hash.Select(i => i.ToString("x2")).ToArray());
		}
	}
}
