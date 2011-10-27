using System;
using System.Net.Security;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Security.Principal;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;
using System.Collections;

namespace Zyan.Communication.Protocols.Ipc
{
	/// <summary>
	/// Server protocol setup for inter process communication via Named Pipes.
	/// </summary>
	public class IpcBinaryServerProtocolSetup : ServerProtocolSetup
	{
		private string _portName = string.Empty;
		private bool _useWindowsSecurity = false;
		private TokenImpersonationLevel _impersonationLevel = TokenImpersonationLevel.Identification;
		private ProtectionLevel _protectionLevel = ProtectionLevel.EncryptAndSign;

		/// <summary>
		/// Gets or sets the unique IPC port name.
		/// </summary>
		public string PortName
		{
			get { return _portName; }
			set { _portName = value; }
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
        /// Creates a new instance of the IpcBinaryServerProtocolSetup class.
        /// </summary>
        /// <param name="portName">IPC port name</param>
        public IpcBinaryServerProtocolSetup(string portName)
            : this(portName, Versioning.Strict)
        {}

		/// <summary>
		/// Creates a new instance of the IpcBinaryServerProtocolSetup class.
		/// </summary>
		/// <param name="portName">IPC port name</param>
        /// <param name="versioning">Versioning behavior</param>
		public IpcBinaryServerProtocolSetup(string portName, Versioning versioning)
			: base((settings, clientSinkChain, serverSinkChain) => new IpcChannel(settings, clientSinkChain, serverSinkChain))
		{
			_portName = portName;
			_channelName = "IpcBinaryServerProtocolSetup_" + Guid.NewGuid().ToString();
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
				_channelSettings["portName"] = _portName;
				_channelSettings["secure"] = _useWindowsSecurity;

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
				return channel;
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
