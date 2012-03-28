using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Toolbox;
using System.Collections;
using System.Net;

namespace Zyan.Communication.Protocols.Http
{
	/// <summary>
	/// Client protocol setup for HTTP communication with support for user defined authentication and security.
	/// </summary>
	public class HttpCustomClientProtocolSetup : ClientProtocolSetup
	{
		private bool _encryption = true;
		private string _algorithm = "3DES";
		private bool _oaep = false;
		private int _maxAttempts = 2;

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		public HttpCustomClientProtocolSetup()
			: this(Versioning.Strict)
		{ }

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(IWebProxy webProxy)
			: this(Versioning.Strict, webProxy)
		{ }

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		public HttpCustomClientProtocolSetup(Versioning versioning)
			: this(versioning, WebRequest.DefaultWebProxy)
		{ }

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, IWebProxy webProxy)
			: base((settings, clientSinkChain, serverSinkChain) => new HttpChannel(settings, clientSinkChain, serverSinkChain))
		{
			_channelName = "HttpCustomClientProtocolSetup" + Guid.NewGuid().ToString();
			_versioning = versioning;

			Hashtable formatterSettings = new Hashtable();
			formatterSettings.Add("includeVersions", _versioning == Versioning.Strict);
			formatterSettings.Add("strictBinding", _versioning == Versioning.Strict);

			WebRequest.DefaultWebProxy = webProxy;

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider(formatterSettings, null));
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider(formatterSettings, null) { TypeFilterLevel = TypeFilterLevel.Full });
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public HttpCustomClientProtocolSetup(bool encryption)
			: this()
		{
			_encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(bool encryption, IWebProxy webProxy)
			: this(webProxy)
		{
			_encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption)
			: this(versioning)
		{
			_encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption, IWebProxy webProxy)
			: this(versioning, webProxy)
		{
			_encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public HttpCustomClientProtocolSetup(bool encryption, string algorithm)
			: this()
		{
			_encryption = encryption;
			_algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(bool encryption, string algorithm, IWebProxy webProxy)
			: this(webProxy)
		{
			_encryption = encryption;
			_algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, IWebProxy webProxy)
			: this(versioning, webProxy)
		{
			_encryption = encryption;
			_algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm)
			: this(versioning)
		{
			_encryption = encryption;
			_algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		public HttpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts)
			: this()
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_maxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, IWebProxy webProxy)
			: this(webProxy)
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_maxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts)
			: this(versioning)
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_maxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts, IWebProxy webProxy)
			: this(versioning, webProxy)
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_maxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public HttpCustomClientProtocolSetup(bool encryption, string algorithm, bool oaep)
			: this()
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(bool encryption, string algorithm, bool oaep, IWebProxy webProxy)
			: this(webProxy)
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, bool oaep)
			: this(versioning)
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, bool oaep, IWebProxy webProxy)
			: this(versioning, webProxy)
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public HttpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, bool oaep)
			: this()
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_maxAttempts = maxAttempts;
			_oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, bool oaep, IWebProxy webProxy)
			: this(webProxy)
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_maxAttempts = maxAttempts;
			_oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts, bool oaep)
			: this(versioning)
		{
			_encryption = encryption;
			_algorithm = algorithm;
			_maxAttempts = maxAttempts;
			_oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts, bool oaep, IWebProxy webProxy)
			: this(versioning, webProxy)
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
