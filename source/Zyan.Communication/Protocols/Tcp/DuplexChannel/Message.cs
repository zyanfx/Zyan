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

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	delegate void MessageHandler(Message m);

	#region MessageException : IOException
	class MessageException : IOException
	{
		Connection connection;

		public MessageException(string msg, Exception innerException, Connection connection) : base(msg, innerException)
		{
			this.connection = connection;
		}

		public Connection Connection
		{
			get
			{
				return connection;
			}
		}
	}
	#endregion

	class Message
	{
		static readonly BinaryFormatter formatter = new BinaryFormatter();
		internal const int SizeOfGuid = 16;

		public Guid Guid;
		public ITransportHeaders Headers;
		Stream messageBody;
		byte[] messageBodyBytes;

		protected Message()
		{
		}

		public Message(Guid guid, ITransportHeaders headers, Stream message)
		{
			this.Guid = guid;
			this.Headers = headers;
			messageBody = message;
		}

		public Stream MessageBody
		{
			get
			{
				if (messageBody == null && messageBodyBytes != null)
					messageBody = new MemoryStream(messageBodyBytes);
				return messageBody;
			}
		}

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

		public static IAsyncResult BeginReceive(Connection connection, AsyncCallback callback, object asyncState)
		{
			byte[] buffer = new Byte[SizeOfGuid];
			AsyncResult myAr = new AsyncResult(connection, buffer, callback, asyncState);
			myAr.InternalAsyncResult = connection.Socket.BeginReceive(buffer, 0, SizeOfGuid, SocketFlags.None, new AsyncCallback(myAr.Complete), null);
			return myAr;
		}

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
					retVal.messageBodyBytes = reader.ReadBytes(bodyLength);
					if (retVal.messageBodyBytes.Length != bodyLength)
						throw new Exception("Not enough body read...");

					System.Diagnostics.Debug.Assert(retVal.MessageBody.CanRead);
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

		public override string ToString()
		{
			return Guid.ToString();
		}

		#region AsyncResult
		class AsyncResult : IAsyncResult
		{
			byte[] buffer;
			bool isCompleted;
			object asyncState;
			Connection connection;
			AsyncCallback callback;
			IAsyncResult internalAsyncResult;
			System.Threading.ManualResetEvent waitHandle;

			public AsyncResult(Connection connection, byte[] buffer, AsyncCallback callback, object asyncState)
			{
				this.buffer = buffer;
				this.callback = callback;
				this.asyncState = asyncState;
				this.connection = connection;
			}

			public void Complete(IAsyncResult ar)
			{
				lock (this)
				{
					internalAsyncResult = ar;
					isCompleted = true;
					if (waitHandle != null)
						waitHandle.Set();
					if (callback != null)
						callback(this);
				}
			}

			public AsyncCallback Callback
			{
				get
				{
					return callback;
				}
			}

			public byte[] Buffer
			{
				get
				{
					return buffer;
				}
			}

			public Connection Connection
			{
				get
				{
					return connection;
				}
			}

			public IAsyncResult InternalAsyncResult
			{
				get
				{
					return internalAsyncResult;
				}

				set
				{
					lock (this)
					{
						internalAsyncResult = value;
					}
				}
			}
		
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
					return internalAsyncResult.CompletedSynchronously;
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
