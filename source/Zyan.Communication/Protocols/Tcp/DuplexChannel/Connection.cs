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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Toolbox.Diagnostics;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	/// <summary>
	/// Defines connection roles. A connection may act as Server or as Client.
	/// </summary>
	public enum ConnectionRole
	{
		/// <summary>
		/// Connection acts as client.
		/// </summary>
		ActAsClient = 1,

		/// <summary>
		/// Connection acts as server.
		/// </summary>
		ActAsServer
	}

	/// <summary>
	/// Encapsulates a connection, providing read/write locking for synchronisation.
	/// Additionally, this should provide a useful position for adding reconnection abilities.
	/// </summary>
	public class Connection
	{
		#region Connection management

		private static readonly Dictionary<string, Connection> _connections=new Dictionary<string, Connection>();
		private static object _connectionsLockObject = new object();

		private static readonly Regex _addressRegEx=new Regex(@"^(?<address>[^:]+)(:(?<port>\d+))?$", RegexOptions.Compiled);

		/// <summary>
		/// Gets a specified connection.
		/// </summary>
		/// <param name="address">Address of the connection</param>
		/// <param name="channel">Channel of the connection</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		/// <param name="maxRetries">Maximum number of connection retry attempts</param>
		/// <param name="retryDelay">Delay after connection retry in milliseconds</param>
		/// <returns>Connection</returns>
		public static Connection GetConnection(string address, TcpExChannel channel, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval, short maxRetries, int retryDelay)
		{
			if (string.IsNullOrEmpty(address))
				throw new ArgumentException(LanguageResource.ArgumentException_AddressMustNotBeEmpty, "address");

			if (channel == null)
				throw new ArgumentNullException("channel");

			Trace.WriteLine("TcpEx.Connection.GetConnection: {0}", address);

			Connection connection = null;
			var newConnectionEstablished = false;

			lock (_connectionsLockObject)
			{
				if (_connections.ContainsKey(address))
				{
					var foundConnection = _connections[address];
					Trace.WriteLine("TcpEx.Connection found. ChannelID: {0}", foundConnection._channel.ChannelID);

					if (foundConnection.IsClosed)
						_connections.Remove(address);
					else
						return foundConnection;
				}

				try
				{
					Trace.WriteLine("TcpEx.Connection is created...");

					connection = new Connection(address, channel, keepAlive, keepAliveTime, KeepAliveInterval, maxRetries, retryDelay);
					if (!_connections.ContainsKey(address))
						_connections.Add(address, connection); // This most often happens when using the loopback address

					Manager.StartListening(connection);
					newConnectionEstablished = true;
				}
				catch (DuplicateConnectionException ex)
				{
					connection = _connections[ex.ChannelID.ToString()];
					_connections.Add(address, connection);
				}
				catch (FormatException formatEx)
				{
					throw new RemotingException(string.Format(LanguageResource.RemotingException_ConnectionError, formatEx.Message), formatEx);
				}
			}

			if (newConnectionEstablished)
			{
				channel.OnConnectionEstablished(EventArgs.Empty);
			}

			return connection;
		}

		/// <summary>
		/// Get all currently running connection of a specified channel.
		/// </summary>
		/// <param name="channel">TcpEx Channel</param>
		/// <returns>Running connections</returns>
		internal static IEnumerable<Connection> GetRunningConnectionsOfChannel(TcpExChannel channel)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			lock (_connectionsLockObject)
			{
				return from connection in _connections.Values
					where connection._channel.ChannelID.Equals(channel.ChannelID)
					select connection;
			}
		}

		/// <summary>
		/// Unregisters all running connections of the specified <see cref="TcpExChannel"/>.
		/// </summary>
		/// <param name="channel">TcpEx Channel</param>
		internal static void UnregisterConnectionsOfChannel(TcpExChannel channel)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			lock (_connectionsLockObject)
			{
				var toBeDeleted =
					from pair in _connections
					where pair.Value._channel.ChannelID.Equals(channel.ChannelID)
					select pair.Key;

				foreach (string key in toBeDeleted.ToArray())
				{
					_connections.Remove(key);
				}
			}
		}

		/// <summary>
		/// Creates a connection object.
		/// </summary>
		/// <param name="socket">Connection socket</param>
		/// <param name="channel">Connection channel</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		/// <param name="maxRetries">Maximum number of connection retry attempts</param>
		/// <param name="retryDelay">Delay after connection retry in milliseconds</param>
		/// <returns>Connection</returns>
		public static Connection CreateConnection(Socket socket, TcpExChannel channel, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval, short maxRetries, int retryDelay)
		{
			if (socket==null)
				throw new ArgumentNullException("socket");

			if (channel == null)
				throw new ArgumentNullException("channel");

			return new Connection(socket, channel, keepAlive, keepAliveTime, KeepAliveInterval, maxRetries, retryDelay);
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new instance of the Connection class.
		/// </summary>
		/// <param name="address">Address (IP oder DNS based)</param>
		/// <param name="channel">Remoting channel</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		/// <param name="maxRetries">Maximum number of connection retry attempts</param>
		/// <param name="retryDelay">Delay after connection retry in milliseconds</param>
		protected Connection(string address, TcpExChannel channel, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval, short maxRetries, int retryDelay)
		{
			if (string.IsNullOrEmpty(address))
				throw new ArgumentException(LanguageResource.ArgumentException_AddressMustNotBeEmpty, "address");

			if (channel == null)
				throw new ArgumentNullException("channel");

			_connectionRole = ConnectionRole.ActAsClient;
			_maxRetries = maxRetries;
			_retryDelay = retryDelay;
			_channel = channel;

			Match m = _addressRegEx.Match(address);

			if (!m.Success)
				throw new FormatException(string.Format(LanguageResource.Format_Exception_InvalidAddressFormat, address));

			IPAddress remoteIPAddress = Manager.GetHostByName(m.Groups["address"].Value);
			_socketRemoteAddress = remoteIPAddress.ToString();
			_socketRemotePort = int.Parse(m.Groups["port"].Value);

			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_socket.Connect(new IPEndPoint(remoteIPAddress, _socketRemotePort));

			CheckSocket();

			if (!SendChannelInfo())
				throw new RemotingException(LanguageResource.RemotingException_ErrorSendingChannelInfo);

			if (!ReceiveChannelInfo())
				throw new RemotingException(LanguageResource.RemotingException_ErrorReceivingChannelInfo);

			if (_connections.ContainsKey(_remoteChannelData.ChannelID.ToString()))
			{
				_socket.Close();
				throw new DuplicateConnectionException(_remoteChannelData.ChannelID);
			}
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
			TcpKeepAliveEnabled = keepAlive;

			AddToConnectionList();
		}

		/// <summary>
		/// Creates a new instance of the Connection class.
		/// </summary>
		/// <param name="socket">Socket which sould be used</param>
		/// <param name="channel">Remoting channel</param>
		/// <param name="keepAlive">Enables or disables TCP KeepAlive for the new connection</param>
		/// <param name="keepAliveTime">Time for TCP KeepAlive in Milliseconds</param>
		/// <param name="KeepAliveInterval">Interval for TCP KeepAlive in Milliseconds</param>
		/// <param name="maxRetries">Maximum number of connection retry attempts</param>
		/// <param name="retryDelay">Delay after connection retry in milliseconds</param>
		protected Connection(Socket socket, TcpExChannel channel, bool keepAlive, ulong keepAliveTime, ulong KeepAliveInterval, short maxRetries, int retryDelay)
		{
			if (socket == null)
				throw new ArgumentNullException("socket");

			if (channel == null)
				throw new ArgumentNullException("channel");

			_connectionRole = ConnectionRole.ActAsServer;
			_maxRetries = maxRetries;
			_retryDelay = retryDelay;
			_channel = channel;
			_socket = socket;

			IPEndPoint remoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
			_socketRemoteAddress = remoteEndPoint.Address.ToString();
			_socketRemotePort = remoteEndPoint.Port;

			CheckSocket();

			if (!ReceiveChannelInfo())
				throw new RemotingException(LanguageResource.RemotingException_ErrorReceivingChannelInfo);

			if (!SendChannelInfo())
				throw new RemotingException(LanguageResource.RemotingException_ErrorSendingChannelInfo);

			if (_connections.ContainsKey(_remoteChannelData.ChannelID.ToString()))
			{
				socket.Close();
				throw new DuplicateConnectionException(_remoteChannelData.ChannelID);
			}
			TcpKeepAliveTime = keepAliveTime;
			TcpKeepAliveInterval = KeepAliveInterval;
			TcpKeepAliveEnabled = keepAlive;

			AddToConnectionList();
		}

		#endregion

		#region Connection logic

		/// <summary>
		/// Defines the buffer size (Default = 10K).
		/// </summary>
		public static int BufferSize = 10 * (2 << 10); // 10K

		/// <summary>
		/// Defines the connection role.
		/// </summary>
		protected ConnectionRole _connectionRole;

		/// <summary>
		/// Socket used for TCP communication.
		/// </summary>
		protected Socket _socket;

		/// <summary>
		/// Address of the remote socket endpoint.
		/// </summary>
		protected string _socketRemoteAddress;

		/// <summary>
		/// Port of the remote socket endpoint.
		/// </summary>
		protected int _socketRemotePort;

		/// <summary>
		/// Networkstream for sending and receiving raw data.
		/// </summary>
		protected Stream _stream;

		/// <summary>
		/// Reader for reading binary raw data from network stream.
		/// </summary>
		protected BinaryReader _reader;

		/// <summary>
		/// Writer for writing binary raw data to network stream.
		/// </summary>
		protected BinaryWriter _writer;

		/// <summary>
		/// Parent Remoting channel of this connection.
		/// </summary>
		protected TcpExChannel _channel;

		/// <summary>
		/// Configuration data of the remoting channel.
		/// </summary>
		protected TcpExChannelData _remoteChannelData;

		/// <summary>
		/// Maximum number of connection retry attempts.
		/// </summary>
		protected short _maxRetries = 10;

		/// <summary>
		/// Delay after retry attempt in milliseconds.
		/// </summary>
		protected int _retryDelay = 1000;

		/// <summary>
		/// Tries to reconnect, if the socket was closed unexpected.
		/// </summary>
		private void CheckSocket()
		{
			if (_socket != null)
				return;

			short retryCount = 0;
			bool success = false;

			while (_socket == null)
			{
				retryCount++;

				if (retryCount <= _maxRetries)
				{
					switch (_connectionRole)
					{
						case ConnectionRole.ActAsClient:

							if (string.IsNullOrEmpty(_socketRemoteAddress))
								throw new RemotingException(LanguageResource.RemotingException_NoAddressForReconnect);

							IPAddress remoteAddress = IPAddress.Parse(_socketRemoteAddress);

							_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
							_socket.Connect(new IPEndPoint(remoteAddress, _socketRemotePort));

							try
							{
								success = SendChannelInfo() && ReceiveChannelInfo();
							}
							catch (SocketException)
							{
								success = false;
							}
							break;

						case ConnectionRole.ActAsServer:

							// Wait for connection from client

							break;
					}
					if (!success)
					{
						if (_retryDelay > 0)
							Thread.Sleep(_retryDelay);
					}
					continue;
				}
				else
					break;
			}
		}

		/// <summary>
		/// Sends channel data.
		/// </summary>
		/// <returns>True, if sending was successfully, otherwise false</returns>
		private bool SendChannelInfo()
		{
			Stream stream = this.Stream;

			if (stream != null)
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(stream, _channel.ChannelData);

				Trace.WriteLine("TcpEx.Connection sends ChannelInfo, this ChannelID: {0}", _channel.ChannelID);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Receives channel data.
		/// </summary>
		/// <returns>True, if receiving was successfully, otherwise false</returns>
		private bool ReceiveChannelInfo()
		{
			Stream stream = this.Stream;

			if (stream != null)
			{
				BinaryFormatter formatter = new BinaryFormatter();
				_remoteChannelData = (TcpExChannelData)formatter.Deserialize(stream);

				Trace.WriteLine("TcpEx.Connection received remote ChannelInfo, ChannelID: {0}", _remoteChannelData.ChannelID);

				return true;
			}
			return false;
		}

		/// <summary>
		/// Adds the connection to the connection list.
		/// </summary>
		private void AddToConnectionList()
		{
			lock (_connectionsLockObject)
			{
				_connections[_remoteChannelData.ChannelID.ToString()] = this;

				if (_remoteChannelData.Addresses != null)
				{
					foreach (string address in _remoteChannelData.Addresses)
					{
						_connections[address] = this;
					}
				}
			}
		}

		private bool _isClosed = false;

		/// <summary>
		/// Returns if the connection is already closed or not.
		/// </summary>
		public bool IsClosed
		{
			get
			{
				return _isClosed;
			}
		}

		/// <summary>
		/// Closes the connection.
		/// </summary>
		public void Close()
		{
			lock (_connectionsLockObject)
			{
				List<string> toBeDeleted = (from pair in _connections
											where pair.Value == this
											select pair.Key).ToList();

				foreach (string key in toBeDeleted)
				{
					_connections.Remove(key);
				}
			}
			LockRead();
			LockWrite();

			_socketRemoteAddress = null;
			_socketRemotePort = 0;

			if (_reader != null)
			{
				try
				{
					_reader.Close();
				}
				catch (ObjectDisposedException)
				{
				}
				finally
				{
					_reader = null;
				}
			}
			if (_writer != null)
			{
				try
				{
					_writer.Close();
				}
				catch (ObjectDisposedException)
				{
				}
				finally
				{
					_writer = null;
				}
			}
			if (_stream != null)
			{
				try
				{
					_stream.Close();
				}
				catch (ObjectDisposedException)
				{
				}
				finally
				{
					_stream = null;
				}
			}
			if (_socket != null)
			{
				try
				{
					_socket.Shutdown(SocketShutdown.Both);
					_socket.Close();
				}
				catch (SocketException)
				{ 
				}
				catch (ObjectDisposedException)
				{
				}
				finally
				{
					_socket = null;
				}
			}
			if (_channel != null)
				_channel = null;

			if (_remoteChannelData != null)
				_remoteChannelData = null;

			ReleaseRead();
			ReleaseWrite();

			_isClosed = true;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the underlying socket of the connection.
		/// </summary>
		public Socket Socket
		{
			get { return _socket; }
		}

		/// <summary>
		/// Gets the Network stream.
		/// </summary>
		public Stream Stream
		{
			get
			{
				if (_stream == null)
				{
					if (_socket != null)
						_stream = new NetworkStream(_socket, FileAccess.ReadWrite, false);
				}
				return _stream;
			}
		}

		/// <summary>
		/// Gets a binary reader for reading raw data from the network stream.
		/// </summary>
		public BinaryReader Reader
		{
			get
			{
				if (_reader == null)
				{
					CheckSocket();

					Stream stream = this.Stream;

					if (stream != null)
						_reader = new BinaryReader(stream);
				}
				return _reader;
			}
		}

		/// <summary>
		/// Gets a binary writer for writing raw data from the network stream.
		/// </summary>
		public BinaryWriter Writer
		{
			get
			{
				if (_writer == null)
				{
					CheckSocket();

					Stream stream = this.Stream;

					if (stream != null)
						_writer = new BinaryWriter(new BufferedStream(stream, BufferSize));
				}
				return _writer;
			}
		}

		/// <summary>
		/// Checks, if the connection is a local connection or not.
		/// </summary>
		public bool IsLocalHost
		{
			get
			{
				if (_socket != null && _socket.RemoteEndPoint != null)
				{
					var address = ((IPEndPoint)_socket.RemoteEndPoint).Address;
					return IPAddress.IsLoopback(address) || IsLocalIP(address);
				}

				return false;
			}
		}

		/// <summary>
		/// Checks if a specified IP address is a local IP or not.
		/// </summary>
		/// <param name="remoteAddress">IP address to check</param>
		/// <returns>true, if local, otherwise false</returns>
		private bool IsLocalIP(IPAddress remoteAddress)
		{
			try
			{
				foreach (var address in Manager.GetAddresses())
				{
					if (address.Equals(remoteAddress))
						return true;
				}

				return false;
			}
			catch (Exception ex)
			{
				// transport sink shouldn't ever throw exceptions
				Trace.WriteLine("IsLocalIP exception: {0}", ex);
				return false;
			}
		}

		/// <summary>
		/// Gets the unique identifier of the remote channel.
		/// </summary>
		public Guid RemoteChannelID
		{
			get { return _remoteChannelData.ChannelID; }
		}

		/// <summary>
		/// Gets a list of all registered addresses of the remote channel.
		/// </summary>
		public List<string> RemoteAddresses
		{
			get { return _remoteChannelData.Addresses; }
		}

		/// <summary>
		/// Gets the unique identifier of the local channel.
		/// </summary>
		public Guid LocalChannelID
		{
			get { return _channel.ChannelID; }
		}

		/// <summary>
		/// Gets the address of the local channel.
		/// </summary>
		public string LocalAddress
		{
			get { return _socket.LocalEndPoint.ToString(); }
		}

		/// <summary>
		/// Gets or sets the maximum number of connection retry attempts.
		/// </summary>
		public short MaxRetries
		{
			get { return _maxRetries; }
			set { _maxRetries = value; }
		}

		/// <summary>
		/// Gets or sets the delay after a retry attempt in milliseconds.
		/// </summary>
		public int RetryDelay
		{
			get { return _retryDelay; }
			set { _retryDelay = value; }
		}

		#endregion

		#region Locking

		private object _readLock = new object();
		private object _writeLock = new object();

		/// <summary>
		/// Locks the connection for reading through other threads.
		/// </summary>
		public void LockRead()
		{
			Monitor.Enter(_readLock);
		}

		/// <summary>
		/// Releases the read lock.
		/// </summary>
		public void ReleaseRead()
		{
			Monitor.Exit(_readLock);
		}

		/// <summary>
		/// Locks the connection for writing through other threads.
		/// </summary>
		public void LockWrite()
		{
			Monitor.Enter(_writeLock);
		}

		/// <summary>
		/// Releases the write lock.
		/// </summary>
		public void ReleaseWrite()
		{
			Monitor.Exit(_writeLock);
		}

		#endregion

		#region TCP KeepAlive

		private bool _tcpKeepAliveEnabled = true;
		private ulong _tcpKeepAliveTime = 30000;
		private ulong _tcpKeepAliveInterval = 1000;

		const int BYTES_PER_LONG = 4;
		const int BITS_PER_BYTE = 8;

		/// <summary>
		/// Enables or disables TCP KeepAlive.
		/// </summary>
		public bool TcpKeepAliveEnabled
		{
			get { return _tcpKeepAliveEnabled; }
			set
			{
				_tcpKeepAliveEnabled = value;
				
				if (_socket!=null)
					_tcpKeepAliveEnabled = SetTcpKeepAlive(_socket, _tcpKeepAliveEnabled ? TcpKeepAliveTime : 0, _tcpKeepAliveEnabled ? TcpKeepAliveInterval : 0);
			}
		}

		/// <summary>
		/// Gets or sets the TCP KeepAlive time in milliseconds.
		/// </summary>
		public ulong TcpKeepAliveTime
		{
			get { return _tcpKeepAliveTime; }
			set { _tcpKeepAliveTime = value; }
		}

		/// <summary>
		/// Gets or sets the TCP KeepAlive interval in milliseconds
		/// </summary>
		public ulong TcpKeepAliveInterval
		{
			get { return _tcpKeepAliveInterval; }
			set { _tcpKeepAliveInterval = value; }
		}

		/// <summary>
		/// Sets TCP-KeepAlive option for a specified socket.
		/// </summary>
		/// <param name="socket">Socket</param>
		/// <param name="time">Time in milliseconds</param>
		/// <param name="interval">Interval in milliseconds</param>
		/// <returns>True if successful, otherwiese false</returns>
		private bool SetTcpKeepAlive(Socket socket, ulong time, ulong interval)
		{
			if (MonoCheck.IsRunningOnMono && MonoCheck.IsUnixOS)
				return false; // Socket.IOControl method doesn´t work on Linux or Mac with mono

			try
			{
				byte[] sioKeepAliveValues = new byte[3 * BYTES_PER_LONG];
				ulong[] input = new ulong[3];

				if (time == 0 || interval == 0)
					input[0] = (0UL); // Off
				else
					input[0] = (1UL); // On

				input[1] = time;
				input[2] = interval;

				for (int i = 0; i < input.Length; i++)
				{
					sioKeepAliveValues[i * BYTES_PER_LONG + 3] = (byte)(input[i] >> ((BYTES_PER_LONG - 1) * BITS_PER_BYTE) & 0xff);
					sioKeepAliveValues[i * BYTES_PER_LONG + 2] = (byte)(input[i] >> ((BYTES_PER_LONG - 2) * BITS_PER_BYTE) & 0xff);
					sioKeepAliveValues[i * BYTES_PER_LONG + 1] = (byte)(input[i] >> ((BYTES_PER_LONG - 3) * BITS_PER_BYTE) & 0xff);
					sioKeepAliveValues[i * BYTES_PER_LONG + 0] = (byte)(input[i] >> ((BYTES_PER_LONG - 4) * BITS_PER_BYTE) & 0xff);
				}
				byte[] result = BitConverter.GetBytes(0);

				socket.IOControl(IOControlCode.KeepAliveValues, sioKeepAliveValues, result);
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		#endregion
	}
}