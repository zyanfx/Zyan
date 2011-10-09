using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.ClientAddress;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Protocols.Tcp.DuplexChannel;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols.Tcp
{
	/// <summary>
	/// Server protocol setup for bi-directional TCP communication with support for user defined authentication and security.
	/// </summary>
	public class TcpDuplexServerProtocolSetup : ServerProtocolSetup
	{
		private bool _encryption = true;
		private string _algorithm = "3DES";
		private bool _oaep = false;
		private int _tcpPort = 0;

		private bool _tcpKeepAliveEnabled = true;
		private ulong _tcpKeepAliveTime = 30000;
		private ulong _tcpKeepAliveInterval = 1000;
		
		/// <summary>
		/// Enables or disables TCP KeepAlive.
		/// </summary>
		public bool TcpKeepAliveEnabled
		{
			get { return _tcpKeepAliveEnabled; }
			set { _tcpKeepAliveEnabled = value; }
		}

		/// <summary>
		/// Gets or sets the TCP KeepAlive time in milliseconds.
		/// </summary>
		public ulong TcpKeepAliveTime
		{
			get { return _tcpKeepAliveTime; }
			set { _tcpKeepAliveTime = value; }
		}

		/// <summary>
		/// Gets or sets the TCP KeepAlive interval in milliseconds
		/// </summary>
		public ulong TcpKeepAliveInterval
		{
			get { return _tcpKeepAliveInterval; }
			set { _tcpKeepAliveInterval = value; }
		}

		/// <summary>
		/// Gets or sets the TCP port to listen for client calls.
		/// </summary>
		public int TcpPort
		{
			get { return _tcpPort; }
			set
			{
				if (_tcpPort < 0 || _tcpPort > 65535)
					throw new ArgumentOutOfRangeException("tcpPort", LanguageResource.ArgumentOutOfRangeException_InvalidTcpPortRange);

				_tcpPort = value;
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
		/// Creates a new instance of the TcpDuplexServerProtocolSetup class.
		/// </summary>
		public TcpDuplexServerProtocolSetup()
			: base((settings, clientSinkChain, serverSinkChain) => new TcpExChannel(settings, clientSinkChain, serverSinkChain))
		{
			_channelName = "TcpDuplexServerProtocolSetup_" + Guid.NewGuid().ToString();

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider());
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider() { TypeFilterLevel = TypeFilterLevel.Full });
			ServerSinkChain.Add(new ClientAddressServerChannelSinkProvider());
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			_encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			_encryption = encryption;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			_encryption = encryption;
			_algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			_encryption = encryption;
			_algorithm = algorithm;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>        
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			_encryption = encryption;
			_algorithm = algorithm;
			_oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>        
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			_encryption = encryption;
			_algorithm = algorithm;
			_oaep = oaep;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
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
				_channelSettings["port"] = _tcpPort;
				_channelSettings["listen"] = true;
				_channelSettings["typeFilterLevel"] = TypeFilterLevel.Full;
				_channelSettings["keepAliveEnabled"] = _tcpKeepAliveEnabled;
				_channelSettings["keepAliveTime"] = _tcpKeepAliveTime;
				_channelSettings["keepAliveInterval"] = _tcpKeepAliveInterval;

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
		/// Gets or sets the Authentication Provider to be used.
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
