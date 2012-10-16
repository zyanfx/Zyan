/*
 THIS CODE IS BASED ON:
 -------------------------------------------------------------------------------------------------------------- 
 Remoting Compression Channel Sink

 November, 12, 2008 - Initial revision.
 Alexander Schmidt - http://www.alexschmidt.net

 Originally published at CodeProject:
 http://www.codeproject.com/KB/IP/remotingcompression.aspx

 Copyright © 2008 Alexander Schmidt. All Rights Reserved.
 Distributed under the terms of The Code Project Open License (CPOL).
 --------------------------------------------------------------------------------------------------------------
*/
using System;
using System.Collections;
using System.Runtime.Remoting.Channels;
using Zyan.Communication.Toolbox.Compression;

namespace Zyan.Communication.ChannelSinks.Compression
{
	/// <summary>
	/// Client channel compression sink provider. Creates <see cref="CompressionClientChannelSink"/> for the client sink chain.
	/// </summary>
	public class CompressionClientChannelSinkProvider : IClientChannelSinkProvider
	{
		// The next sink provider in the sink provider chain.
		private IClientChannelSinkProvider _next = null;

		// The compression threshold.
		private readonly int _compressionThreshold;

		// The compression method.
		private readonly CompressionMethod _compressionMethod;

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressionClientChannelSinkProvider" /> class.
		/// </summary>
		public CompressionClientChannelSinkProvider()
			: this(CompressionHelper.CompressionThreshold, CompressionMethod.Default)
		{ 
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressionClientChannelSinkProvider"/> class.
		/// </summary>
		/// <param name="compressionThreshold">The compression threshold.</param>
		/// <param name="compressionMethod">The compression method.</param>
		public CompressionClientChannelSinkProvider(int compressionThreshold, CompressionMethod compressionMethod)
		{
			_compressionThreshold = compressionThreshold;
			_compressionMethod = compressionMethod;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressionClientChannelSinkProvider"/> class.
		/// </summary>
		/// <param name="properties">Sink properties.</param>
		/// <param name="contextData">The context data (ignored).</param>
		public CompressionClientChannelSinkProvider(IDictionary properties, ICollection contextData)
		{
			// read in web.config parameters
			foreach (DictionaryEntry entry in properties)
			{
				switch ((string)entry.Key)
				{
					case "compressionThreshold":
						_compressionThreshold = Convert.ToInt32((string)entry.Value);
						break;

					case "compressionMethod":
						_compressionMethod = (CompressionMethod)Enum.Parse(typeof(CompressionMethod), (string)entry.Value);
						break;

					default:
						throw new ArgumentException("Invalid configuration entry: " + (String)entry.Key);
				}
			}
		}

		/// <summary>
		/// Creates a sink chain.
		/// </summary>
		/// <param name="channel">Channel for which the current sink chain is being constructed.</param>
		/// <param name="url">The URL of the object to connect to. This parameter can be null if the connection is based entirely on the information contained in the <paramref name="remoteChannelData"/> parameter.</param>
		/// <param name="remoteChannelData">A channel data object that describes a channel on the remote server.</param>
		/// <returns>
		/// The first sink of the newly formed channel sink chain, or null, which indicates that this provider will not or cannot provide a connection for this endpoint.
		/// </returns>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
		{
			IClientChannelSink nextSink = null;

			if (_next != null)
			{
				// Call CreateSink on the next sink provider in the chain.  This will return
				// to us the actual next sink object.  If the next sink is null, uh oh!
				if ((nextSink = _next.CreateSink(channel, url, remoteChannelData)) == null) return null;
			}

			// Create this sink, passing to it the previous sink in the chain so that it knows
			// to whom messages should be passed.
			return new CompressionClientChannelSink(nextSink, _compressionThreshold, _compressionMethod);
		}

		/// <summary>
		/// Gets or sets the next sink provider in the channel sink provider chain.
		/// </summary>
		public IClientChannelSinkProvider Next
		{
			get { return _next; }
			set { _next = value; }
		}
	}
}
