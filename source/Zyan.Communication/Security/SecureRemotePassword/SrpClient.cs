using System.Security;

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

		/// <summary>
		/// Derives the client session.
		/// </summary>
		/// <param name="clientSecretEphemeral">The client secret ephemeral.</param>
		/// <param name="serverPublicEphemeral">The server public ephemeral.</param>
		/// <param name="salt">The salt.</param>
		/// <param name="username">The username.</param>
		/// <param name="privateKey">The private key.</param>
		/// <returns>Session key and proof.</returns>
		public static SrpSession DeriveSession(string clientSecretEphemeral, string serverPublicEphemeral, string salt, string username, string privateKey)
		{
			// N — A large safe prime (N = 2q+1, where q is prime)
			// g — A generator modulo N
			// k — Multiplier parameter (k = H(N, g) in SRP-6a, k = 3 for legacy SRP-6)
			// H — One-way hash function
			var N = SrpParameters.Default.N;
			var g = SrpParameters.Default.G;
			var k = SrpParameters.Default.K;
			var H = SrpParameters.Default.H;

			// a — Secret ephemeral value
			// B — Public ephemeral value
			// s — User's salt
			// I — Username
			// x — Private key (derived from p and s)
			var a = SrpInteger.FromHex(clientSecretEphemeral);
			var B = SrpInteger.FromHex(serverPublicEphemeral);
			var s = SrpInteger.FromHex(salt);
			var I = username + string.Empty;
			var x = SrpInteger.FromHex(privateKey);

			// A = g^a (a = random number)
			var A = g.ModPow(a, N);

			// B % N > 0
			if (B % N == SrpInteger.Zero)
			{
				// fixme: .code, .statusCode, etc.
				throw new SecurityException("The server sent an invalid public ephemeral");
			}

			// u = H(A, B)
			var u = H(A, B);

			// S = (B - kg^x) ^ (a + ux)
			var S = (B - (k * (g.ModPow(x, N)))).ModPow(a + (u * x), N);

			// K = H(S)
			var K = H(S);

			// M = H(H(N) xor H(g), H(I), s, A, B, K)
			var M = H(H(N) ^ H(g), H(I), s, A, B, K);

			return new SrpSession
			{
				Key = K.ToHex(),
				Proof = M.ToHex()
			};
		}
	}
}
