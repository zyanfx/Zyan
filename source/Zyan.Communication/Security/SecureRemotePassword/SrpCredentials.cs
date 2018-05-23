using System;
using System.Collections;

namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// Client-side: credentials for the SRP-6a authentication protocol.
	/// </summary>
	public class SrpCredentials : AuthCredentials
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SrpCredentials"/> class.
		/// </summary>
		/// <param name="userName">Name of the user.</param>
		/// <param name="password">The password.</param>
		/// <param name="parameters">Optional SRP-6a protocol parameters.</param>
		public SrpCredentials(string userName, string password, SrpParameters parameters = null)
		{
			UserName = userName;
			Password = password;
			SrpClient = new SrpClient(parameters);
		}

		private SrpClient SrpClient { get; set; }

		/// <inheritdoc/>
		public override void Authenticate(Guid sessionId, IZyanDispatcher dispatcher)
		{
			// step1 request: User -> Host: I, A = g^a (identifies self, a = random number)
			var clientEphemeral = SrpClient.GenerateEphemeral();
			var request1 = new Hashtable
			{
				{ SrpProtocolConstants.SRP_STEP_NUMBER, 1 },
				{ SrpProtocolConstants.SRP_USERNAME, UserName },
				{ SrpProtocolConstants.SRP_CLIENT_PUBLIC_EPHEMERAL, clientEphemeral.Public },
			};

			// step1 response: Host -> User: s, B = kv + g^b (sends salt, b = random number)
			var response1 = dispatcher.Logon(sessionId, request1).Parameters;
			var salt = (string)response1[SrpProtocolConstants.SRP_SALT];
			var serverPublicEphemeral = (string)response1[SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL];

			// step2 request: User -> Host: M = H(H(N) xor H(g), H(I), s, A, B, K)
			var privateKey = SrpClient.DerivePrivateKey(salt, UserName, Password);
			var clientSession = SrpClient.DeriveSession(clientEphemeral.Secret, serverPublicEphemeral, salt, UserName, privateKey);
			var request2 = new Hashtable
			{
				{ SrpProtocolConstants.SRP_STEP_NUMBER, 2 },
				{ SrpProtocolConstants.SRP_CLIENT_SESSION_PROOF, clientSession.Proof },
			};

			// step2 response: Host -> User: H(A, M, K)
			var response2 = dispatcher.Logon(sessionId, request2).Parameters;
			var serverSessionProof = (string)response2[SrpProtocolConstants.SRP_SERVER_SESSION_PROOF];
			SrpClient.VerifySession(clientEphemeral.Public, clientSession, serverSessionProof);
		}
	}
}
