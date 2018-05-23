using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// A thin wrapper over the <see cref="BigInteger"/> class
	/// represented as a fixed-length hexadecimal string (optional).
	/// </summary>
	public class SrpInteger : IEquatable<SrpInteger>
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

			var sign = hex.StartsWith("-") ? -1 : 1;
			hex = hex.TrimStart('-');

			// append leading zero to make sure we get a positive BigInteger value
			Value = sign * BigInteger.Parse("0" + hex, NumberStyles.HexNumber);
		}

		/// <summary>
		/// Prevents a default instance of the <see cref="SrpInteger"/> class from being created.
		/// </summary>
		private SrpInteger()
		{
			HexLength = 1;
			Value = BigInteger.Zero;
		}

		/// <summary>
		/// Normalizes the whitespace.
		/// </summary>
		/// <param name="hexNumber">The hexadecimal number.</param>
		private static string NormalizeWhitespace(string hexNumber) =>
			Regex.Replace(hexNumber ?? string.Empty, @"[\s_]", string.Empty);

		/// <summary>
		/// The <see cref="SrpInteger"/> value representing 0.
		/// </summary>
		public static SrpInteger Zero { get; } = new SrpInteger();

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		private BigInteger Value { get; set; }

		/// <summary>
		/// Gets the hexadecimal length.
		/// </summary>
		internal int? HexLength { get; private set; }

		/// <summary>
		/// Pads the value to the specified new hexadecimal length.
		/// </summary>
		/// <param name="newLength">The new length.</param>
		public SrpInteger Pad(int newLength) => new SrpInteger
		{
			Value = Value,
			HexLength = newLength,
		};

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

			// make sure random number is positive
			var result = FromBytes(randomBytes);
			if (result.Value < 0)
			{
				result.Value = -result.Value;
			}

			return result;
		}

		/// <summary>
		/// Raises the number to the power of the given exponent modulo given modulus.
		/// </summary>
		/// <param name="exponent">The exponent.</param>
		/// <param name="modulus">The modulus.</param>
		public SrpInteger ModPow(SrpInteger exponent, SrpInteger modulus)
		{
			var value = BigInteger.ModPow(Value, exponent.Value, modulus.Value);
			if (value < 0)
			{
				value = modulus.Value + value;
			}

			return new SrpInteger
			{
				Value = value,
				HexLength = modulus.HexLength,
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

			var sign = string.Empty;
			var value = Value;
			if (Value < 0)
			{
				sign = "-";
				value = -Value;
			}

			// ToString may add extra leading zeros to the positive BigIntegers, so we trim them first
			return sign + value.ToString("x").TrimStart('0').PadLeft(HexLength.Value, '0');
		}

		/// <summary>
		/// Returns the byte array representing the given value in big endian encoding.
		/// </summary>
		/// <remarks>
		/// Skips extra leading zeros produced by BigInteger.ToByteArray(), if any.
		/// Pads the resulting value with leading zeros to match the HexLength property.
		/// </remarks>
		public byte[] ToByteArray()
		{
			var array = Value.ToByteArray().Reverse().SkipWhile(v => v == 0).ToArray();
			if (!HexLength.HasValue || HexLength.Value <= array.Length * 2)
			{
				// no padding required
				return array;
			}

			// pad with leading zeros
			var length = HexLength.Value / 2;
			var result = new byte[length];
			Buffer.BlockCopy(array, 0, result, length - array.Length, array.Length);
			return result;
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="SrpInteger"/> to <see cref="string"/>.
		/// </summary>
		/// <param name="srpint">The <see cref="SrpInteger"/> instance.</param>
		public static implicit operator string(SrpInteger srpint) => srpint.ToHex();

		/// <summary>
		/// Performs an implicit conversion from <see cref="int"/> to <see cref="SrpInteger"/>.
		/// </summary>
		/// <param name="integer">The <see cref="int"/> value.</param>
		public static implicit operator SrpInteger(int integer) => FromHex(integer.ToString("X"));

		/// <summary>
		/// Performs an implicit conversion from <see cref="uint"/> to <see cref="SrpInteger"/>.
		/// </summary>
		/// <param name="integer">The <see cref="uint"/> value.</param>
		public static implicit operator SrpInteger(uint integer) => FromHex(integer.ToString("X"));

		/// <summary>
		/// Performs an implicit conversion from <see cref="long"/> to <see cref="SrpInteger"/>.
		/// </summary>
		/// <param name="integer">The <see cref="long"/> value.</param>
		public static implicit operator SrpInteger(long integer) => FromHex(integer.ToString("X"));

		/// <summary>
		/// Performs an implicit conversion from <see cref="ulong"/> to <see cref="SrpInteger"/>.
		/// </summary>
		/// <param name="integer">The <see cref="ulong"/> value.</param>
		public static implicit operator SrpInteger(ulong integer) => FromHex(integer.ToString("X"));

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		public static bool operator ==(SrpInteger left, SrpInteger right) => Equals(left, right);

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		public static bool operator !=(SrpInteger left, SrpInteger right) => !Equals(left, right);

		/// <summary>
		/// Implements the operator -.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		public static SrpInteger operator -(SrpInteger left, SrpInteger right)
		{
			return new SrpInteger
			{
				Value = left.Value - right.Value,
				HexLength = left.HexLength,
			};
		}

		/// <summary>
		/// Implements the operator +.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		public static SrpInteger operator +(SrpInteger left, SrpInteger right)
		{
			return new SrpInteger
			{
				Value = left.Value + right.Value,
				HexLength = left.HexLength,
			};
		}

		/// <summary>
		/// Implements the operator /.
		/// </summary>
		/// <param name="dividend">The dividend.</param>
		/// <param name="divisor">The divisor.</param>
		public static SrpInteger operator /(SrpInteger dividend, SrpInteger divisor)
		{
			return new SrpInteger
			{
				Value = dividend.Value / divisor.Value,
				HexLength = dividend.HexLength,
			};
		}

		/// <summary>
		/// Implements the operator %.
		/// </summary>
		/// <param name="dividend">The dividend.</param>
		/// <param name="divisor">The divisor.</param>
		public static SrpInteger operator %(SrpInteger dividend, SrpInteger divisor)
		{
			return new SrpInteger
			{
				Value = dividend.Value % divisor.Value,
				HexLength = dividend.HexLength,
			};
		}

		/// <summary>
		/// Implements the operator *.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		public static SrpInteger operator *(SrpInteger left, SrpInteger right)
		{
			return new SrpInteger
			{
				Value = left.Value * right.Value,
				HexLength = left.HexLength,
			};
		}

		/// <summary>
		/// Implements the operator ^ (xor).
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		public static SrpInteger operator ^(SrpInteger left, SrpInteger right)
		{
			return new SrpInteger
			{
				Value = left.Value ^ right.Value,
				HexLength = left.HexLength,
			};
		}

		/// <summary>
		/// Returns a new <see cref="SrpInteger"/> instance from the given hexadecimal string.
		/// </summary>
		/// <param name="bytes">The array of bytes.</param>
		public static SrpInteger FromBytes(byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
			{
				return Zero;
			}

			return new SrpInteger
			{
				Value = new BigInteger(bytes),
				HexLength = bytes.Length * 2,
			};
		}

		/// <summary>
		/// Returns a new <see cref="SrpInteger"/> instance from the given array of bytes.
		/// </summary>
		/// <param name="hex">The hexadecimal string.</param>
		public static SrpInteger FromHex(string hex)
		{
			if (string.IsNullOrEmpty(hex))
			{
				hex = "0";
			}

			var hexLength = NormalizeWhitespace(hex).Trim(' ', '-').Length;
			return new SrpInteger(hex, hexLength);
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			var hex = Value.ToString("x");
			if (hex.Length > 16)
			{
				hex = hex.Substring(0, 16) + "...";
			}

			return $"<SrpInteger: {hex}>";
		}

		/// <inheritdoc/>
		public bool Equals(SrpInteger other) => other != null && Value == other.Value; // ignore HexLength

		/// <inheritdoc/>
		public override bool Equals(object obj) => Equals(obj as SrpInteger);

		/// <inheritdoc/>
		public override int GetHashCode() => Value.GetHashCode();
	}
}