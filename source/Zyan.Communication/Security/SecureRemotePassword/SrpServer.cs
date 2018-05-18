using System.Security;

namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// Server-side code of the SRP-6a protocol.
	/// </summary>
	public class SrpServer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SrpServer"/> class.
		/// </summary>
		/// <param name="parameters">The parameters of the SRP-6a protocol.</param>
		public SrpServer(SrpParameters parameters = null)
		{
			Parameters = parameters ?? new SrpParameters();
		}

		/// <summary>
		/// Gets or sets the protocol parameters.
		/// </summary>
		internal SrpParameters Parameters { get; set; }

		/// <summary>
		/// Generates the ephemeral value from the given verifier.
		/// </summary>
		public SrpEphemeral GenerateEphemeral(string verifier)
		{
			// N — A large safe prime (N = 2q+1, where q is prime)
			// g — A generator modulo N
			// k — Multiplier parameter (k = H(N, g) in SRP-6a, k = 3 for legacy SRP-6)
			var N = Parameters.N;
			var g = Parameters.G;
			var k = Parameters.K;
			var size = Parameters.HashSizeBytes;

			// v — Password verifier
			var v = SrpInteger.FromHex(verifier);

			// B = kv + g^b (b = random number)
			var b = SrpInteger.RandomInteger(size);
			var B = (k * v + g.ModPow(b, N)) % N;

			return new SrpEphemeral
			{
				Secret = b.ToHex(),
				Public = B.ToHex(),
			};
		}

		/// <summary>
		/// Derives the server session.
		/// </summary>
		/// <param name="serverSecretEphemeral">The server secret ephemeral.</param>
		/// <param name="clientPublicEphemeral">The client public ephemeral.</param>
		/// <param name="salt">The salt.</param>
		/// <param name="username">The username.</param>
		/// <param name="verifier">The verifier.</param>
		/// <param name="clientSessionProof">The client session proof value.</param>
		/// <returns>Session key and proof.</returns>
		public SrpSession DeriveSession(string serverSecretEphemeral, string clientPublicEphemeral, string salt, string username, string verifier, string clientSessionProof)
		{
			// N — A large safe prime (N = 2q+1, where q is prime)
			// g — A generator modulo N
			// k — Multiplier parameter (k = H(N, g) in SRP-6a, k = 3 for legacy SRP-6)
			// H — One-way hash function
			var N = Parameters.N;
			var g = Parameters.G;
			var k = Parameters.K;
			var H = Parameters.H;

			// b — Secret ephemeral values
			// A — Public ephemeral values
			// s — User's salt
			// p — Cleartext Password
			// I — Username
			// v — Password verifier
			var b = SrpInteger.FromHex(serverSecretEphemeral);
			var A = SrpInteger.FromHex(clientPublicEphemeral);
			var s = SrpInteger.FromHex(salt);
			var I = username + string.Empty;
			var v = SrpInteger.FromHex(verifier);

			// B = kv + g^b (b = random number)
			var B = (k * v + g.ModPow(b, N)) % N;

			// A % N > 0
			if (A % N == 0)
			{
				// fixme: .code, .statusCode, etc.
				throw new SecurityException("The client sent an invalid public ephemeral");
			}

			// u = H(A, B)
			var u = H(A, B);

			// S = (Av^u) ^ b (computes session key)
			var S = (A * v.ModPow(u, N)).ModPow(b, N);

			// K = H(S)
			var K = H(S);

			// M = H(H(N) xor H(g), H(I), s, A, B, K)
			var M = H(H(N) ^ H(g), H(I), s, A, B, K);

			// validate client session proof
			var expected = M;
			var actual = SrpInteger.FromHex(clientSessionProof);
			if (actual != expected)
			{
				// fixme: .code, .statusCode, etc.
				throw new SecurityException("Client provided session proof is invalid");
			}

			// P = H(A, M, K)
			var P = H(A, M, K);

			return new SrpSession
			{
				Key = K.ToHex(),
				Proof = P.ToHex(),
			};
		}
	}
}
