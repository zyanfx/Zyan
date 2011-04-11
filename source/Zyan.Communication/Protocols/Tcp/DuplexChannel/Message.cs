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
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	/// <summary>
	/// A message which can be sent or received over a Duplex Channel instance.
	/// </summary>
	public class Message
	{
		private static readonly BinaryFormatter formatter = new BinaryFormatter();
		internal const int SizeOfGuid = 16;

		public Guid Guid;
		public ITransportHeaders Headers;
		private Stream messageBody;
		private byte[] messageBodyBytes;

        /// <summary>
        /// Creates a new instance of the Message class.
        /// </summary>
		protected Message()
		{
		}

        /// <summary>
        /// Creates a new instance of the Message class.
        /// </summary>
        /// <param name="guid">Unique identifier of the message</param>
        /// <param name="headers">Remoting transport headers</param>
        /// <param name="message">Stream for message raw data</param>
		public Message(Guid guid, ITransportHeaders headers, Stream message)
		{
			this.Guid = guid;
			this.Headers = headers;
			messageBody = message;
		}

        /// <summary>
        /// Gets a stream for accessing the message´s body raw data.
        /// </summary>
		public Stream MessageBody
		{
			get
			{
				if (messageBody == null && messageBodyBytes != null)
					messageBody = new MemoryStream(messageBodyBytes);
				return messageBody;
			}
		}

        /// <summary>
        /// Sends the message over a specified connection.
        /// </summary>
        /// <param name="connection">Duplex Channel Connection</param>
		public void Send(Connection connection)
		{
			try
			{
				connection.LockWrite();
				BinaryWriter writer = connection.Writer;
				writer.Write(Guid.ToByteArray());
				MemoryStream headerStream = new MemoryStream();
				formatter.Serialize(headerStream, Headers);
				writer.Write((int)headerStream.Length);
				writer.Write(headerStream.GetBuffer(), 0, (int)headerStream.Length);
				
				writer.Write((int)MessageBody.Length);
				MemoryStream ms = MessageBody as MemoryStream;
				
                if (ms == null)
				{
					byte[] msgBuffer = new byte[MessageBody.Length];
					MessageBody.Read(msgBuffer, 0, (int)MessageBody.Length);
					writer.Write(msgBuffer);
				}
				else
					writer.Write(ms.GetBuffer(), 0, (int)MessageBody.Length);
				
                writer.Flush();
			}
			finally
			{
				connection.ReleaseWrite();
			}
		}

        /// <summary>
        /// Sends a specified message over a specified connection.
        /// </summary>
        /// <param name="connection">Duplex Channel Connection</param>
        /// <param name="guid">Unique identifier of the Message</param>
        /// <param name="headers">Remoting transport headers</param>
        /// <param name="message">Stream with raw data of the message</param>
		public static void Send(Connection connection, Guid guid, ITransportHeaders headers, Stream message)
		{
			try
			{
				connection.LockWrite();
				BinaryWriter writer = connection.Writer;
				writer.Write(guid.ToByteArray());

				MemoryStream headerStream = new MemoryStream();
				formatter.Serialize(headerStream, headers);
				writer.Write((int)headerStream.Length);
				writer.Write(headerStream.GetBuffer(), 0, (int)headerStream.Length);
				
				writer.Write((int)message.Length);
				MemoryStream ms = message as MemoryStream;
				if (ms == null)
				{
					byte[] msgBuffer = new byte[message.Length];
					message.Read(msgBuffer, 0, (int)message.Length);
					writer.Write(msgBuffer, 0, (int)message.Length);
				}
				else
					writer.Write(ms.GetBuffer(), 0, (int)message.Length);
				writer.Flush();
			}
			finally
			{
				connection.ReleaseWrite();
			}
		}

        /// <summary>
        /// Begins receiving message data asynchronously.
        /// </summary>
        /// <param name="connection">Duplex Channel Connection</param>
        /// <param name="callback">Delegate to invoke, when asynchronous operation is completed</param>
        /// <param name="asyncState">Pass through state object</param>
        /// <returns>Result</returns>
		public static IAsyncResult BeginReceive(Connection connection, AsyncCallback callback, object asyncState)
		{
			byte[] buffer = new Byte[SizeOfGuid];
			AsyncResult myAr = new AsyncResult(connection, buffer, callback, asyncState);
			myAr.InternalAsyncResult = connection.Socket.BeginReceive(buffer, 0, SizeOfGuid, SocketFlags.None, new AsyncCallback(myAr.Complete), null);
			return myAr;
		}

        /// <summary>
        /// Receives a message over a specified Duplex Channel Connection.
        /// </summary>
        /// <param name="connection">Duplex Channel Connection</param>
        /// <param name="ar">Result (for Async pattern)</param>
        /// <returns>Received message</returns>
		public static Message EndReceive(out Connection connection, IAsyncResult ar)
		{
			AsyncResult myAr = (AsyncResult)ar;
			connection = myAr.Connection;
			try
			{
				connection.LockRead();
				int bytesRead = connection.Socket.EndReceive(myAr.InternalAsyncResult);
				if (bytesRead == 16)
				{
					Message retVal = new Message();
					retVal.Guid = new Guid(myAr.Buffer);
					BinaryReader reader = connection.Reader;

					int headerLength = reader.ReadInt32();
					MemoryStream headerStream = new MemoryStream(reader.ReadBytes(headerLength));
					if (headerStream.Length != headerLength)
						throw new Exception("Not enough headers read...");
					retVal.Headers = (ITransportHeaders)formatter.Deserialize(headerStream);

					int bodyLength = reader.ReadInt32();

                    if (bodyLength > 0)
                    {

                        retVal.messageBodyBytes = reader.ReadBytes(bodyLength);
                        if (retVal.messageBodyBytes.Length != bodyLength)
                            throw new Exception("Not enough body read...");
                        
                        System.Diagnostics.Debug.Assert(retVal.MessageBody.CanRead);
                    }
                    
                    Message.BeginReceive(connection, myAr.Callback, myAr.AsyncState);
                    
					return retVal;
				}
				else if (bytesRead == 0)
				{
					throw new MessageException("Connection closed.", null, connection);
				}
				else
					throw new MessageException("Insufficient data received", null, connection);
			}
			catch (Exception e)
			{
				throw new MessageException("Error receiving message", e, connection);
			}
			finally
			{
				connection.ReleaseRead();
			}
		}

        /// <summary>
        /// Gets a string representation of this object.
        /// </summary>
        /// <returns>Unique message identifier</returns>
		public override string ToString()
		{
			return Guid.ToString();
		}

		#region AsyncResult

        /// <summary>
        /// State object needed to perform asynchronous receive operations.
        /// </summary>
		private class AsyncResult : IAsyncResult
		{
			byte[] buffer;
			bool isCompleted;
			object asyncState;
			Connection connection;
			AsyncCallback callback;
			IAsyncResult internalAsyncResult;
			System.Threading.ManualResetEvent waitHandle;
            private object _lockObject = new object();
            private object _internalAsyncResultLockObject = new object();

            /// <summary>
            /// Creates a new instance of the AsyncResult class.
            /// </summary>
            /// <param name="connection">Duplex Channel Connection</param>
            /// <param name="buffer">Buffer</param>
            /// <param name="callback">Delegate to invoke, when asynchronous operation is completed</param>
            /// <param name="asyncState">Pass trough state object</param>
			public AsyncResult(Connection connection, byte[] buffer, AsyncCallback callback, object asyncState)
			{
				this.buffer = buffer;
				this.callback = callback;
				this.asyncState = asyncState;
				this.connection = connection;
			}

            /// <summary>
            /// Marks the asynchronous receive operation as completed.
            /// </summary>
            /// <param name="ar">Result (for Async pattern)</param>
			public void Complete(IAsyncResult ar)
			{
                lock (_lockObject)
				{
					internalAsyncResult = ar;
					isCompleted = true;
					if (waitHandle != null)
						waitHandle.Set();
					if (callback != null)
						callback(this);
				}
			}

            /// <summary>
            /// Gets the callback delegate.
            /// </summary>
			public AsyncCallback Callback
			{
				get { return callback; }
			}

            /// <summary>
            /// Gets the receive buffer.
            /// </summary>
			public byte[] Buffer
			{
				get { return buffer; }
			}

            /// <summary>
            /// Get the affected Duplex Channel Connection.
            /// </summary>
			public Connection Connection
			{
				get { return connection; }
			}

            /// <summary>
            /// Gets the internal async. result.
            /// </summary>
			public IAsyncResult InternalAsyncResult
			{
				get { return internalAsyncResult; }
				set 
                {   
                    lock (_internalAsyncResultLockObject) 
					{
						internalAsyncResult = value;
					}
				}
			}
		
			#region Implementation of IAsyncResult
			
            /// <summary>
            /// Gets the pass through state object.
            /// </summary>
            public object AsyncState
			{
				get { return asyncState; }
			}

            /// <summary>
            /// Gets if the operation is completed synchronously or not.
            /// </summary>
			public bool CompletedSynchronously
			{
				get { return internalAsyncResult.CompletedSynchronously; }
			}

            /// <summary>
            /// Gets a wait handle for the asynchronous operation.
            /// </summary>
			public WaitHandle AsyncWaitHandle
			{
				get
				{
                    lock (_internalAsyncResultLockObject)
					{
						if (waitHandle == null)
							waitHandle = new System.Threading.ManualResetEvent(isCompleted);
						return waitHandle;
					}
				}
			}

            /// <summary>
            /// Gets if the asynchronous operation is completed or not.
            /// </summary>
			public bool IsCompleted
			{
				get { return isCompleted; }
			}

			#endregion
		}

		#endregion
	}
}
