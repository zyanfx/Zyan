using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// Client-side code of the SRP 6a protocol.
	/// </summary>
	public class SrpClient
	{
		/// <summary>
		/// Generates the random salt of the same size as a used hash.
		/// </summary>
		public static string GenerateSalt()
		{
			var hashSize = SrpParameters.Default.HashSizeBytes;
			return SrpInteger.RandomInteger(hashSize).ToHex();
		}

		/// <summary>
		/// Derives the private key from the given salt, user name and password.
		/// </summary>
		/// <param name="salt">The salt.</param>
		/// <param name="userName">The name of the user.</param>
		/// <param name="password">The password.</param>
		public static string DerivePrivateKey(string salt, string userName, string password)
		{
			// H() — One-way hash function
			var H = SrpParameters.Default.H;

			// validate the parameters:
			// s — User's salt, hexadecimal
			// I — login
			// p — Cleartext Password
			var s = SrpInteger.FromHex(salt);
			var I = userName + string.Empty;
			var p = password + string.Empty;

			// x = H(s, H(I | ':' | p))  (s is chosen randomly)
			var x = H(s, H($"{I}:{p}"));
			return x.ToHex();
		}

		/// <summary>
		/// Derives the verifier from the private key.
		/// </summary>
		/// <param name="privateKey">The private key.</param>
		public static string DeriveVerifier(string privateKey)
		{
			// N — A large safe prime (N = 2q+1, where q is prime)
			// g — A generator modulo N
			var N = SrpParameters.Default.N;
			var g = SrpParameters.Default.G;

			// x — Private key (derived from p and s)
			var x = SrpInteger.FromHex(privateKey);

			// v = g^x (computes password verifier)
			var v = g.ModPow(x, N);
			return v.ToHex();
		}

		/// <summary>
		/// Generates the ephemeral value.
		/// </summary>
		public static SrpEphemeral GenerateEphemeral()
		{
			// N — A large safe prime (N = 2q+1, where q is prime)
			// g — A generator modulo N
			var N = SrpParameters.Default.N;
			var g = SrpParameters.Default.G;
			var hashSize = SrpParameters.Default.HashSizeBytes;

			// A = g^a (a = random number)
			var a = SrpInteger.RandomInteger(hashSize);
			var A = g.ModPow(a, N);

			return new SrpEphemeral
			{
				Secret = a.ToHex(),
				Public = A.ToHex(),
			};
		}
	}
}
