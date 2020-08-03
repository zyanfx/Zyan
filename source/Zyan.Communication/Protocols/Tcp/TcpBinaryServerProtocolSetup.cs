using System;
using System.Collections;
using System.Linq;
using System.Net.Security;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Security.Principal;
using Zyan.Communication.ChannelSinks.ClientAddress;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;
using Zyan.SafeDeserializationHelpers.Channels;

namespace Zyan.Communication.Protocols.Tcp
{
	/// <summary>
	/// Server protocol setup for TCP communication with support for Windows authentication and security.
	/// </summary>
	public sealed class TcpBinaryServerProtocolSetup : ServerProtocolSetup
	{
		private int _tcpPort = 0;
		private string _ipAddress = "0.0.0.0";
		private bool _useWindowsSecurity = false;
		private TokenImpersonationLevel _impersonationLevel = TokenImpersonationLevel.Identification;
		private ProtectionLevel _protectionLevel = ProtectionLevel.EncryptAndSign;

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
		/// Gets or sets the level of impersonation.
		/// </summary>
		public TokenImpersonationLevel ImpersonationLevel
		{
			get { return _impersonationLevel; }
			set { _impersonationLevel = value; }
		}

		/// <summary>
		/// Get or sets the level of protection (sign or encrypt, or both)
		/// </summary>
		public ProtectionLevel ProtectionLevel
		{
			get { return _protectionLevel; }
			set { _protectionLevel = value; }
		}

		/// <summary>
		/// Gets or sets, if Windows Security should be used.
		/// </summary>
		public bool UseWindowsSecurity
		{
			get { return _useWindowsSecurity; }
			set { _useWindowsSecurity = value; }
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
		/// Creates a new instance of the TcpBinaryServerProtocolSetup class.
		/// </summary>
		public TcpBinaryServerProtocolSetup()
			: this(Versioning.Strict)
		{ }

		/// <summary>
		/// Creates a new instance of the TcpBinaryServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		public TcpBinaryServerProtocolSetup(Versioning versioning)
			: base((settings, clientSinkChain, serverSinkChain) => new TcpChannel(settings, clientSinkChain, serverSinkChain))
		{
			SocketCachingEnabled = true;
			_channelName = "TcpBinaryServerProtocolSetup_" + Guid.NewGuid().ToString();
			_versioning = versioning;

			Hashtable formatterSettings = new Hashtable();
			formatterSettings.Add("includeVersions", _versioning == Versioning.Strict);
			formatterSettings.Add("strictBinding", _versioning == Versioning.Strict);

			ClientSinkChain.Add(new SafeBinaryClientFormatterSinkProvider(formatterSettings, null));
			ServerSinkChain.Add(new SafeBinaryServerFormatterSinkProvider(formatterSettings, null) { TypeFilterLevel = TypeFilterLevel.Full });
			ServerSinkChain.Add(new ClientAddressServerChannelSinkProvider());
		}

		/// <summary>
		/// Creates a new instance of the TcpBinaryServerProtocolSetup class.
		/// </summary>
		/// <param name="tcpPort">TCP port number</param>
		public TcpBinaryServerProtocolSetup(int tcpPort)
			: this()
		{
			TcpPort = tcpPort;
		}

		/// <summary>
		/// Creates a new instance of the TcpBinaryServerProtocolSetup class.
		/// </summary>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		public TcpBinaryServerProtocolSetup(string ipAddress, int tcpPort)
			: this()
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
		}

		/// <summary>
		/// Creates a new instance of the TcpBinaryServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="tcpPort">TCP port number</param>
		public TcpBinaryServerProtocolSetup(Versioning versioning, int tcpPort)
			: this(versioning)
		{
			TcpPort = tcpPort;
		}

		/// <summary>
		/// Creates a new instance of the TcpBinaryServerProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="ipAddress">IP address to bind</param>
		/// <param name="tcpPort">TCP port number</param>
		public TcpBinaryServerProtocolSetup(Versioning versioning, string ipAddress, int tcpPort)
			: this(versioning)
		{
			IpAddress = ipAddress;
			TcpPort = tcpPort;
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
				_channelSettings["secure"] = _useWindowsSecurity;
				_channelSettings["socketCacheTimeout"] = 0;
				_channelSettings["socketCachePolicy"] = SocketCachingEnabled ? SocketCachePolicy.Default : SocketCachePolicy.AbsoluteTimeout;

				if (_useWindowsSecurity)
				{
					_channelSettings["tokenImpersonationLevel"] = _impersonationLevel;
					_channelSettings["protectionLevel"] = _protectionLevel;
				}
				if (_channelFactory == null)
					throw new ApplicationException(LanguageResource.ApplicationException_NoChannelFactorySpecified);

				channel = _channelFactory(_channelSettings, BuildClientSinkChain(), BuildServerSinkChain());
				RemotingHelper.ResetCustomErrorsMode();
			}

			return channel;
		}

		/// <summary>
		/// Gets the authentication provider.
		/// </summary>
		public override IAuthenticationProvider AuthenticationProvider
		{
			get
			{
				if (_useWindowsSecurity)
					return new IntegratedWindowsAuthProvider();
				else
					return new NullAuthenticationProvider();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		/// <inheritdoc/>
		public override string GetDiscoverableUrl(string zyanHostName)
		{
			var channel = CreateChannel() as IChannelReceiver;
			if (channel == null)
			{
				return null;
			}

			var url = channel.GetUrlsForUri(zyanHostName).FirstOrDefault();
			return TryReplaceHostName(url, GetIpAddresses().FirstOrDefault()?.ToString());
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
