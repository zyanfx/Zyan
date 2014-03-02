using System;
using System.Collections;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.ChannelSinks.Encryption
{
	/// <summary>
	/// Provider of client-side channel sink for encrypted transmission.
	/// </summary>
	public class CryptoClientChannelSinkProvider : IClientChannelSinkProvider
	{
		// Next sink provider
		private IClientChannelSinkProvider _next = null;

		// Name of the symmetrical encryption algorithm
		private string _algorithm = "3DES";

		// OAEP padding switch
		private bool _oaep = false;

		// Number of attempts
		private int _maxAttempts = 2;

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
		/// Gets or sets the maximum number of attempts when trying to establish a encrypted conection.
		/// </summary>
		public int MaxAttempts
		{
			get { return _maxAttempts; }
			set { _maxAttempts = value; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoClientChannelSinkProvider"/> class.
		/// </summary>
		public CryptoClientChannelSinkProvider()
		{
			// Use default settings
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CryptoClientChannelSinkProvider"/> class.
		/// </summary>
		/// <param name="properties">Configuration settings (taken, for example, from App.config).</param>
		/// <param name="providerData">Optional provider data.</param>
		public CryptoClientChannelSinkProvider(IDictionary properties, ICollection providerData)
		{
			foreach (DictionaryEntry entry in properties)
			{
				switch ((String)entry.Key)
				{
					case "algorithm": // Encryption algorithm name
						_algorithm = (string)entry.Value;
						break;

					case "oaep": // OAEP padding switch
						_oaep = bool.Parse((string)entry.Value);
						break;

					case "maxRetries": // Number of attempts
						_maxAttempts = Convert.ToInt32((string)entry.Value);

						if (_maxAttempts < 1)
							throw new ArgumentException(LanguageResource.ArgumentException_MaxAttempts, "maxAttempts");

						// Increase by one because the first attempt also counts
						_maxAttempts++;
						break;

					default:
						throw new ArgumentException(string.Format(LanguageResource.ArgumentException_InvalidConfigSetting, (String)entry.Key));
				}
			}
		}

		/// <summary>
		/// Creates a sink chain.
		/// </summary>
		/// <param name="channel">Channel for which the current sink chain is being constructed.</param>
		/// <param name="url">The URL of the object to connect to. This parameter can be null if the connection is based entirely on the information contained in the <paramref name="remoteChannelData" /> parameter.</param>
		/// <param name="remoteChannelData">A channel data object that describes a channel on the remote server.</param>
		/// <returns>
		/// The first sink of the newly formed channel sink chain, or null, which indicates that this provider will not or cannot provide a connection for this endpoint.
		/// </returns>
		public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
		{
			IClientChannelSink nextSink = null;

			if (_next != null)
			{
				nextSink = _next.CreateSink(channel, url, remoteChannelData);

				if (nextSink == null)
					return null;
			}

			return new CryptoClientChannelSink(nextSink, _algorithm, _oaep, _maxAttempts);
		}

		/// <summary>
		/// Gets or sets the next sink provider in the channel sink provider chain.
		/// </summary>
		/// <returns>The next sink provider in the channel sink provider chain.</returns>
		public IClientChannelSinkProvider Next
		{
			get { return _next; }
			set { _next = value; }
		}
	}
}
