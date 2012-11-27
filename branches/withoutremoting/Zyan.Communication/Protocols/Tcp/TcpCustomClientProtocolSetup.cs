using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Toolbox;
using System.Collections;

namespace Zyan.Communication.Protocols.Tcp
{
	/// <summary>
	/// Client protocol setup for TCP communication with support for user defined authentication and security.
	/// </summary>
	public class TcpCustomClientProtocolSetup : CustomClientProtocolSetup
	{
		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		public TcpCustomClientProtocolSetup(Versioning versioning)
			: base((settings, clientSinkChain, serverSinkChain) => new TcpChannel(settings, clientSinkChain, serverSinkChain))
		{
			SocketCachingEnabled = true;
			_channelName = "TcpCustomClientProtocolSetup" + Guid.NewGuid().ToString();
			_versioning = versioning;

			Hashtable formatterSettings = new Hashtable();
			formatterSettings.Add("includeVersions", _versioning == Versioning.Strict);
			formatterSettings.Add("strictBinding", _versioning == Versioning.Strict);

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider(formatterSettings, null));
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider(formatterSettings, null) { TypeFilterLevel = TypeFilterLevel.Full });
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		public TcpCustomClientProtocolSetup()
			: this(Versioning.Strict)
		{ }

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpCustomClientProtocolSetup(bool encryption)
			: this()
		{
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption)
			: this(versioning)
		{
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm, bool oaep)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, bool oaep)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, bool oaep)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts, bool oaep)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
		}

		/// <summary>
		/// Gets or sets, if sockets should be cached and reused.
		/// <remarks>
		/// Caching sockets may reduce ressource consumption but may cause trouble in Network Load Balancing clusters.
		/// </remarks>
		/// </summary>
		public bool SocketCachingEnabled { get; set; }

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
				ConfigureCompression();

				if (_channelFactory == null)
					throw new ApplicationException(LanguageResource.ApplicationException_NoChannelFactorySpecified);

				channel = _channelFactory(_channelSettings, BuildClientSinkChain(), BuildServerSinkChain());

				if (!MonoCheck.IsRunningOnMono)
				{
					if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
					{
						RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
					}
				}
			}

			return channel;
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
