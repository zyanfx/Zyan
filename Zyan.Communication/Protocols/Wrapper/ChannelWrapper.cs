using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.Protocols.Wrapper
{
	/// <summary>
	/// Wraps up IChannel instance to implement custom URI processing.
	/// </summary>
	internal class ChannelWrapper : IChannel, IChannelSender, IChannelReceiver, IDisposable
	{
		/// <summary>
		/// Creates channel wrapper aroung the given remoting channel.
		/// </summary>
		/// <param name="innerChannel">Inner remoting channel.</param>
		/// <returns><see cref="ChannelWrapper"/> supporting randomized urls.</returns>
		public static IChannel WrapChannel(IChannel innerChannel)
		{
			return new ChannelWrapper(innerChannel);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelWrapper"/> class.
		/// </summary>
		/// <param name="innerChannel">Inner remoting channel.</param>
		private ChannelWrapper(IChannel innerChannel)
		{
			InnerChannel = innerChannel;
		}

		private const char AnchorSymbol = '#';

		private const string UrlFormat = "wrap://{0}#{1}";

		private static int counter = 0;

		/// <summary>
		/// Adds random portion to the given url.
		/// </summary>
		/// <param name="originalUrl">Remoting object url.</param>
		/// <returns>Randomized url.</returns>
		public static string RandomizeUrl(string originalUrl)
		{
			var num = Interlocked.Increment(ref counter);
			return string.Format(UrlFormat, num, originalUrl);
		}

		/// <summary>
		/// Strips random portion from the given url.
		/// </summary>
		/// <param name="url">Randomized url.</param>
		/// <returns>Original url.</returns>
		public static string NormalizeUrl(string url)
		{
			if (string.IsNullOrEmpty(url) || !url.Contains(AnchorSymbol))
			{
				return url;
			}

			return url.Substring(url.IndexOf(AnchorSymbol) + 1).Trim();
		}

		public IChannel InnerChannel { get; private set; }

		private IChannelSender InnerChannelSender { get { return (IChannelSender)InnerChannel; } }

		private IChannelReceiver InnerChannelReceiver { get { return (IChannelReceiver)InnerChannel; } }

		public string ChannelName
		{
			get { return InnerChannel.ChannelName; }
		}

		public int ChannelPriority
		{
			get { return InnerChannel.ChannelPriority; }
		}

		public string Parse(string url, out string objectURI)
		{
			return InnerChannel.Parse(NormalizeUrl(url), out objectURI);
		}

		public IMessageSink CreateMessageSink(string url, object remoteChannelData, out string objectURI)
		{
			var innerSink = InnerChannelSender.CreateMessageSink(NormalizeUrl(url), remoteChannelData, out objectURI);
			return new MessageSinkWrapper(innerSink);
		}

		public object ChannelData
		{
			get { return InnerChannelReceiver.ChannelData; }
		}

		public string[] GetUrlsForUri(string objectURI)
		{
			// do not wrap urls on server-side
			return InnerChannelReceiver.GetUrlsForUri(objectURI);
		}

		public void StartListening(object data)
		{
			InnerChannelReceiver.StartListening(data);
		}

		public void StopListening(object data)
		{
			InnerChannelReceiver.StopListening(data);
		}

		public void Dispose()
		{
			try
			{
				var disposable = InnerChannel as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Unexpected exception of type {0} while disposing ChannelWrapper: {1}", ex.GetType(), ex.Message);
			}
		}
	}
}
