using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDictionary = System.Collections.IDictionary;

namespace Zyan.Communication.Protocols.Null
{
	/// <summary>
	/// Remoting channel for communications inside the same AppDomain.
	/// </summary>
	public class NullChannel : IChannel, IChannelSender, IChannelReceiver
	{
		/// <summary>
		/// Ininitializes a new instance of the <see cref="NullChannel"/> class.
		/// </summary>
		/// <param name="properties">Channel initialization properties.</param>
		/// <param name="clientSinkProvider">The client sink provider.</param>
		/// <param name="serverSinkProvider">The server sink provider.</param>
		public NullChannel(IDictionary properties, IClientChannelSinkProvider clientSinkProvider, IServerChannelSinkProvider serverSinkProvider)
			: this(properties["name"] as string ?? "NullChannel", clientSinkProvider, serverSinkProvider)
		{ 
		}

		/// <summary>
		/// Ininitializes a new instance of the <see cref="NullChannel"/> class.
		/// </summary>
		/// <param name="channelName">Channel name.</param>
		/// <param name="clientSinkProvider">The client sink provider.</param>
		/// <param name="serverSinkProvider">The server sink provider.</param>
		public NullChannel(string channelName = "NullChannel", IClientChannelSinkProvider clientSinkProvider = null, IServerChannelSinkProvider serverSinkProvider = null)
		{
			ChannelName = channelName;
			ChannelDataStore = new ChannelDataStore(new[] { "null://" + channelName });

			ClientSinkProvider = clientSinkProvider = clientSinkProvider ?? new BinaryClientFormatterSinkProvider();
			serverSinkProvider = serverSinkProvider ?? new BinaryServerFormatterSinkProvider { TypeFilterLevel = TypeFilterLevel.Full };

			// add our client sink provider to the end of ClientSinkProvider chain
			while (clientSinkProvider.Next != null)
				clientSinkProvider = clientSinkProvider.Next;
			clientSinkProvider.Next = new NullClientChannelSink.Provider();

			// collect channel data
			var provider = serverSinkProvider;
			while (provider.Next != null)
			{
				provider.GetChannelData(ChannelDataStore);
				provider = provider.Next;
			}

			// create server sink chain
			var nextSink = ChannelServices.CreateServerChannelSinkChain(serverSinkProvider, this);
			ServerSink = new NullServerChannelSink(nextSink);

			// start listening for messages
			StartListening(null);
		}

		private IClientChannelSinkProvider ClientSinkProvider { get; set; }

		private NullServerChannelSink ServerSink { get; set; }

		private IChannelDataStore ChannelDataStore { get; set; }

		// =============== IChannel =========================================

		/// <summary>
		/// Gets the name of the channel.
		/// </summary>
		public string ChannelName { get; private set; }

		/// <summary>
		/// Gets the priority of the channel.
		/// </summary>
		public int ChannelPriority
		{
			get { return 100; }
		}

		/// <summary>
		/// Returns the object URI as an out parameter, and the URI of the current channel as the return value.
		/// </summary>
		/// <param name="url">Complete url.</param>
		/// <param name="objectUri">Object uri part.</param>
		/// <returns>Channel url, if parsing was successful, otherwise, false.</returns>
		public string Parse(string url, out string objectUri)
		{
			return ParseUrl(url, out objectUri);
		}

		private const string ChannelPrefix = "null://";

		private static readonly int ChannelPrefixLength = ChannelPrefix.Length;

		internal static string ParseUrl(string url, out string objectUri)
		{
			if (string.IsNullOrEmpty(url) || !url.StartsWith(ChannelPrefix))
			{
				return objectUri = null;
			}

			try
			{
				var slashIndex = url.IndexOf('/', ChannelPrefixLength);
				objectUri = url.Substring(slashIndex);
				return url.Substring(ChannelPrefixLength, slashIndex - ChannelPrefixLength);
			}
			catch
			{
				return objectUri = null;
			}
		}

		// =============== IChannelReceiver =========================================

		/// <summary>
		/// Gets the channel-specific data.
		/// </summary>
		public object ChannelData
		{
			get { return ChannelDataStore; }
		}

		/// <summary>
		/// Returns an array of all the URLs for a URI.
		/// </summary>
		/// <param name="objectUri">Object uri.</param>
		/// <returns>Array of object urls.</returns>
		public string[] GetUrlsForUri(string objectUri)
		{
			if (!objectUri.StartsWith("/"))
			{
				objectUri = "/" + objectUri;
			}

			var urls = new List<String>();
			foreach (var url in ChannelDataStore.ChannelUris)
			{
				urls.Add(url + objectUri);
			}

			return urls.ToArray();
		}

		private Thread ServerThread { get; set; }

		/// <summary>
		/// Instructs the current channel to start listening for requests.
		/// </summary>
		/// <param name="data">Channel-specific data.</param>
		public void StartListening(object data)
		{
			ServerThread = new Thread(() =>
			{
				ServerSink.Listen(ChannelName);
			});

			ServerThread.IsBackground = true;
			ServerThread.Start();
		}

		/// <summary>
		/// Instructs the current channel to stop listening for requests.
		/// </summary>
		/// <param name="data">Channel-specific data.</param>
		public void StopListening(object data)
		{
			if (ServerThread != null)
			{
				ServerSink.Stopped = true;

				// try to stop gracefully, then abort
				if (!ServerThread.Join(TimeSpan.FromSeconds(1)))
				{
					ServerThread.Abort();
				}

				ServerThread = null;
			}
		}

		// =============== IChannelSender =========================================

		/// <summary>
		/// Returns a channel message sink that delivers messages to the specified URL or channel data object.
		/// </summary>
		/// <param name="url">Object url.</param>
		/// <param name="remoteChannelData">Channel-specific data of the remote channel.</param>
		/// <param name="objectUri">Object uri portion of the given url.</param>
		/// <returns><see cref="IMessageSink"/> instance.</returns>
		public IMessageSink CreateMessageSink(string url, object remoteChannelData, out string objectUri)
		{
			objectUri = null;

			// is wellknown object url specified?
			if (url == null)
			{
				// client-activated object url is specified in the channel data store
				var dataStore = remoteChannelData as IChannelDataStore;
				if (dataStore == null || dataStore.ChannelUris.Length < 1)
				{
					return null;
				}

				url = dataStore.ChannelUris[0];
			}

			// validate url: is there compatible channel listening?
			if (url != null && Parse(url, out objectUri) != null)
			{
				// yes, create client transport sink
				return (IMessageSink)ClientSinkProvider.CreateSink(this, url, remoteChannelData);
			}

			return null;
		}
	}
}
