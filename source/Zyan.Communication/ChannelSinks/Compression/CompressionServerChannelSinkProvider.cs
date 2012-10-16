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
	/// Server channel compression sink provider. Creates <see cref="CompressionServerChannelSink"/> for the server sink chain.
	/// </summary>
	public class CompressionServerChannelSinkProvider : IServerChannelSinkProvider
	{
		// The next sink provider in the sink provider chain.
		private IServerChannelSinkProvider _next = null;

		// The compression threshold.
		private readonly int _compressionThreshold;

		// The compression method.
		private readonly CompressionMethod _compressionMethod;

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressionServerChannelSinkProvider"/> class.
		/// </summary>
		public CompressionServerChannelSinkProvider()
			: this(CompressionHelper.CompressionThreshold, CompressionMethod.Default)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressionServerChannelSinkProvider"/> class.
		/// </summary>
		/// <param name="compressionThreshold">The compression threshold.</param>
		/// <param name="compressionMethod">The compression method.</param>
		public CompressionServerChannelSinkProvider(int compressionThreshold, CompressionMethod compressionMethod)
		{
			_compressionThreshold = compressionThreshold;
			_compressionMethod = compressionMethod;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressionServerChannelSinkProvider"/> class.
		/// </summary>
		/// <param name="properties">Compression sink properties.</param>
		/// <param name="providerData">The provider data (ignored).</param>
		public CompressionServerChannelSinkProvider(IDictionary properties, ICollection providerData)
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
						throw new ArgumentException("Invalid configuration entry: " + (string)entry.Key);
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
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public IServerChannelSink CreateSink(IChannelReceiver channel)
		{
			IServerChannelSink nextSink = null;
			if (_next != null)
			{
				// Call CreateSink on the next sink provider in the chain.  This will return
				// to us the actual next sink object.  If the next sink is null, uh oh!
				if ((nextSink = _next.CreateSink(channel)) == null) return null;
			}

			// Create this sink, passing to it the previous sink in the chain so that it knows
			// to whom messages should be passed.
			return new CompressionServerChannelSink(nextSink, _compressionThreshold, _compressionMethod);
		}

		/// <summary>
		/// Returns the channel data for the channel that the current sink is associated with. Compression sink doesn't have any specific data.
		/// </summary>
		/// <param name="channelData">A <see cref="T:System.Runtime.Remoting.Channels.IChannelDataStore"/> object in which the channel data is to be returned.</param>
		/// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
		public void GetChannelData(IChannelDataStore channelData)
		{
			// Do nothing.  No channel specific data.
		}

		/// <summary>
		/// Gets or sets the next sink provider in the channel sink provider chain.
		/// </summary>
		public IServerChannelSinkProvider Next
		{
			get { return _next; }
			set { _next = value; }
		}
	}
}
