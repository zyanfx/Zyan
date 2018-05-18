using System;
using System.Collections.Concurrent;
using System.Security;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Security.SecureRemotePassword
{
	/// <summary>
	/// Server-side: authentication provider for the SRP-6a protocol.
	/// </summary>
	/// <seealso cref="IAuthenticationProvider" />
	public class SrpAuthenticationProvider : IAuthenticationProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SrpAuthenticationProvider"/> class.
		/// </summary>
		/// <param name="repository">User account repository.</param>
		/// <param name="parameters">Optional SRP-6a protocol parameters.</param>
		public SrpAuthenticationProvider(ISrpAccountRepository repository, SrpParameters parameters = null)
		{
			AuthRepository = repository;
			SrpServer = new SrpServer(parameters);
		}

		private ISrpAccountRepository AuthRepository { get; set; }

		private SrpServer SrpServer { get; set; }

		// fixme: add a timeout to clean up pending data from step1 requests not followed by step2
		internal ConcurrentDictionary<string, Step1Data> PendingAuthentications { get; } =
			new ConcurrentDictionary<string, Step1Data>();

		// variables produced on the first authentication step
		internal class Step1Data
		{
			public string UserName { get; set; }
			public string Salt { get; set; }
			public string Verifier { get; set; }
			public string ClientEphemeralPublic { get; set; }
			public SrpEphemeral ServerEphemeral { get; set; }
		}

		/// <inheritdoc/>
		public AuthResponseMessage Authenticate(AuthRequestMessage authRequest)
		{
			if (authRequest.Credentials == null)
				return Error("No credentials specified");

			if (!authRequest.Credentials.ContainsKey(SrpProtocolConstants.SRP_STEP_NUMBER))
				return Error("Authentication step not specified");

			if (!authRequest.Credentials.ContainsKey(SrpProtocolConstants.SRP_SESSION_ID))
				return Error("Authentication session identity is not specified");

			// step number and session identity
			var step = Convert.ToInt32(authRequest.Credentials[SrpProtocolConstants.SRP_STEP_NUMBER]);
			var sessionId = Convert.ToString(authRequest.Credentials[SrpProtocolConstants.SRP_SESSION_ID]);

			// first step never fails: User -> Host: I, A = g^a (identifies self, a = random number)
			if (step == 1)
			{
				var userName = (string)authRequest.Credentials[SrpProtocolConstants.SRP_USERNAME];
				var clientEphemeralPublic = (string)authRequest.Credentials[SrpProtocolConstants.SRP_CLIENT_PUBLIC_EPHEMERAL];
				var account = AuthRepository.FindByName(userName);
				if (account != null)
				{
					// save the data for the second authentication step
					var salt = account.Salt;
					var verifier = account.Verifier;
					var serverEphemeral = SrpServer.GenerateEphemeral(verifier);
					PendingAuthentications[sessionId] = new Step1Data
					{
						UserName = userName,
						Salt = salt,
						Verifier = verifier,
						ClientEphemeralPublic = clientEphemeralPublic,
						ServerEphemeral = serverEphemeral
					};

					// Host -> User: s, B = kv + g^b (sends salt, b = random number)
					return ResponseStep1(salt, serverEphemeral.Public);
				}

				// generate fake salt and B values so that attacker cannot tell whether the given user exists or not
				var fakeSalt = SrpServer.Parameters.H(userName).ToHex();
				var fakeEphemeral = SrpServer.GenerateEphemeral(fakeSalt);
				return ResponseStep1(fakeSalt, fakeEphemeral.Public);
			}

			// second step may fail: User -> Host: M = H(H(N) xor H(g), H(I), s, A, B, K)
			if (step == 2)
			{
				// get the values calculated on the first step
				Step1Data vars;
				if (!PendingAuthentications.TryRemove(sessionId, out vars))
				{
					return Error("Authentication failed: retry the first step");
				}

				try
				{
					// Host -> User: H(A, M, K)
					var clientSessionProof = (string)authRequest.Credentials[SrpProtocolConstants.SRP_CLIENT_SESSION_PROOF];
					var serverSession = SrpServer.DeriveSession(vars.ServerEphemeral.Secret, vars.ClientEphemeralPublic, vars.Salt, vars.UserName, vars.Verifier, clientSessionProof);
					return ResponseStep2(serverSession.Proof);
				}
				catch (SecurityException ex)
				{
					return Error("Authentication failed: " + ex.Message);
				}
			}

			// step number should be either 1 or 2
			return Error("Unknown authentication step");
		}

		private AuthResponseMessage ResponseStep1(string salt, string serverPublicEphemeral)
		{
			var result = new AuthResponseMessage { Completed = false, Success = true };
			result.AddParameter(SrpProtocolConstants.SRP_SALT, salt);
			result.AddParameter(SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL, serverPublicEphemeral);
			return result;
		}

		private AuthResponseMessage ResponseStep2(string serverSessionProof)
		{
			var result = new AuthResponseMessage { Completed = true, Success = true };
			result.AddParameter(SrpProtocolConstants.SRP_SERVER_SESSION_PROOF, serverSessionProof);
			return result;
		}

		private AuthResponseMessage Error(string message) => new AuthResponseMessage
		{
			Success = false,
			Completed = true,
			ErrorMessage = message,
		};
	}
}
