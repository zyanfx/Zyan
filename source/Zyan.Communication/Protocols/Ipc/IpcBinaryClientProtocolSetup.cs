using System;
using System.Collections;
using System.Net.Security;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols.Ipc
{
	/// <summary>
	/// Client protocol setup for inter process communication via Named Pipes.
	/// </summary>
	public sealed class IpcBinaryClientProtocolSetup : ClientProtocolSetup, IClientProtocolSetup
	{
		private bool _useWindowsSecurity = false;
		private TokenImpersonationLevel _impersonationLevel = TokenImpersonationLevel.Identification;
		private ProtectionLevel _protectionLevel = ProtectionLevel.EncryptAndSign;
		private bool _exclusiveAddressUse = false;
		private string _authorizedGroup = WindowsSecurityTools.EveryoneGroupName;

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
		/// Gets or sets a value indicating whether the channel uses its address exclusively.
		/// </summary>
		public bool ExclusiveAddressUse
		{
			get { return _exclusiveAddressUse; }
			set { _exclusiveAddressUse = value; }
		}

		/// <summary>
		/// Gets or sets the name of the user group authorized to use this channel.
		/// </summary>
		public string AuthorizedGroup
		{
			get { return _authorizedGroup; }
			set { _authorizedGroup = value; }
		}

		/// <summary>
		/// Creates a new instance of the IpcBinaryClientProtocolSetup class.
		/// </summary>
		public IpcBinaryClientProtocolSetup()
			: this(Versioning.Strict)
		{ }

		/// <summary>
		/// Creates a new instance of the IpcBinaryClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		public IpcBinaryClientProtocolSetup(Versioning versioning)
			: base((settings, clientSinkChain, serverSinkChain) => new IpcChannel(settings, clientSinkChain, serverSinkChain))
		{
			_channelName = "IpcBinaryClientProtocol_" + Guid.NewGuid().ToString();
			_versioning = versioning;

			Hashtable formatterSettings = new Hashtable();
			formatterSettings.Add("includeVersions", _versioning == Versioning.Strict);
			formatterSettings.Add("strictBinding", _versioning == Versioning.Strict);

			ClientSinkChain.Add(new BinaryClientFormatterSinkProvider(formatterSettings, null));
			ServerSinkChain.Add(new BinaryServerFormatterSinkProvider(formatterSettings, null) { TypeFilterLevel = TypeFilterLevel.Full });
		}

		/// <summary>
		/// Formats the connection URL for this protocol.
		/// </summary>
		/// <param name="portName">The port name (valid filename required).</param>
		/// <param name="zyanHostName">Name of the zyan host.</param>
		/// <returns>
		/// Formatted URL supported by the protocol.
		/// </returns>
		public string FormatUrl(string portName, string zyanHostName)
		{
			return (this as IClientProtocolSetup).FormatUrl(portName, zyanHostName);
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
			if (parts == null || parts.Length < 2)
				throw new ArgumentException(GetType().Name + " requires two arguments for URL: port name and ZyanHost name.");

			return string.Format("ipc://{0}/{1}", parts);
		}

		static readonly Regex UrlRegex = new Regex(@"^ipc://([^/]+)/(.+)", RegexOptions.Compiled);

		/// <summary>
		/// Checks whether the given URL is valid for this protocol.
		/// </summary>
		/// <param name="url">The URL to check.</param>
		/// <returns>
		/// True, if the URL is supported by the protocol, otherwise, False.
		/// </returns>
		public override bool IsUrlValid(string url)
		{
			if (string.IsNullOrEmpty(url))
				return false;

			return UrlRegex.IsMatch(url);
		}

		/// <summary>
		/// Creates and configures a Remoting channel.
		/// </summary>
		/// <returns>Remoting channel</returns>
		public override IChannel CreateChannel()
		{
			var channel = ChannelServices.GetChannel(_channelName);
			if (channel == null)
			{
				_channelSettings["name"] = _channelName;
				_channelSettings["portName"] = "zyan_" + Guid.NewGuid().ToString();
				if (!_channelSettings.ContainsKey("authorizedGroup"))
				{
					_channelSettings["authorizedGroup"] = AuthorizedGroup;
				}

				if (!_channelSettings.ContainsKey("exclusiveAddressUse"))
				{
					_channelSettings["exclusiveAddressUse"] = ExclusiveAddressUse;
				}

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
