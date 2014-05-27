using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Zyan.Communication.Discovery.Metadata;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.Discovery
{
	/// <summary>
	/// Enables automatic <see cref="ZyanComponentHost"/> discovery in local area networks. Requires <see cref="DiscoveryClient"/>.
	/// </summary>
	public class DiscoveryServer : IDisposable
	{
		/// <summary>
		/// The default discovery port.
		/// </summary>
		public const int DefaultDiscoveryPort = 8765;

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscoveryServer"/> class.
		/// </summary>
		/// <param name="responseMetadata">The response metadata.</param>
		/// <param name="port">The port to bind to.</param>
		/// <param name="localAddress">The local address to bind to.</param>
		public DiscoveryServer(DiscoveryMetadata responseMetadata, int port = DefaultDiscoveryPort, IPAddress localAddress = null)
		{
			if (responseMetadata == null)
			{
				throw new ArgumentNullException("responseMetadata", "Discovery response metadata is missing.");
			}

			Port = port;
			LocalAddress = localAddress ?? IPAddress.Any;
			ResponseMetadata = responseMetadata;
			ResponseMetadataPacket = DiscoveryMetadataHelper.Encode(ResponseMetadata);
		}

		/// <summary>
		/// Gets or sets the port.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// Gets or sets the local address.
		/// </summary>
		public IPAddress LocalAddress { get; set; }

		/// <summary>
		/// Gets or sets the response metadata.
		/// </summary>
		public DiscoveryMetadata ResponseMetadata { get; set; }

		private byte[] ResponseMetadataPacket { get; set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="DiscoveryServer"/> is stopped.
		/// </summary>
		public bool Stopped { get; private set; }

		private UdpClient UdpClient { get; set; }

		private object lockObject = new object();

		/// <summary>
		/// Starts listening for the incoming connections.
		/// </summary>
		public void StartListening()
		{
			Stopped = false;

			var serverEndpoint = new IPEndPoint(LocalAddress, Port);
			UdpClient = new UdpClient();
			UdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
			UdpClient.Client.Bind(serverEndpoint);
			UdpClient.BeginReceive(ReceiveCallback, UdpClient);
		}

		/// <summary>
		/// Stops the listening.
		/// </summary>
		public void StopListening()
		{
			Stopped = true;

			if (UdpClient != null)
			{
				UdpClient.Close();
				UdpClient = null;
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			StopListening();
		}

		private void SafePerform(Action udpAction)
		{
			// Suggested here: http://stackoverflow.com/questions/840090/where-is-udpclient-cancelreceive
			try
			{
				lock (lockObject)
				{
					udpAction();
				}
			}
			catch (SocketException ex)
			{
				// connection is closed
				Trace.WriteLine("Connection is closed. Exception: {0}", ex);
			}
			catch (ObjectDisposedException ex)
			{
				// UdpClient is closed
				Trace.WriteLine("Oops! UDP client is closed. Exception: {0}", ex);
			}
		}

		private void ReceiveCallback(IAsyncResult asyncResult)
		{
			SafePerform(() =>
			{
				var udpClient = (UdpClient)asyncResult.AsyncState;
				var clientEndpoint = default(IPEndPoint);
				var requestData = udpClient.EndReceive(asyncResult, ref clientEndpoint);
				if (!Stopped)
				{
					ThreadPool.QueueUserWorkItem(x => HandleRequest(udpClient, clientEndpoint, requestData));
				}
			});

			SafePerform(() =>
			{
				if (!Stopped && UdpClient != null)
				{
					UdpClient.BeginReceive(ReceiveCallback, UdpClient);
				}
			});
		}

		private void HandleRequest(UdpClient udpClient, IPEndPoint clientEndpoint, byte[] requestMetadata)
		{
			// check if request packet is damaged
			var request = DiscoveryMetadataHelper.Decode(requestMetadata);
			if (request == null)
			{
				return;
			}

			// make sure discovery metadata matches the request
			if (!ResponseMetadata.Matches(request) || Stopped)
			{
				return;
			}

			// send a response
			SafePerform(() =>
			{
				udpClient.BeginSend(ResponseMetadataPacket, ResponseMetadataPacket.Length, clientEndpoint, SendCallback, udpClient);
			});
		}

		private void SendCallback(IAsyncResult asyncResult)
		{
			SafePerform(() =>
			{
				var udpClient = (UdpClient)asyncResult.AsyncState;
				udpClient.EndSend(asyncResult);
			});
		}
	}
}
