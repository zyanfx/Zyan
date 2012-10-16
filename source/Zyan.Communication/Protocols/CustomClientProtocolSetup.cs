using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Toolbox;
using System.Collections;
using System.Net;

namespace Zyan.Communication.Protocols
{
	/// <summary>
	/// Base client protocol setup with support for user-defined authentication, security and traffic compression.
	/// </summary>
	public class CustomClientProtocolSetup : ClientProtocolSetup
	{
		private bool _encryption = true;
		private string _algorithm = "3DES";
		private bool _oaep = false;
		private int _maxAttempts = 2;

		/// <summary>
		/// Creates a new instance of the CustomClientProtocolSetup class.
		/// </summary>
		protected CustomClientProtocolSetup()
			: base()
		{
		}

		/// <summary>
		/// Creates a new instance of the CustomClientProtocolSetup class.
		/// </summary>
		/// <param name="channelFactory">Delegate to channel factory method</param>
		public CustomClientProtocolSetup(Func<IDictionary, IClientChannelSinkProvider, IServerChannelSinkProvider, IChannel> channelFactory)
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
	}
}
