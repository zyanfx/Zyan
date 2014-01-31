using System;
using System.Collections;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols.Http
{
	/// <summary>
	/// Client protocol setup for HTTP communication with support for user defined authentication and security.
	/// </summary>
	public sealed class HttpCustomClientProtocolSetup : CustomClientProtocolSetup, IClientProtocolSetup
	{
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
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="webProxy">Defines HTTP proxy settings</param>
		public HttpCustomClientProtocolSetup(bool encryption, IWebProxy webProxy)
			: this(webProxy)
		{
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public HttpCustomClientProtocolSetup(Versioning versioning, bool encryption)
			: this(versioning)
		{
			Encryption = encryption;
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
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the HttpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public HttpCustomClientProtocolSetup(bool encryption, string algorithm)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
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
			Encryption = encryption;
			Algorithm = algorithm;
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
			Encryption = encryption;
			Algorithm = algorithm;
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
			Encryption = encryption;
			Algorithm = algorithm;
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
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
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
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
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
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
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
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
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
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
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
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
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
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
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
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
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
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
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
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
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
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
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
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
		}

		/// <summary>
		/// Formats the connection URL for this protocol.
		/// </summary>
		/// <param name="serverAddress">The server address.</param>
		/// <param name="portNumber">The port number.</param>
		/// <param name="zyanHostName">Name of the zyan host.</param>
		/// <returns>
		/// Formatted URL supported by the protocol.
		/// </returns>
		public string FormatUrl(string serverAddress, int portNumber, string zyanHostName)
		{
			return (this as IClientProtocolSetup).FormatUrl(serverAddress, portNumber, zyanHostName);
		}

		/// <summary>
		/// Formats the connection URL for this protocol.
		/// </summary>
		/// <param name="parts">The parts of the url, such as server name, port, etc.</param>
		/// <returns>
		/// Formatted URL supported by the protocol.
		/// </returns>
		string IClientProtocolSetup.FormatUrl(params object[] parts)
		{
			if (parts == null || parts.Length < 3)
				throw new ArgumentException(GetType().Name + " requires three arguments for URL: server address, port number and ZyanHost name.");

			return string.Format("http://{0}:{1}/{2}", parts);
		}

		/// <summary>
		/// Checks whether the given URL is valid for this protocol.
		/// </summary>
		/// <param name="url">The URL to check.</param>
		/// <returns>
		/// True, if the URL is supported by the protocol, otherwise, False.
		/// </returns>
		public override bool IsUrlValid(string url)
		{
			return base.IsUrlValid(url) && url.StartsWith("http");
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
