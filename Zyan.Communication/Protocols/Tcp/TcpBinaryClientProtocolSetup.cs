using System;
using System.Net.Security;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Security.Principal;
using Zyan.Communication.Toolbox;
using System.Collections;

namespace Zyan.Communication.Protocols.Tcp
{
	/// <summary>
	/// Client protocol setup for TCP communication with support for Windows authentication and security.
	/// </summary>
	public class TcpBinaryClientProtocolSetup : ClientProtocolSetup
	{
		private bool _useWindowsSecurity = false;
		private TokenImpersonationLevel _impersonationLevel = TokenImpersonationLevel.Identification;
		private ProtectionLevel _protectionLevel = ProtectionLevel.EncryptAndSign;

		/// <summary>
		/// Gets or sets, if Windows Security should be used.
		/// </summary>
		public bool UseWindowsSecurity
		{
			get { return _useWindowsSecurity; }
			set { _useWindowsSecurity = value; }
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
		/// Gets or sets, if sockets should be cached and reused.
		/// <remarks>
		/// Caching sockets may reduce ressource consumption but may cause trouble in Network Load Balancing clusters.
		/// </remarks>
		/// </summary>
		public bool SocketCachingEnabled { get; set; }

		/// <summary>
		/// Creates a new instance of the TcpBinaryClientProtocolSetup class.
		/// </summary>
		public TcpBinaryClientProtocolSetup()
			: this(Versioning.Strict)
		{ }

		/// <summary>
		/// Creates a new instance of the TcpBinaryClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavoir</param>
		public TcpBinaryClientProtocolSetup(Versioning versioning)
			: base((settings, clientSinkChain, serverSinkChain) => new TcpChannel(settings, clientSinkChain, serverSinkChain))
		{
			SocketCachingEnabled = true;
			_channelName = "TcpBinaryClientProtocol_" + Guid.NewGuid().ToString();
			_versioning = versioning;

			Hashtable formatterSettings = new Hashtable();
			formatterSettings.Add("includeVersions", _versioning == Versioning.Strict);
			formatterSettings.Add("strictBinding", _versioning == Versioning.Strict);

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider(formatterSettings, null));
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider(formatterSettings, null) { TypeFilterLevel = TypeFilterLevel.Full });
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

				if (!MonoCheck.IsRunningOnMono)
				{
					if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
						RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
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
