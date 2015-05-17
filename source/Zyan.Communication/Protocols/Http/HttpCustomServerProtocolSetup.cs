using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.ClientAddress;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;
using System.Collections;

namespace Zyan.Communication.Protocols.Http
{
	/// <summary>
	/// Server protocol setup für HTTP communication with support for user defined authentication and security.
	/// </summary>
	public sealed class HttpCustomServerProtocolSetup : CustomServerProtocolSetup
	{
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
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>
		public HttpCustomServerProtocolSetup()
			: this(Versioning.Strict)
		{ }

		/// <summary>
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		public HttpCustomServerProtocolSetup(Versioning versioning)
			: base((settings, clientSinkChain, serverSinkChain) => new HttpChannel(settings, clientSinkChain, serverSinkChain))
		{
			_channelName = "HttpCustomServerProtocolSetup_" + Guid.NewGuid().ToString();
			_versioning = versioning;

			Hashtable formatterSettings = new Hashtable();
			formatterSettings.Add("includeVersions", _versioning == Versioning.Strict);
			formatterSettings.Add("strictBinding", _versioning == Versioning.Strict);

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider(formatterSettings, null));
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider(formatterSettings, null) { TypeFilterLevel = TypeFilterLevel.Full });
			ServerSinkChain.Add(new ClientAddressServerChannelSinkProvider());
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
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="httpPort">HTTP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		public HttpCustomServerProtocolSetup(Versioning versioning, int httpPort, IAuthenticationProvider authProvider)
			: this(versioning)
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
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="httpPort">HTTP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public HttpCustomServerProtocolSetup(Versioning versioning, int httpPort, IAuthenticationProvider authProvider, bool encryption)
			: this(versioning)
		{
			HttpPort = httpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
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
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="httpPort">HTTP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public HttpCustomServerProtocolSetup(Versioning versioning, int httpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm)
			: this(versioning)
		{
			HttpPort = httpPort;
			AuthenticationProvider = authProvider;
			Encryption = encryption;
			Algorithm = algorithm;
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
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="httpPort">HTTP port number</param>
		/// <param name="authProvider">Authentication provider</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public HttpCustomServerProtocolSetup(Versioning versioning, int httpPort, IAuthenticationProvider authProvider, bool encryption, string algorithm, bool oaep)
			: this(versioning)
		{
			HttpPort = httpPort;
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
				_channelSettings["port"] = _httpPort;
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
