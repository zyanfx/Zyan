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
using System.Linq;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using Zyan.Communication.Protocols.Tcp.DuplexChannel.Diagnostics;
using System.Runtime.Remoting;

//TODO: Localize Exceptions.

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
    /// <summary>
    /// This exception should be thrown, when an attempt to create a duplicate connection is detected.
    /// </summary>
	internal class DuplicateConnectionException : Exception
	{
        /// <summary>
        /// Creates a new instance of the DuplicateConnectionException class.
        /// </summary>
        /// <param name="channelID">Unique channel identifier</param>
		public DuplicateConnectionException(Guid channelID)
		{
            ChannelID = channelID;
		}

        /// <summary>
        /// Gets the unique channel identifier.
        /// </summary>
        public Guid ChannelID
        {
            get;
            private set;
        }
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
        /// <returns>Connection</returns>
		public static Connection GetConnection(string address, TcpExChannel channel)
		{
			if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Address must not be empty.", "address");

            if (channel == null)
                throw new ArgumentNullException("channel");

			lock (_connectionsLockObject)
			{
                if (_connections.ContainsKey(address))
                    return _connections[address];
                
                Connection connection=null;

                try
                {
                    connection = new Connection(address, channel);
                    if (!_connections.ContainsKey(address))
                        _connections.Add(address, connection); // This most often happens when using the loopback address

                    Manager.StartListening(connection);
                }
                catch (DuplicateConnectionException ex)
                {
                    connection = (Connection)_connections[ex.ChannelID.ToString()];
                    _connections.Add(address, connection);
                }
                catch (FormatException formatEx)
                {
                    throw new RemotingException(string.Format(LanguageResource.RemotingException_ConnectionError, formatEx.Message), formatEx);
                }
				return connection;
			}
		}

        /// <summary>
        /// Get all currently running connection of a specified channel.
        /// </summary>
        /// <param name="channel">TcpEx Channel</param>
        /// <returns>Running connections</returns>
        public static IEnumerable<Connection> GetRunningConnectionsOfChannel(TcpExChannel channel)
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
        /// Creates a connection object.
        /// </summary>
        /// <param name="socket">Connection socket</param>
        /// <param name="channel">Connection channel</param>
        /// <returns>Connection</returns>
		public static Connection CreateConnection(Socket socket, TcpExChannel channel)
		{
            if (socket==null)
                throw new ArgumentNullException("socket");

            if (channel == null)
                throw new ArgumentNullException("channel");

			return new Connection(socket, channel);
		}               

        #endregion

		#region Constructors
		
        /// <summary>
        /// Creates a new instance of the Connection class.
        /// </summary>
        /// <param name="address">Address (IP oder DNS based)</param>
        /// <param name="channel">Remoting channel</param>
        protected Connection(string address, TcpExChannel channel)
		{
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Address must not be empty.", "address");

            if (channel == null)
                throw new ArgumentNullException("channel");

			_channel = channel;

			Match m = _addressRegEx.Match(address);
			
            if (!m.Success)
				throw new FormatException(string.Format("Invalid format for 'address' parameter - {0}", address));

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_socket.Connect(new IPEndPoint(Manager.GetHostByName(m.Groups["address"].Value), int.Parse(m.Groups["port"].Value)));

			SendChannelInfo();
			ReceiveChannelInfo();

			if (_connections.ContainsKey(_remoteChannelData.ChannelID.ToString()))
			{
				_socket.Close();
				throw new DuplicateConnectionException(_remoteChannelData.ChannelID);
			}
			AddToConnectionList();
		}

        /// <summary>
        /// Creates a new instance of the Connection class.
        /// </summary>
        /// <param name="socket">Socket which sould be used</param>
        /// <param name="channel">Remoting channel</param>
		protected Connection(Socket socket, TcpExChannel channel)
		{
            if (socket == null)
                throw new ArgumentNullException("socket");

            if (channel == null)
                throw new ArgumentNullException("channel");

			this._channel = channel;            		
			this._socket = socket;

			ReceiveChannelInfo();
			SendChannelInfo();

			if (_connections.ContainsKey(_remoteChannelData.ChannelID.ToString()))
			{
				socket.Close();
				throw new DuplicateConnectionException(_remoteChannelData.ChannelID);
			}
			AddToConnectionList();
		}

		#endregion

        #region Connection logic

        /// <summary>
        /// Defines the buffer size (Default = 10K).
        /// </summary>
        public static int BufferSize = 10 * (2 << 10); // 10K

        /// <summary>
        /// Socket used for TCP communication.
        /// </summary>
        protected Socket _socket;
        
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
        /// Sends channel data.
        /// </summary>
		private void SendChannelInfo()
		{
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(Stream, _channel.ChannelData);
		}

        /// <summary>
        /// Receives channel data.
        /// </summary>
		private void ReceiveChannelInfo()
		{
			BinaryFormatter formatter = new BinaryFormatter();
			_remoteChannelData = (TcpExChannelData)formatter.Deserialize(Stream);
		}

        /// <summary>
        /// Adds the connection to the connection list.
        /// </summary>
		private void AddToConnectionList()
		{
			lock (_connectionsLockObject)
			{				
				_connections.Add(_remoteChannelData.ChannelID.ToString(), this);

                if (_remoteChannelData.Addresses != null)
                {
                    foreach (string address in _remoteChannelData.Addresses)
                    {                     
                        _connections.Add(address, this);
                    }
                }
			}
		}

        /// <summary>
        /// Add connections for loopback to the connection list.
        /// </summary>
		private void AddLoopbackConnections()
		{
            lock (_connectionsLockObject)
            {
                _connections.Add("localhost", this);
                _connections.Add(IPAddress.Loopback.ToString(), this);
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
                    _socket.Close();
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
                    _stream = new NetworkStream(_socket, FileAccess.ReadWrite, false);
                
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
					_reader = new BinaryReader(Stream);
				
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
					_writer = new BinaryWriter(new BufferedStream(Stream, BufferSize));
				
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
				IPAddress address = ((IPEndPoint)_socket.RemoteEndPoint).Address;
				return IPAddress.IsLoopback(address) || IsLocalIP(address);
			}
		}

        /// <summary>
        /// Checks if a specified IP address is a local IP or not.
        /// </summary>
        /// <param name="remoteAddress">IP address to check</param>
        /// <returns>true, if local, otherwise false</returns>
		private bool IsLocalIP(IPAddress remoteAddress)
		{
            foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (address.Equals(remoteAddress))
                    return true;
            }
			return false;
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
	}
}