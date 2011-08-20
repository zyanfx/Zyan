using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols.Tcp
{
	/// <summary>
	/// Client protocol setup for TCP communication with support for user defined authentication and security.
	/// </summary>
	public class TcpCustomClientProtocolSetup : ClientProtocolSetup
	{
		private bool _encryption = true;
		private string _algorithm = "3DES";
		private bool _oaep = false;
		private int _maxAttempts = 2;

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		public TcpCustomClientProtocolSetup()
			: base((settings, clientSinkChain, serverSinkChain) => new TcpChannel(settings, clientSinkChain, serverSinkChain))
		{
			SocketCachingEnabled = true;
			_channelName = "TcpCustomClientProtocolSetup" + Guid.NewGuid().ToString();

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider());
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider() { TypeFilterLevel = TypeFilterLevel.Full });
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpCustomClientProtocolSetup(bool encryption) : this()
		{
			_encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm) : this()
		{
			_encryption = encryption;
			_algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts) : this()
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_maxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm, bool oaep) : this()
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, bool oaep) : this()
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_maxAttempts = maxAttempts;
			_oaep = oaep;
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
		/// Gets or sets the maximum number of attempts when trying to establish a encrypted conection.
		/// </summary>
		public int MaxAttempts
		{
			get { return _maxAttempts; }
			set { _maxAttempts = value; }
		}

		/// <summary>
		/// Gets or sets, if sockets should be cached and reused.
		/// <remarks>
		/// Caching sockets may reduce ressource consumption but may cause trouble in Network Load Balancing clusters.
		/// </remarks>
		/// </summary>
		public bool SocketCachingEnabled
		{ get; set; }

		/// <summary>
		/// Configures encrpytion sinks, if encryption is enabled.
		/// </summary>
		private void ConfigureEncryption()
		{
			if (_encryption)
			{
				this.AddClientSinkAfterFormatter(new CryptoClientChannelSinkProvider()
				{
					Algorithm = _algorithm,
					MaxAttempts = _maxAttempts,
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
				_channelSettings["port"] = 0;
				_channelSettings["socketCacheTimeout"] = 0;
				_channelSettings["socketCachePolicy"] = SocketCachingEnabled ? SocketCachePolicy.Default : SocketCachePolicy.AbsoluteTimeout;
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
			return null;
		}
	}
}
