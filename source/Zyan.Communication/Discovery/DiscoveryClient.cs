using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Zyan.Communication.Discovery.Metadata;
using Zyan.Communication.Toolbox.Diagnostics;
using System.Net.NetworkInformation;

namespace Zyan.Communication.Discovery
{
	/// <summary>
	/// Connects to <see cref="DiscoveryServer"/> to discover available <see cref="ZyanComponentHost"/> instances in local area networks.
	/// </summary>
	public class DiscoveryClient : IDisposable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiscoveryClient"/> class.
		/// </summary>
		/// <param name="requestMetadata">The request metadata.</param>
		/// <param name="port">The port of the discovery server.</param>
		public DiscoveryClient(DiscoveryMetadata requestMetadata, int port = DiscoveryServer.DefaultDiscoveryPort)
		{
			if (requestMetadata == null)
			{
				throw new ArgumentNullException("requestMetadata", "Discovery request metadata is missing.");
			}

			Port = port;
			RequestMetadata = requestMetadata;
			RequestMetadataPacket = DiscoveryMetadataHelper.Encode(RequestMetadata);
			DestinationAddress = IPAddress.Broadcast;
			DestinationEndpoint = new IPEndPoint(DestinationAddress, Port);
			Results = new HashSet<DiscoveryMetadata>();
			RetryTimeout = TimeSpan.FromSeconds(1);
			RetryCount = 10;
		}

		/// <summary>
		/// Gets or sets the request metadata.
		/// </summary>
		public DiscoveryMetadata RequestMetadata { get; set; }

		private byte[] RequestMetadataPacket { get; set; }

		/// <summary>
		/// Gets or sets the port of the discovery server.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// Gets a value indicating whether discovery is stopped.
		/// </summary>
		public bool Stopped { get; private set; }

		private IPAddress DestinationAddress { get; set; }

		private IPEndPoint DestinationEndpoint { get; set; }

		private List<UdpClient> UdpSendClients { get; set; } = new List<UdpClient>();

		private TimeSpan RetryTimeout { get; set; }

		private int RetryCount { get; set; }

		private int Counter { get; set; }

		private Timer RetryTimer { get; set; }

		private HashSet<DiscoveryMetadata> Results { get; set; }

		private object lockObject = new object();

		private bool isRunning = false;

		/// <summary>
		/// Occurs when <see cref="DiscoveryServer"/> response is acquired.
		/// </summary>
		public event EventHandler<DiscoveryEventArgs> Discovered;

		/// <summary>
		/// Raises the <see cref="E:Discovered" /> event.
		/// </summary>
		/// <param name="e">The <see cref="DiscoveryEventArgs"/> instance containing the event data.</param>
		protected virtual void OnDiscovered(DiscoveryEventArgs e)
		{
			var handler = Discovered;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		/// <summary>
		/// Starts the discovery.
		/// </summary>
		public void StartDiscovery()
		{
			SafePerform(() =>
			{
				Stopped = false;
				Counter = RetryCount;
				Results.Clear();
				foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
				{
					if (ni.OperationalStatus == OperationalStatus.Up && ni.SupportsMulticast &&
						ni.GetIPProperties().GetIPv4Properties() != null)
					{
						int id = ni.GetIPProperties().GetIPv4Properties().Index;
						if (NetworkInterface.LoopbackInterfaceIndex != id)
						{
							foreach (UnicastIPAddressInformation uip in ni.GetIPProperties().UnicastAddresses)
							{
								if (uip.Address.AddressFamily == AddressFamily.InterNetwork)
								{
									UdpSendClients.Add(new UdpClient(new IPEndPoint(uip.Address, 0)));
								}
							}
						}
					}
				}

				RetryTimer = new Timer(DiscoveryTimerCallback, null, TimeSpan.Zero, RetryTimeout);
			});
		}

		/// <summary>
		/// Stops the discovery.
		/// </summary>
		public void StopDiscovery()
		{
			Stopped = true;
			RetryTimer?.Dispose();
			RetryTimer = null;

			foreach (var udpClient in UdpSendClients)
			{
				udpClient.Close();
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			StopDiscovery();
		}

		private void DiscoveryTimerCallback(object state)
		{
			if (isRunning)
			{
				return;
			}

			isRunning = true;

			if (Counter <= 0)
			{
				StopDiscovery();
				return;
			}

			Counter--;
			foreach (UdpClient client in UdpSendClients)
			{
				if (!Stopped && client != null && Counter > 0)
				{
					SafePerform(() =>
					{
						client.BeginSend(RequestMetadataPacket, RequestMetadataPacket.Length, DestinationEndpoint,
							SendCallback, client);
					});
				}
			}

			isRunning = false;
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

		private void SendCallback(IAsyncResult asyncResult)
		{
			SafePerform(() =>
			{
				var udpClient = (UdpClient)asyncResult.AsyncState;
				udpClient.EndSend(asyncResult);
				if (!Stopped)
				{
					udpClient.BeginReceive(ReceiveCallback, udpClient);
				}
			});
		}

		private void ReceiveCallback(IAsyncResult asyncResult)
		{
			var response = default(DiscoveryMetadata);
			SafePerform(() =>
			{
				var udpClient = (UdpClient)asyncResult.AsyncState;
				var remoteEndpoint = default(IPEndPoint);
				if (!Stopped)
				{
					var responseMetadata = udpClient.EndReceive(asyncResult, ref remoteEndpoint);
					response = DiscoveryMetadataHelper.Decode(responseMetadata);
				}
			});

			if (response != null)
			{
				HandleResponse(response);
			}
		}

		private void HandleResponse(DiscoveryMetadata response)
		{
			lock (lockObject)
			{
				if (response == null || Results.Contains(response))
				{
					return;
				}

				Results.Add(response);
			}

			OnDiscovered(new DiscoveryEventArgs(response));
		}
	}
}