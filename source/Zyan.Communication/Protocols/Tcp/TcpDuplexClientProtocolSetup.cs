using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.Protocols.Tcp.DuplexChannel;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols.Tcp
{
	/// <summary>
	/// Client protocol setup for bi-directional TCP communication with support for user defined authentication and security.
	/// </summary>
	public class TcpDuplexClientProtocolSetup : CustomClientProtocolSetup, IClientProtocolSetup
	{
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
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning)
			: base((settings, clientSinkChain, serverSinkChain) => new TcpExChannel(settings, clientSinkChain, serverSinkChain))
		{
			_channelName = "TcpDuplexClientProtocolSetup" + Guid.NewGuid().ToString();
			_versioning = versioning;

			Hashtable formatterSettings = new Hashtable();
			formatterSettings.Add("includeVersions", _versioning == Versioning.Strict);
			formatterSettings.Add("strictBinding", _versioning == Versioning.Strict);

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider(formatterSettings, null));
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider(formatterSettings, null) { TypeFilterLevel = TypeFilterLevel.Full });
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		public TcpDuplexClientProtocolSetup()
			: this(Versioning.Strict)
		{
			_channelName = "TcpDuplexClientProtocolSetup" + Guid.NewGuid().ToString();
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpDuplexClientProtocolSetup(bool encryption)
			: this()
		{
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption)
			: this(versioning)
		{
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(bool encryption, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this()
		{
			Encryption = encryption;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this(versioning)
		{
			Encryption = encryption;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		public TcpDuplexClientProtocolSetup(bool encryption, string algorithm)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption, string algorithm)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(bool encryption, string algorithm, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		public TcpDuplexClientProtocolSetup(bool encryption, string algorithm, int maxAttempts)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public TcpDuplexClientProtocolSetup(bool encryption, string algorithm, bool oaep)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, bool oaep)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(bool encryption, string algorithm, bool oaep, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, bool oaep, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public TcpDuplexClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, bool oaep)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts, bool oaep)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, bool oaep, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
		}

		/// <summary>
		/// Creates a new instance of the TcpDuplexClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Symmetric encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be activated</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		public TcpDuplexClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts, bool oaep, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
			TcpKeepAliveEnabled = keepAlive;
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
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

			return string.Format("tcpex://{0}:{1}/{2}", parts);
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
			return base.IsUrlValid(url) && url.StartsWith("tcpex");
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
				_channelSettings["listen"] = false;
				_channelSettings["typeFilterLevel"] = TypeFilterLevel.Full;
				_channelSettings["keepAliveEnabled"] = _tcpKeepAliveEnabled;
				_channelSettings["keepAliveTime"] = _tcpKeepAliveTime;
				_channelSettings["keepAliveInterval"] = _tcpKeepAliveInterval;

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
