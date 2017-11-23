using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.ClientAddress;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;
using System.Collections;

namespace Zyan.Communication.Protocols.Tcp
{
	/// <summary>
	/// Server protocol setup for TCP communication with support for user defined authentication and security.
	/// </summary>
	public sealed class TcpCustomServerProtocolSetup : CustomServerProtocolSetup
	{
		private int _tcpPort = 0;
		private string _ipAddress = "0.0.0.0";

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
		/// Gets or sets the IP Address to listen for client calls.
		/// </summary>
		public string IpAddress
		{
			get { return _ipAddress; }
			set { _ipAddress = value; }
		}

		/// <summary>
		/// Gets or sets, if socket caching is enabled.
		/// </summary>
		public bool SocketCachingEnabled { get; set; }

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		public TcpCustomServerProtocolSetup(Versioning versioning)
			: base((settings, clientSinkChain, serverSinkChain) => new TcpChannel(settings, clientSinkChain, serverSinkChain))
		{
			SocketCachingEnabled = true;
			_channelName = "TcpCustomServerProtocolSetup_" + Guid.NewGuid().ToString();
			_versioning = versioning;

			Hashtable formatterSettings = new Hashtable();
			formatterSettings.Add("includeVersions", _versioning == Versioning.Strict);
			formatterSettings.Add("strictBinding", _versioning == Versioning.Strict);

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider(formatterSettings, null));
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider(formatterSettings, null) { TypeFilterLevel = TypeFilterLevel.Full });
			ServerSinkChain.Add(new ClientAddressServerChannelSinkProvider());
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		public TcpCustomServerProtocolSetup()
			: this(Versioning.Strict)
		{ }

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		public TcpCustomServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		public TcpCustomServerProtocolSetup(string ipAddress, int tcpPort, IAuthenticationProvider authProvider)
			: this()
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		public TcpCustomServerProtocolSetup(Versioning versioning, int tcpPort, IAuthenticationProvider authProvider)
			: this(versioning)
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		public TcpCustomServerProtocolSetup(Versioning versioning, string ipAddress, int tcpPort, IAuthenticationProvider authProvider)
			: this(versioning)
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpCustomServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpCustomServerProtocolSetup(string ipAddress, int tcpPort, IAuthenticationProvider authProvider, bool encryption)
			: this()
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpCustomServerProtocolSetup(Versioning versioning, int tcpPort, IAuthenticationProvider authProvider, bool encryption)
			: this(versioning)
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpCustomServerProtocolSetup(Versioning versioning, string ipAddress, int tcpPort, IAuthenticationProvider authProvider, bool encryption)
			: this(versioning)
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpCustomServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpCustomServerProtocolSetup(string ipAddress, int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm)
			: this()
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpCustomServerProtocolSetup(Versioning versioning, int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm)
			: this(versioning)
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpCustomServerProtocolSetup(Versioning versioning, string ipAddress, int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm)
			: this(versioning)
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomServerProtocolSetup(int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep)
			: this()
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomServerProtocolSetup(string ipAddress, int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep)
			: this()
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomServerProtocolSetup(Versioning versioning, int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep)
			: this(versioning)
		{
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomServerProtocolSetup(Versioning versioning, string ipAddress, int tcpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep)
			: this(versioning)
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
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
				_channelSettings["bindTo"] = _ipAddress;
				_channelSettings["socketCacheTimeout"] = 0;
				_channelSettings["socketCachePolicy"] = SocketCachingEnabled ? SocketCachePolicy.Default : SocketCachePolicy.AbsoluteTimeout;
				_channelSettings["secure"] = false;

				ConfigureEncryption();
				ConfigureCompression();

				if (_channelFactory == null)
					throw new ApplicationException(LanguageResource.ApplicationException_NoChannelFactorySpecified);

				channel = _channelFactory(_channelSettings, BuildClientSinkChain(), BuildServerSinkChain());
				RemotingHelper.ResetCustomErrorsMode();
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

		#region Versioning settings

		private Versioning _versioning = Versioning.Strict;

		/// <summary>
		/// Gets or sets the versioning behavior.
		/// </summary>
		private Versioning Versioning
		{
			get { return _versioning; }
		}

		#endregion
	}
}
