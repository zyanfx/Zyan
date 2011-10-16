/*
 THIS CODE IS BASED ON:
 -------------------------------------------------------------------------------------------------------------- 
 TcpEx Remoting Channel
 Version 1.2 - 18 November, 2003
 Richard Mason - r.mason@qut.edu.au
 Originally published at GotDotNet:
 http://www.gotdotnet.com/Community/UserSamples/Details.aspx?SampleGuid=3F46C102-9970-48B1-9225-8758C38905B1
 Copyright © 2003 Richard Mason. All Rights Reserved. 
 --------------------------------------------------------------------------------------------------------------
*/
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	internal partial class Manager
	{
		#region Uri Utilities

		static readonly Regex regUrl = new Regex("tcpex://(?<server>[^/]+)/?(?<objectID>.*)", RegexOptions.Compiled);

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

			ArrayList retVal = new ArrayList();

			if (guid != Guid.Empty)
				retVal.Add(string.Format("tcpex://{0}{1}", guid, objectUri));

			string hostname = Dns.GetHostName();
			IPHostEntry hostEntry = Dns.GetHostEntry(hostname);
			if (port != 0)
			{
				foreach (IPAddress address in hostEntry.AddressList)
					retVal.Add(string.Format("tcpex://{0}:{1}{2}", address, port, objectUri));
			}

			return (string[])retVal.ToArray(typeof(string));
		}

		public static string[] GetAddresses(int port, Guid guid)
		{
			var addresses = new List<string>();

			if (guid != Guid.Empty)
				addresses.Add(guid.ToString());

			if (port != 0)
				_addresses.Value.ForEach(addr => addresses.Add(String.Format("{0}:{1}", addr, port)));

			return addresses.Distinct().ToArray();
		}

		private static Lazy<List<string>> _addresses = new Lazy<List<string>>(() =>
		{
			// get loopback address
			var addressFamily = DefaultAddressFamily;
			var loopback = addressFamily == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;

			// GetAllNetworkInterfaces() may be slow, so execute it once and cache results
			var query =
				from nic in NetworkInterface.GetAllNetworkInterfaces()
				from ua in GetUnicastAddresses(nic.GetIPProperties())
				where ua.AddressFamily == addressFamily
				select ua;

			// Mono framework doesn't include loopback address
			var addresses = query.ToList();
			if (!addresses.Contains(loopback))
				addresses.Add(loopback);

			return addresses.Select(a => a.ToString()).ToList();

		}, true);

		private static IEnumerable<IPAddress> GetUnicastAddresses(IPInterfaceProperties ipProps)
		{
			// straightforward version (may throw exceptions on Mono 2.10.x/Windows)
			if (!MonoCheck.IsRunningOnMono || !MonoCheck.NoWindowsOS)
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
		
		// Hashtable<Guid|string, AsyncResult>
		private static readonly Hashtable _listeners = new Hashtable();
		private static object _listenersLockObject=new object();

		public static void StartListening(Connection connection)
		{
			Message.BeginReceive(connection, new AsyncCallback(ReceiveMessage), null);
		}
		
		public static int StartListening(int port, TcpExChannel channel)
		{
			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listener.Bind(new IPEndPoint(IPAddress.Any, port));
			listener.Listen(1000);
			listener.BeginAccept(new AsyncCallback(listener_Accept), new object[] {listener, channel});

			return ((IPEndPoint)listener.LocalEndPoint).Port;
		}

		/// <summary>
		/// Stops listening of a specified channel. 
		/// </summary>
		/// <param name="channel">TcpEx Channel</param>
		public static void StopListening(TcpExChannel channel)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			var runningConnections = Connection.GetRunningConnectionsOfChannel(channel);

			if (runningConnections != null)
			{
				while(runningConnections.Count()>0)
				{
					runningConnections.First().Close();
				}
			}
		}

		static void listener_Accept(IAsyncResult ar)
		{
			object[] state = (object[])ar.AsyncState;
			Socket listener = (Socket)state[0];
			TcpExChannel channel = (TcpExChannel)state[1];
			Socket client = listener.EndAccept(ar);

			try
			{
				StartListening(Connection.CreateConnection(client, channel, channel.TcpKeepAliveEnabled, channel.TcpKeepAliveTime, channel.TcpKeepAliveInterval, channel.MaxRetries, channel.RetryDelay));
			}
			catch (DuplicateConnectionException)
			{
			}
			catch (IOException)
			{
				// Client socket is not responding
				//TODO: Add Tracing here!
			}
			catch (SerializationException)
			{ 
				// Client sends bad data
				//TODO: Add Tracing here!
			}
			// Wait for next Client request
			listener.BeginAccept(new AsyncCallback(listener_Accept), new object[] {listener, channel});
		}

		// Hashtable<string(server), Stack<object[Connection, Message]>>
		static readonly Hashtable outstandingMessages = new Hashtable();

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
				if (!_listeners.Contains(m.Guid))
				{
					// New incoming message
					if (_listeners.Contains(connection.LocalChannelID))
					{
						AsyncResult myAr = (AsyncResult)_listeners[connection.LocalChannelID];
						_listeners.Remove(connection.LocalChannelID);
						myAr.Complete(connection, m);
					}
					else if (_listeners.Contains(connection.LocalAddress))
					{
						AsyncResult myAr = (AsyncResult)_listeners[connection.LocalAddress];
						_listeners.Remove(connection.LocalAddress);
						myAr.Complete(connection, m);
					}
					else
					{
						Stack outstanding = (Stack)outstandingMessages[connection.LocalAddress];
						if (outstanding == null)
						{
							outstanding = new Stack();
							outstandingMessages.Add(connection.LocalAddress, outstanding);
						}
						outstanding.Push(new Object[] {connection, m});
					}	
				}
				else
				{
					// Response to previous message
					AsyncResult myAr = (AsyncResult)_listeners[m.Guid];
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
					AsyncResult myAr = (AsyncResult)_listeners[key];
					if (myAr != null && myAr.Connection == e.Connection)
					{
						myAr.Complete(e);
						toBeDeleted.Add(myAr);
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
				Debug.Assert(!_listeners.Contains(guidOrServer), "Handler for this guid already registered.");

				AsyncResult ar = new AsyncResult(connection, callback, asyncState);
				if (outstandingMessages.Contains(guidOrServer))
				{
					Stack outstanding = (Stack)outstandingMessages[guidOrServer];
					if (outstanding.Count > 0)
					{
						object[] result = (object[])outstanding.Pop();
						ar.Complete((Connection)result[0], (Message)result[1]);
						return ar;
					}
				}
				_listeners.Add(guidOrServer, ar);
				return ar;
			}
		}

		public static Message EndReadMessage(out Connection connection, IAsyncResult ar)
		{
			AsyncResult myAr = (AsyncResult)ar;
			if (myAr.Failed)
				throw myAr.Exception;
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
	}
}