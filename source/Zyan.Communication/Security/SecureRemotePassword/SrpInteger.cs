using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// A thin wrapper over the <see cref="BigInteger"/> class
	/// represented as a fixed-length hexadecimal string (optional).
	/// </summary>
	public class SrpInteger
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SrpInteger"/> class.
		/// </summary>
		/// <param name="hex">The hexadecimal.</param>
		/// <param name="hexLength">Length of the hexadecimal.</param>
		public SrpInteger(string hex, int? hexLength = null)
		{
			hex = NormalizeWhitespace(hex);
			HexLength = hexLength;

			// append leading zero to make sure we get a positive BigInteger value
			Value = BigInteger.Parse("0" + hex, NumberStyles.HexNumber);
		}

		/// <summary>
		/// Prevents a default instance of the <see cref="SrpInteger"/> class from being created.
		/// </summary>
		private SrpInteger()
		{
		}

		/// <summary>
		/// Normalizes the whitespace.
		/// </summary>
		/// <param name="hexNumber">The hexadecimal number.</param>
		private static string NormalizeWhitespace(string hexNumber) =>
			Regex.Replace(hexNumber ?? string.Empty, @"[\s_]", string.Empty);

		private BigInteger Value { get; set; }

		private int? HexLength { get; set; }

		/// <summary>
		/// Generates the random integer number.
		/// </summary>
		/// <param name="bytes">The number length in bytes.</param>
		public static SrpInteger RandomInteger(int bytes)
		{
			if (bytes <= 0)
			{
				throw new ArgumentException("Integer size in bytes should be positive", "bytes");
			}

			var random = new RNGCryptoServiceProvider();
			var randomBytes = new byte[bytes];
			random.GetNonZeroBytes(randomBytes);

			return new SrpInteger
			{
				Value = new BigInteger(randomBytes),
				HexLength = bytes * 2,
			};
		}

		/// <summary>
		/// Returns the fixed-length hexadecimal representation of the <see cref="SrpInteger"/> instance.
		/// </summary>
		public string ToHex()
		{
			if (!HexLength.HasValue)
			{
				throw new InvalidOperationException("Hexadecimal length is not specified");
			}

			// ToString may add extra leading zeros to the positive BigIntegers, so we trim them first
			return Value.ToString("X").TrimStart('0').PadLeft(HexLength.Value, '0');
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="SrpInteger"/> to <see cref="System.String"/>.
		/// </summary>
		/// <param name="srpint">The <see cref="SrpInteger"/> instance.</param>
		public static implicit operator string(SrpInteger srpint) => srpint.ToHex();

		/// <summary>
		/// Returns a new <see cref="SrpInteger"/> instance from the given hexadecimal string.
		/// </summary>
		/// <param name="hex">The hexadecimal string.</param>
		public static SrpInteger FromHex(string hex)
		{
			if (string.IsNullOrEmpty(hex))
			{
				hex = "0";
			}

			return new SrpInteger(hex, hex.Length);
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			var hex = Value.ToString("X");
			if (hex.Length > 16)
			{
				hex = hex.Substring(0, 16) + "...";
			}

			return $"<SrpInteger: {hex}>";
		}
	}
}