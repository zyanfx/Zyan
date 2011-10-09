using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols.Http
{
	/// <summary>
	/// Server protocol setup für HTTP communication with support for user defined authentication and security.
	/// </summary>
	public class HttpCustomServerProtocolSetup : ServerProtocolSetup
	{
		private bool _encryption = true;
		private string _algorithm = "3DES";
		private bool _oaep = false;
		private int _httpPort = 0;

		/// <summary>
		/// Gets or sets the HTTP port number.
		/// </summary>
		public int HttpPort
		{
			get { return _httpPort; }
			set
			{
				if (_httpPort < 0 || _httpPort > 65535)
					throw new ArgumentOutOfRangeException("_httpPort", LanguageResource.ArgumentOutOfRangeException_InvalidHttpPortRange);

				_httpPort = value;
			}
		}

		/// <summary>
		/// Gets or sets the name of the symmetric encryption algorithm.
		/// </summary>
		public string Algorithm
		{
			get { return _algorithm; }
			set { _algorithm = value; }
		}

		/// <summary>
		/// Gets or sets, if OEAP padding should be activated.
		/// </summary>
		public bool Oeap
		{
			get { return _oaep; }
			set { _oaep = value; }
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>        
		public HttpCustomServerProtocolSetup()
			: base((settings, clientSinkChain, serverSinkChain) => new HttpChannel(settings, clientSinkChain, serverSinkChain))
		{
			_channelName = "HttpCustomServerProtocolSetup_" + Guid.NewGuid().ToString();

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider());
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider() { TypeFilterLevel = TypeFilterLevel.Full });
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="httpPort">HTTP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		public HttpCustomServerProtocolSetup(int httpPort, IAuthenticationProvider authProvider)
			: this()
		{
			HttpPort = httpPort;
			AuthenticationProvider = authProvider;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="httpPort">HTTP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public HttpCustomServerProtocolSetup(int httpPort, IAuthenticationProvider authProvider, bool encryption)
			: this()
		{
			HttpPort = httpPort;
			AuthenticationProvider = authProvider;
			_encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="httpPort">HTTP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public HttpCustomServerProtocolSetup(int httpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm)
			: this()
		{
			HttpPort = httpPort;
			AuthenticationProvider = authProvider;
			_encryption = encryption;
			_algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="httpPort">HTTP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>        
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public HttpCustomServerProtocolSetup(int httpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep)
			: this()
		{
			HttpPort = httpPort;
			AuthenticationProvider = authProvider;
			_encryption = encryption;
			_algorithm = algorithm;
			_oaep = oaep;
		}

		private bool _encryptionConfigured = false;

		/// <summary>
		/// Configures encrpytion sinks, if encryption is enabled.
		/// </summary>
		private void ConfigureEncryption()
		{
			if (_encryption)
			{
				if (_encryptionConfigured)
					return;

				_encryptionConfigured = true;

				this.AddClientSinkAfterFormatter(new CryptoClientChannelSinkProvider()
				{
					Algorithm = _algorithm,
					Oaep = _oaep
				});
				this.AddServerSinkBeforeFormatter(new CryptoServerChannelSinkProvider()
				{
					Algorithm = _algorithm,
					RequireCryptoClient = true,
					Oaep = _oaep
				});
			}
		}

		/// <summary>
		/// Creates and configures a Remoting channel.
		/// </summary>
		/// <returns>Remoting channel</returns>
		public override IChannel CreateChannel()
		{
			IChannel channel = ChannelServices.GetChannel(_channelName);

			if (channel == null)
			{
				_channelSettings["name"] = _channelName;
				_channelSettings["port"] = _httpPort;
				_channelSettings["secure"] = false;

				ConfigureEncryption();

				if (_channelFactory == null)
					throw new ApplicationException(LanguageResource.ApplicationException_NoChannelFactorySpecified);

				channel = _channelFactory(_channelSettings, BuildClientSinkChain(), BuildServerSinkChain());

				if (!MonoCheck.IsRunningOnMono)
				{
					if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
						RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
				}
				return channel;
			}
			return channel;
		}

		/// <summary>
		/// Gets or sets the authentication provider.
		/// </summary>
		public override IAuthenticationProvider AuthenticationProvider
		{
			get
			{
				return _authProvider;
			}
			set
			{
				if (value == null)
					_authProvider = new NullAuthenticationProvider();
				else
					_authProvider = value;
			}
		}
	}
}
