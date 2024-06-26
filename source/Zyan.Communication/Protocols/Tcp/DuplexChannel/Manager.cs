/*
 THIS CODE IS BASED ON:
 --------------------------------------------------------------------------------------------------------------
 TcpEx Remoting Channel
 Version 1.2 - 18 November, 2003
 Richard Mason - r.mason@qut.edu.au
 Originally published at GotDotNet:
 http://www.gotdotnet.com/Community/UserSamples/Details.aspx?SampleGuid=3F46C102-9970-48B1-9225-8758C38905B1
 Copyright � 2003 Richard Mason. All Rights Reserved.
 --------------------------------------------------------------------------------------------------------------
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Runtime.Serialization;
using System.Net.NetworkInformation;
using Zyan.Communication.Toolbox;
using Debug = System.Diagnostics.Debug;
using Trace = Zyan.Communication.Toolbox.Diagnostics.Trace;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	internal class Manager
	{
		#region Uri Utilities

		static readonly Regex regUrl = new Regex("^tcpex://(?<server>[^/]+)/?(?<objectID>.*)", RegexOptions.Compiled);

		public static string Parse(string url, out string objectID)
		{
			Match m = regUrl.Match(url);
			if (m.Success)
			{
				objectID = m.Groups["objectID"].Value;
				return m.Groups["server"].Value;
			}
			else
			{
				objectID = null;
				return null;
			}
		}

		public static string[] GetUrlsForUri(string objectUri, int port, Guid guid)
		{
			if (objectUri == null)
				objectUri = "/";
			else if (objectUri == "" || objectUri[0] != '/')
				objectUri = "/" + objectUri;

			var retVal = new List<string>();
			foreach (var address in GetAddresses(port, guid, true))
			{
				retVal.Add(string.Format("tcpex://{0}{1}", address, objectUri));
			}

			return retVal.ToArray();
		}

		public static IPAddress[] GetAddresses()
		{
			return _addresses.Value.ToArray();
		}

		public static string[] GetAddresses(int port, Guid guid, bool includeGuid)
		{
			var addresses = new List<string>();

			if (guid != Guid.Empty && includeGuid)
				addresses.Add(guid.ToString());

			if (port != 0)
				_addresses.Value.ForEach(addr => addresses.Add(String.Format("{0}:{1}", addr, port)));

			return addresses.Distinct().ToArray();
		}

		private static Lazy<List<IPAddress>> _addresses = new Lazy<List<IPAddress>>(() =>
		{
			// get loopback address
			var addressFamily = DefaultAddressFamily;
			var loopback = addressFamily == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;
			List<IPAddress> addresses;

			try
			{
				// GetAllNetworkInterfaces() may be slow, so execute it once and cache results
				var query =
					from nic in NetworkInterface.GetAllNetworkInterfaces()
					where nic.OperationalStatus == OperationalStatus.Up
					let props = nic.GetIPProperties()
					where props.GatewayAddresses.Any() // has default gateway address
					from ua in GetUnicastAddresses(props)
					where ua.AddressFamily == addressFamily
					select ua;
				addresses = query.ToList();
			}
			catch
			{
 				// GetAllNetworkInterfaces might fail on Linux and will fail on Android due to this bug:
				// https://bugzilla.xamarin.com/show_bug.cgi?id=1969
				addresses = Dns.GetHostAddresses(Dns.GetHostName()).ToList();
			}

			// Mono framework doesn't include loopback address
			if (!addresses.Contains(loopback))
				addresses.Add(loopback);

			return addresses;

		}, true);

		private static IEnumerable<IPAddress> GetUnicastAddresses(IPInterfaceProperties ipProps)
		{
			// straightforward version (may throw exceptions on Mono 2.10.x/Windows)
			if (!MonoCheck.IsRunningOnMono || MonoCheck.IsUnixOS)
			{
				return ipProps.UnicastAddresses.Select(address => address.Address);
			}

			var result = new List<IPAddress>();

			// catch exceptions to work around Mono 2.10.x bug with some virtual network adapter drivers
			// http://bugzilla.xamarin.com/show_bug.cgi?id=1254
			try
			{
				foreach (var address in ipProps.UnicastAddresses)
				{
					try
					{
						result.Add(address.Address);
					}
					catch // NullReferenceException
					{
					}
				}
			}
			catch // NullReferenceException
			{
			}

			return result;
		}

		public static string CreateUrl(Guid guid)
		{
			return string.Format("tcpex://{0}", guid);
		}

		public static IPAddress GetHostByName(string name)
		{
			IPAddress ipAddress=null;

			if (!IPAddress.TryParse(name, out ipAddress))
			{
				IPAddress[] resolvedAddresses = Dns.GetHostEntry(name).AddressList;

				foreach (var ip in resolvedAddresses)
				{
					if (ip.AddressFamily == AddressFamily.InterNetwork)
						return ip;
				}
				throw new ArgumentOutOfRangeException("IPv4 address not found for host " + name);
			}
			return ipAddress;
		}

		#endregion

		#region Listening

		// Key � Guid or string (can be MessageID, LocalChannelID or LocalAddress), Value � AsyncResult
		private static readonly Dictionary<object, AsyncResult> _listeners = new Dictionary<object,AsyncResult>();
		private static object _listenersLockObject=new object();

		public static void StartListening(Connection connection)
		{
			Message.BeginReceive(connection, new AsyncCallback(ReceiveMessage), null);
		}

		public static Socket StartListening(int port, TcpExChannel channel, IPAddress bindToAddress)
		{
			if (bindToAddress == null)
				throw new ArgumentNullException("bindToAddress");

			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listener.Bind(new IPEndPoint(bindToAddress, port));
			listener.Listen(1000);
			listener.BeginAccept(new AsyncCallback(listener_Accept), new object[] {listener, channel});

			return listener;
		}

		/// <summary>
		/// Stops listening of a specified channel.
		/// </summary>
		/// <param name="channel">TcpEx Channel</param>
		/// <param name="listenerAddresses">Addresses the channel is listening</param>
		public static void StopListening(TcpExChannel channel, string[] listenerAddresses)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			// close running connections
			var runningConnections = Connection.GetRunningConnectionsOfChannel(channel);
			if (runningConnections != null)
			{
				while (runningConnections.Count()>0)
				{
					runningConnections.First().Close();
				}
			}

			// remove pending listeners if specified
			lock (_listenersLockObject)
			{
				_listeners.Remove(channel.ChannelID);

				if (listenerAddresses != null)
				{
					foreach (var address in listenerAddresses)
						_listeners.Remove(address);
				}
			}
		}

		static void listener_Accept(IAsyncResult ar)
		{
			object[] state = (object[])ar.AsyncState;
			Socket listener = (Socket)state[0];
			TcpExChannel channel = (TcpExChannel)state[1];
			Socket client = null;
			try
			{
				try
				{
					client = listener.EndAccept(ar);
				}
				catch (SocketException x)
				{
					// connection forcibly closed by the client's host
					Trace.WriteLine("TcpEx.Manager: invalid incoming connection. Got exception: {0}" + x.ToString());
				}

				// Wait for next Client request
				listener.BeginAccept(new AsyncCallback(listener_Accept), new object[] { listener, channel });
			}
			catch (ObjectDisposedException ex)
			{
				// the listener was closed
				Trace.WriteLine("TcpEx.Manager: the listener was closed. Got exception: " + ex.ToString());
				return;
			}

			// ignore invalid incoming connections
			if (client == null)
			{
				return;
			}

			try
			{
				StartListening(Connection.CreateConnection(client, channel, channel.TcpKeepAliveEnabled, channel.TcpKeepAliveTime, channel.TcpKeepAliveInterval, channel.MaxRetries, channel.RetryDelay));
			}
			catch (DuplicateConnectionException)
			{
			}
			catch (IOException ex)
			{
				// Client socket is not responding
				Trace.WriteLine("TcpEx.Manager: client socket is not responding. Got exception: " + ex.ToString());
			}
			catch (SerializationException ex)
			{
				// Client sends bad data
				Trace.WriteLine("TcpEx.Manager: client is sending bad data. Got exception: " + ex.ToString());
			}
			catch (Exception ex)
			{
				// Cannot cleanly connect to the remote party
				// it's probably not TcpEx channel that's trying to connect us
				Trace.WriteLine("TcpEx.Manager: cannot accept the incoming connection. Got exception: " + ex.ToString());
				return;
			}
		}

		// Key � ChannelID or LocalAddress, Value � Queue<Connection+Message>
		static readonly Dictionary<object, Queue<ConnectionAndMessage>> pendingMessages = new Dictionary<object, Queue<ConnectionAndMessage>>();

		private class ConnectionAndMessage
		{
			public Connection Connection { get; set; }

			public Message Message { get; set; }
		}

		internal static void ReceiveMessage(IAsyncResult ar)
		{
			Message m = null;
			Connection connection = null;

			try
			{
				m = Message.EndReceive(out connection, ar);
			}
			catch (MessageException e)
			{
				TriggerException(e);
				return;
			}

			lock (_listenersLockObject)
			{
				if (!_listeners.ContainsKey(m.Guid))
				{
					// New incoming message
					if (_listeners.ContainsKey(connection.LocalChannelID))
					{
						AsyncResult myAr = _listeners[connection.LocalChannelID];
						_listeners.Remove(connection.LocalChannelID);
						myAr.Complete(connection, m);
					}
					else if (connection.LocalAddress != null && _listeners.ContainsKey(connection.LocalAddress))
					{
						AsyncResult myAr = _listeners[connection.LocalAddress];
						_listeners.Remove(connection.LocalAddress);
						myAr.Complete(connection, m);
					}
					else
					{
						// add incoming message to the pending messages
						var key = connection.LocalChannelID;
						Queue<ConnectionAndMessage> queue;
						if (!pendingMessages.TryGetValue(key, out queue))
						{
							queue = new Queue<ConnectionAndMessage>();
							pendingMessages.Add(key, queue);
						}

						queue.Enqueue(new ConnectionAndMessage
						{
							Connection = connection,
							Message = m
						});
					}
				}
				else
				{
					// Response to previous message
					AsyncResult myAr = _listeners[m.Guid];
					_listeners.Remove(m.Guid);
					myAr.Complete(connection, m);
				}
			}
		}

		static void TriggerException(MessageException e)
		{
			lock (_listenersLockObject)
			{
				ArrayList toBeDeleted = new ArrayList();

				foreach (object key in _listeners.Keys)
				{
					AsyncResult myAr = _listeners[key];
					if (myAr != null && myAr.Connection == e.Connection)
					{
						myAr.Complete(e);
						toBeDeleted.Add(key);
					}
				}
				foreach (object o in toBeDeleted)
					_listeners.Remove(o);
			}
			e.Connection.Close();
		}
		#endregion

		#region ReadMessage

		public static IAsyncResult BeginReadMessage(object guidOrServer, Connection connection, AsyncCallback callback, object asyncState)
		{
			lock (_listenersLockObject)
			{
				Debug.Assert(!_listeners.ContainsKey(guidOrServer), "Handler for this guid already registered.");

				var ar = new AsyncResult(connection, callback, asyncState);

				// process pending message
				Queue<ConnectionAndMessage> queue;
				if (pendingMessages.TryGetValue(guidOrServer, out queue))
				{
					if (queue.Count > 0)
					{
						var result = queue.Dequeue();
						ar.Complete(result.Connection, result.Message);
						return ar;
					}
				}

				// or start listening for incoming messages
				_listeners.Add(guidOrServer, ar);
				return ar;
			}
		}

		public static Message EndReadMessage(out Connection connection, IAsyncResult ar)
		{
			AsyncResult myAr = (AsyncResult)ar;
			if (myAr.Failed)
				throw myAr.Exception.PreserveStackTrace();
			connection = myAr.Connection;
			return myAr.Message;
		}

		public static Message EndReadMessage(out Connection connection, out Exception exception, IAsyncResult ar)
		{
			AsyncResult myAr = (AsyncResult)ar;
			exception = myAr.Exception;
			connection = myAr.Connection;
			return myAr.Message;
		}

		public static Message ReadMessage(Connection connection, object guidOrServer)
		{
			IAsyncResult ar = BeginReadMessage(guidOrServer, connection, null, null);
			ar.AsyncWaitHandle.WaitOne();
			return EndReadMessage(out connection, ar);
		}
		#endregion

		#region AsyncResult
		class AsyncResult : IAsyncResult
		{
			object asyncState;
			AsyncCallback callback;
			bool isCompleted = false;
			System.Threading.ManualResetEvent waitHandle;
			Message m;
			Connection connection;
			Exception exception;

			public AsyncResult(Connection connection, AsyncCallback callback, object asyncState)
			{
				this.connection = connection;
				this.callback = callback;
				this.asyncState = asyncState;
			}

			#region Complete
			public void Complete(Connection connection, Message m)
			{
				lock (this)
				{
					if (isCompleted)
						throw new InvalidOperationException("Already complete");
					this.m = m;
					this.connection = connection;

					isCompleted = true;
					if (waitHandle != null)
						waitHandle.Set();
					if (callback != null)
						ThreadPool.QueueUserWorkItem(new WaitCallback(DoCallback), this);
				}
			}

			public void Complete(Exception e)
			{
				lock (this)
				{
					if (isCompleted)
						throw new InvalidOperationException("Already complete");

					exception = e;

					isCompleted = true;
					if (waitHandle != null)
						waitHandle.Set();
					if (callback != null)
						ThreadPool.QueueUserWorkItem(new WaitCallback(DoCallback), this);
				}
			}

			void DoCallback(object o)
			{
				callback(this);
			}
			#endregion

			#region Properties
			public bool Failed
			{
				get
				{
					return exception != null;
				}
			}

			public Exception Exception
			{
				get
				{
					return exception;
				}
			}

			public Message Message
			{
				get
				{
					return m;
				}
			}

			public Connection Connection
			{
				get
				{
					return connection;
				}
			}
			#endregion

			#region Implementation of IAsyncResult
			public object AsyncState
			{
				get
				{
					return asyncState;
				}
			}

			public bool CompletedSynchronously
			{
				get
				{
					return false;
				}
			}

			public System.Threading.WaitHandle AsyncWaitHandle
			{
				get
				{
					lock (this)
					{
						if (waitHandle == null)
							waitHandle = new System.Threading.ManualResetEvent(isCompleted);
						return waitHandle;
					}
				}
			}

			public bool IsCompleted
			{
				get
				{
					return isCompleted;
				}
			}
			#endregion
		}
		#endregion

		#region DefaultAddressFamily

		private static AddressFamily DefaultAddressFamily
		{
			// prefer IPv4 address
			get { return OSSupportsIPv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6; }
		}

		private static bool? osSupportsIPv4;

		/// <summary>
		/// Gets a value indicating whether IPv4 support is available and enabled on the current host.
		/// </summary>
		/// <remarks>
		/// This property is equivalent to Socket.OSSupportsIPv4 (which is not available under Mono and FX3.5).
		/// </remarks>
		public static bool OSSupportsIPv4
		{
			get
			{
				CheckProtocolSupport();
				return osSupportsIPv4.Value;
			}
		}

		private static void CheckProtocolSupport()
		{
			if (osSupportsIPv4 == null)
			{
				try
				{
					using (var tmpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
					{
						osSupportsIPv4 = true;
					}
				}
				catch
				{
					osSupportsIPv4 = false;
				}
			}
		}

		#endregion
	}
}
