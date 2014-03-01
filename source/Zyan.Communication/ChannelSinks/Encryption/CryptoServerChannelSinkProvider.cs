using System;
using System.Collections;
using System.Net;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
	/// Provides the server-side channel sink for encrypted communication.
	/// </summary>
	public class CryptoServerChannelSinkProvider : IServerChannelSinkProvider
	{
		// Next sink provider
		private IServerChannelSinkProvider _next = null;

		// Name of the symmetric encryption algorithm to be used
		private string _algorithm = "3DES";

		// Switch for OAEP padding
		private bool _oaep = false;

		// Specifies whether the corresponding encryption channel sink need to be present on the client side
		private bool _requireCryptoClient = false;

		// Lifetime of a client connection, in seconds
		private double _connectionAgeLimit = 60.0;

		// Interval for sweeping old connections, in seconds
		private double _sweepFrequency = 15.0;

		// Client IP exemption list
		private IPAddress[] _securityExemptionList = null;

		/// <summary>
		/// Gets or sets the name of the symmetric encryption algorithm.
		/// </summary>
		public string Algorithm
		{
			get { return _algorithm; }
			set { _algorithm = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether OEAP padding should be activated.
		/// </summary>
		public bool Oaep
		{
			get { return _oaep; }
			set { _oaep = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether crypto client sink is required on the client side.
		/// </summary>
		public bool RequireCryptoClient
		{
			get { return _requireCryptoClient; }
			set { _requireCryptoClient = value; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoServerChannelSinkProvider"/> class.
		/// </summary>
		public CryptoServerChannelSinkProvider()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoServerChannelSinkProvider"/> class.
		/// </summary>
		/// <param name="properties">Configuration properties (from App.config, for example).</param>
		/// <param name="providerData">Optional provider data.</param>
		public CryptoServerChannelSinkProvider(IDictionary properties, ICollection providerData)
		{
			foreach (DictionaryEntry entry in properties)
			{
				switch ((String)entry.Key)
				{
					case "algorithm": // Symmetric encryption algorithm
						_algorithm = (string)entry.Value;
						break;

					case "oaep": // OAEP padding switch
						_oaep = bool.Parse((string)entry.Value);
						break;

					case "connectionAgeLimit": // Maximal connection lifetime
						_connectionAgeLimit = double.Parse((string)entry.Value);

						if (_connectionAgeLimit < 0)
							throw new ArgumentException(LanguageResource.ArgumentException_InvalidConnectionAgeLimitSetting, "_connectionAgeLimit");
						break;

					case "sweepFrequency": // Inactive connection sweeping frequency
						_sweepFrequency = double.Parse((string)entry.Value);

						if (_sweepFrequency < 0)
							throw new ArgumentException(LanguageResource.ArgumentException_InvalidSweepFrequencySetting, "_sweepFrequency");
						break;

					case "requireCryptoClient": // Whether the client-side encryption sink is required
						_requireCryptoClient = bool.Parse((string)entry.Value);
						break;

					case "securityExemptionList": // IP addresses that do not require encryption
						string ipList = (string)entry.Value;
						if (ipList != null && ipList != string.Empty)
						{
							string[] values = ipList.Split(';');
							_securityExemptionList = new IPAddress[values.Length];
							for (int i = 0; i < values.Length; i++) _securityExemptionList[i] = IPAddress.Parse(values[i].Trim());
						}
						break;

					default:
						throw new ArgumentException(string.Format(LanguageResource.ArgumentException_InvalidConfigurationSetting, (String)entry.Key));
				}
			}
		}

		/// <summary>
		/// Creates a sink chain.
		/// </summary>
		/// <param name="channel">The channel for which to create the channel sink chain.</param>
		/// <returns>
		/// The first sink of the newly formed channel sink chain, or null, which indicates that this provider will not or cannot provide a connection for this endpoint.
		/// </returns>
		public IServerChannelSink CreateSink(IChannelReceiver channel)
		{
			IServerChannelSink nextSink = null;

			// If next sink provider is specified, create the next channel sink
			if (_next != null)
			{
				nextSink = _next.CreateSink(channel);

				if (nextSink == null)
					return null;
			}

			// Create channel sink and attach it to the current sink chain
			return new CryptoServerChannelSink(nextSink, _algorithm, _oaep, _connectionAgeLimit,
				_sweepFrequency, _requireCryptoClient, _securityExemptionList);
		}

		/// <summary>
		/// Returns the channel data for the channel that the current sink is associated with.
		/// </summary>
		/// <param name="channelData">A <see cref="T:System.Runtime.Remoting.Channels.IChannelDataStore" /> object in which the channel data is to be returned.</param>
		public void GetChannelData(System.Runtime.Remoting.Channels.IChannelDataStore channelData)
		{
			// we don't use channel data
		}

		/// <summary>
		/// Gets or sets the next sink provider in the channel sink provider chain.
		/// </summary>
		/// <returns>The next sink provider in the channel sink provider chain.</returns>
		public IServerChannelSinkProvider Next
		{
			get { return _next; }
			set { _next = value; }
		}
	}
}
