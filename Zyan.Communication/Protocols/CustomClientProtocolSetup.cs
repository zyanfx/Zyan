using System;
using System.Collections;
using Zyan.Communication.ChannelSinks.Compression;
using Zyan.Communication.ChannelSinks.Encryption;
using Zyan.Communication.Transport;

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
		private int _compressionThreshold = 1 << 16;
		private CompressionMethod _compressionMethod = CompressionMethod.Default;

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
        public CustomClientProtocolSetup(Func<IDictionary, IClientTransportAdapter> channelFactory)
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

		/// <summary>
		/// Gets or sets the compression threshold.
		/// </summary>
		public int CompressionThreshold
		{
			get { return _compressionThreshold; }
			set { _compressionThreshold = value; }
		}

		/// <summary>
		/// Gets or sets the compression method.
		/// </summary>
		public CompressionMethod CompressionMethod
		{
			get { return _compressionMethod; }
			set { _compressionMethod = value; }
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

                //TODO: Implement encryption as pipeline stages first.
                //this.AddClientSinkAfterFormatter(new CryptoClientChannelSinkProvider()
                //{
                //    Algorithm = _algorithm,
                //    MaxAttempts = _maxAttempts,
                //    Oaep = _oaep
                //});

                //this.AddServerSinkBeforeFormatter(new CryptoServerChannelSinkProvider()
                //{
                //    Algorithm = _algorithm,
                //    RequireCryptoClient = true,
                //    Oaep = _oaep
                //});
			}
		}

		private bool _compressionConfigured = false;

		/// <summary>
		/// Configures the compression sinks.
		/// </summary>
		protected void ConfigureCompression()
		{
			if (_compressionConfigured)
			{
				return;
			}

			_compressionConfigured = true;

            //TODO: Implement compression as pipeline stages first.
            //this.AddClientSinkAfterFormatter(new CompressionClientChannelSinkProvider(_compressionThreshold, _compressionMethod));
            //this.AddServerSinkBeforeFormatter(new CompressionServerChannelSinkProvider(_compressionThreshold, _compressionMethod));
		}
	}
}
