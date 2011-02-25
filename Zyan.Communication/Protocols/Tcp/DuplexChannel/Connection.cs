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
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using Zyan.Communication.Protocols.Tcp.DuplexChannel.Diagnostics;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	internal class DuplicateConnectionException : Exception
	{
		Guid guid;

		public DuplicateConnectionException(Guid guid)
		{
			this.guid = guid;
		}

		public Guid Guid
		{
			get
			{
				return guid;
			}
		}
	}

	/// <summary>
	/// Encapsulates a connection, providing read/write locking for synchronisation.  
	/// Additionally, this should provide a useful position for adding reconnection abilities.
	/// </summary>
	class Connection
	{
		#region Statics
		// Hashtable<string(address), Connection>
		static readonly Hashtable connections = new Hashtable();
		static readonly Regex regServerAddress = new Regex(@"^(?<address>[^:]+)(:(?<port>\d+))?$", RegexOptions.Compiled);

		public static Connection GetConnection(string address, TcpExChannel channel)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			lock (connections)
			{
				Connection retVal = (Connection)connections[address];
				if (retVal == null)
				{
					try
					{
						retVal = new Connection(address, channel);
						if (!connections.Contains(address))
							connections.Add(address, retVal); // This most often happens when using the loopback address
						Manager.StartListening(retVal);
					}
					catch (DuplicateConnectionException ex)
					{
						Trace.WriteLine("Detected attempt to create new duplicate connection");
						retVal = (Connection)connections[ex.Guid.ToString()];
						connections.Add(address, retVal);
					}
				}
				return retVal;
			}
		}

		public static Connection RegisterConnection(Socket socket, TcpExChannel channel)
		{
			return new Connection(socket, channel);
		}

		public static IList GetAddresses(Connection connection)
		{
			lock (connections)
			{
				ArrayList retVal = new ArrayList();
				foreach (object key in connections.Keys)
					if (connections[key] == connection)
						retVal.Add(key);
				return retVal;
			}
		}
		#endregion

		public static int BufferSize = 10 * (2 << 10); // 10K

		protected Socket socket;
		protected Stream stream;
		protected BinaryReader reader;
		protected BinaryWriter writer;
		protected TcpExChannel channel;
		protected TcpExChannelData remoteData;

		object readLock = new object(), writeLock = new object();

		#region Constructors
		protected Connection(string address, TcpExChannel channel)
		{
			this.channel = channel;

			Match m = regServerAddress.Match(address);
			if (!m.Success)
				throw new FormatException(string.Format("Invalid format for 'address' parameter - {0}", address));

			Trace.WriteLine("Creating connection - {0}", address);

			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(new IPEndPoint(Manager.GetHostByName(m.Groups["address"].Value), int.Parse(m.Groups["port"].Value)));

			SendChannelInfo();
			RecvChannelInfo();

			if (connections.Contains(remoteData.Guid.ToString()))
			{
				socket.Close();
				throw new DuplicateConnectionException(remoteData.Guid);
			}

			AddToConnectionList();
		}

		protected Connection(Socket socket, TcpExChannel channel)
		{
			this.channel = channel;

			Trace.WriteLine("Linking connection - {0}", socket.RemoteEndPoint);
			this.socket = socket;

			RecvChannelInfo();
			SendChannelInfo();

			if (connections.Contains(remoteData.Guid.ToString()))
			{
				socket.Close();
				throw new DuplicateConnectionException(remoteData.Guid);
			}

			AddToConnectionList();
		}
		#endregion

		void SendChannelInfo()
		{
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(Stream, channel.ChannelData);
		}

		void RecvChannelInfo()
		{
			BinaryFormatter formatter = new BinaryFormatter();
			remoteData = (TcpExChannelData)formatter.Deserialize(Stream);
		}

		void AddToConnectionList()
		{
			lock (connections)
			{
				Trace.WriteLine("Remote GUID: {0}", remoteData.Guid);
				connections.Add(remoteData.Guid.ToString(), this);
				if (remoteData.Addresses != null)
					foreach (string address in remoteData.Addresses)
					{
						Trace.WriteLine("Remote Address: {0}", address);
						connections.Add(address, this);
					}
			}
		}

		void AddLoopback()
		{
			connections.Add("localhost", this);
			connections.Add(IPAddress.Loopback.ToString(), this);
		}

		public void Close()
		{
			// TODO: Handling disconnections...
			// Maybe leave this around then next time it's requested we can reconnect...
			// But, if the other side reconnects we need to match them up in the RegisterConnection method.
			lock (connections)
			{
				ArrayList toBeDeleted = new ArrayList();
				foreach (object key in connections.Keys)
					if (connections[key] == this)
						toBeDeleted.Add(key);

				foreach (object key in toBeDeleted)
					connections.Remove(key);
			}
			socket.Close();
		}

		#region Properties
		public Socket Socket
		{
			get
			{
				return socket;
			}
		}

		public Stream Stream
		{
			get
			{
				if (stream == null)
					stream = new NetworkStream(socket, FileAccess.ReadWrite, false);
				return stream;
			}
		}

		public BinaryReader Reader
		{
			get
			{
				if (reader == null)
					reader = new BinaryReader(Stream);
				return reader;
			}
		}

		public BinaryWriter Writer
		{
			get
			{
				if (writer == null)
					writer = new BinaryWriter(new BufferedStream(Stream, BufferSize));
				return writer;
			}
		}

		public bool IsLocalHost
		{
			get
			{
				IPAddress address = ((IPEndPoint)socket.RemoteEndPoint).Address;
				return IPAddress.IsLoopback(address) || IsLocalIP(address);
			}
		}

		bool IsLocalIP(IPAddress remoteAddress)
		{
			foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
				if (address.Equals(remoteAddress))
					return true;
			return false;
		}

		public Guid RemoteGuid
		{
			get
			{
				return remoteData.Guid;
			}
		}

		public IList RemoteAddresses // IList<string>
		{
			get
			{
				return remoteData.Addresses;
			}
		}

		public Guid LocalGuid
		{
			get
			{
				return channel.Guid;
			}
		}

		public string LocalAddress
		{
			get
			{
				return socket.LocalEndPoint.ToString();
			}
		}
		#endregion

		#region Locking
		public void LockRead()
		{
			Monitor.Enter(readLock);
		}

		public void ReleaseRead()
		{
			Monitor.Exit(readLock);
		}

		public void LockWrite()
		{
			Monitor.Enter(writeLock);
		}

		public void ReleaseWrite()
		{
			Monitor.Exit(writeLock);
		}
		#endregion
	}
}