using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Security;
using Zyan.Communication.Toolbox;
using System.Collections;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Base server protocol setup with support for user-defined authentication, security and traffic compression.
	/// </summary>
	public class CustomServerProtocolSetup : ServerProtocolSetup
	{
		private bool _encryption = true;
		private string _algorithm = "3DES";
		private bool _oaep = false;

		/// <summary>
		/// Creates a new instance of the CustomServerProtocolSetup class.
		/// </summary>
		protected CustomServerProtocolSetup()
			: base()
		{
		}

		/// <summary>
		/// Creates a new instance of the CustomServerProtocolSetup class.
		/// </summary>
		/// <param name="channelFactory">Delegate to channel factory method</param>
		public CustomServerProtocolSetup(Func<IDictionary, IClientChannelSinkProvider, IServerChannelSinkProvider, IChannel> channelFactory)
			: base(channelFactory)
		{
		}

		/// <summary>
		/// Gets or sets a value indicating whether the encryption is enabled.
		/// </summary>
		public bool Encryption
		{
			get { return _encryption; }
			set { _encryption = value; }
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
		/// Gets or sets, if OAEP padding should be activated.
		/// </summary>
		public bool Oaep
		{
			get { return _oaep; }
			set { _oaep = value; }
		}

		private bool _encryptionConfigured = false;

		/// <summary>
		/// Configures encrpytion sinks, if encryption is enabled.
		/// </summary>
		protected void ConfigureEncryption()
		{
			if (_encryption)
			{
				if (_encryptionConfigured)
					return;

				_encryptionConfigured = true;

				this.AddClientSinkAfterFormatter(new CryptoClientChannelSinkProvider()
				{
					Algorithm = _algorithm,
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
	}
}
