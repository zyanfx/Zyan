using System;
using System.Collections.Concurrent;
using System.Security;
using SecureRemotePassword;
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
			SrpParameters = parameters ?? new SrpParameters();
			SrpServer = new SrpServer(SrpParameters);
			UnknownUserSalt = new SrpClient(parameters).GenerateSalt();
		}

		private ISrpAccountRepository AuthRepository { get; set; }

		private SrpParameters SrpParameters { get; set; }

		private SrpServer SrpServer { get; set; }

		private string UnknownUserSalt { get; set; }

		// fixme: add a timeout to clean up pending data from step1 requests not followed by step2
		internal ConcurrentDictionary<Guid, Step1Data> PendingAuthentications { get; } =
			new ConcurrentDictionary<Guid, Step1Data>();

		// variables produced on the first authentication step
		internal class Step1Data
		{
			public ISrpAccount Account { get; set; }
			public string ClientEphemeralPublic { get; set; }
			public SrpEphemeral ServerEphemeral { get; set; }
		}

		internal class MissingSrpAccount : ISrpAccount
		{
			public string UserName { get; set; }
			public string Salt => "1234";
			public string Verifier => "4321";
		}

		/// <inheritdoc/>
		public AuthResponseMessage Authenticate(AuthRequestMessage authRequest)
		{
			if (authRequest.Credentials == null)
				return Error("No credentials specified");

			if (!authRequest.Credentials.ContainsKey(SrpProtocolConstants.SRP_STEP_NUMBER))
				return Error("Authentication protocol not supported: step number not specified");

			// step number and session identity
			switch (Convert.ToInt32(authRequest.Credentials[SrpProtocolConstants.SRP_STEP_NUMBER]))
			{
				case 1:
					return AuthStep1(authRequest);

				case 2:
					return AuthStep2(authRequest);

				// step number should be either 1 or 2
				default:
					return Error("Authentication protocol not supported: unknown step number");
			}
		}

		private AuthResponseMessage AuthStep1(AuthRequestMessage authRequest)
		{
			// first step never fails: User -> Host: I, A = g^a (identifies self, a = random number)
			var userName = (string)authRequest.Credentials[SrpProtocolConstants.SRP_USERNAME];
			var clientEphemeralPublic = (string)authRequest.Credentials[SrpProtocolConstants.SRP_CLIENT_PUBLIC_EPHEMERAL];
			var account = AuthRepository.FindByName(userName);
			if (account != null)
			{
				// save the data for the second authentication step
				var salt = account.Salt;
				var verifier = account.Verifier;
				var serverEphemeral = SrpServer.GenerateEphemeral(verifier);
				PendingAuthentications[authRequest.SessionID] = new Step1Data
				{
					Account = account,
					ClientEphemeralPublic = clientEphemeralPublic,
					ServerEphemeral = serverEphemeral
				};

				// Host -> User: s, B = kv + g^b (sends salt, b = random number)
				return ResponseStep1(salt, serverEphemeral.Public);
			}

			// generate fake salt and B values so that attacker cannot tell whether the given user exists or not
			var fakeSalt = SrpParameters.Hash(userName + UnknownUserSalt).ToHex();
			var fakeEphemeral = SrpServer.GenerateEphemeral(fakeSalt);
			PendingAuthentications[authRequest.SessionID] = new Step1Data
			{
				Account = new MissingSrpAccount { UserName = userName },
				ClientEphemeralPublic = clientEphemeralPublic,
				ServerEphemeral = fakeEphemeral
			};

			return ResponseStep1(fakeSalt, fakeEphemeral.Public);
		}

		private AuthResponseMessage AuthStep2(AuthRequestMessage authRequest)
		{
			// get the values calculated on the first step
			Step1Data vars;
			if (!PendingAuthentications.TryRemove(authRequest.SessionID, out vars))
			{
				return Error("Authentication failed: retry the first step");
			}

			try
			{
				if (vars.Account is MissingSrpAccount)
					throw new SecurityException();

				// second step may fail: User -> Host: M = H(H(N) xor H(g), H(I), s, A, B, K)
				var clientSessionProof = (string)authRequest.Credentials[SrpProtocolConstants.SRP_CLIENT_SESSION_PROOF];
				var serverSession = SrpServer.DeriveSession(vars.ServerEphemeral.Secret, vars.ClientEphemeralPublic,
					vars.Account.Salt, vars.Account.UserName, vars.Account.Verifier, clientSessionProof);

				// Host -> User: H(A, M, K)
				return ResponseStep2(serverSession.Proof, vars.Account);
			}
			catch (SecurityException)
			{
				return Error("Authentication failed: bad password or user name");
			}
		}

		private AuthResponseMessage ResponseStep1(string salt, string serverPublicEphemeral)
		{
			var result = new AuthResponseMessage { Completed = false, Success = true };
			result.AddParameter(SrpProtocolConstants.SRP_SALT, salt);
			result.AddParameter(SrpProtocolConstants.SRP_SERVER_PUBLIC_EPHEMERAL, serverPublicEphemeral);
			return result;
		}

		private AuthResponseMessage ResponseStep2(string serverSessionProof, ISrpAccount account)
		{
			var result = new AuthResponseMessage { Completed = true, Success = true };
			result.AddParameter(SrpProtocolConstants.SRP_SERVER_SESSION_PROOF, serverSessionProof);
			result.AuthenticatedIdentity = AuthRepository.GetIdentity(account);
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
