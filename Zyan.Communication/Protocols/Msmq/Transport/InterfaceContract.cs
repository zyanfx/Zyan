//=============================================================================
//	The Custom Channel Interface Contract 
//	(C) Copyright 2003, Roman Kiss (rkiss@pathcom.com)
//	All rights reserved.
//	The code and information is provided "as-is" without waranty of any kind,
//	either expresed or implied.
//
//-----------------------------------------------------------------------------
//	History:
//		06/05/2003	Roman Kiss				Initial Revision
//=============================================================================
//

#region references
using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Messaging;
using System.Runtime.Serialization;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
#endregion

namespace RKiss.CustomChannel
{
	#region IKnowledgeBaseControl
	public interface IKnowledgeBaseControl
	{
		void Save(string filename);														// save it into the config file
		void Load(string filename);														// refresh from the config file
		void Store(string strLURL, bool bOverwrite);					// store logical addresses name/value, ... 
		void Store(string name, string val, bool bOverwrite); // store single logical address
		void Update(string strLURL);													// update logical addresses name/value, ... 
		void Update(string name, string val);									// update single logical address
		void RemoveAll();																			// remove all logical addresses
		void Remove(string name);															// remove specified logical name
		object GetAll();																			// get all logical addresses
		string Get(string name);															// get the specified logical name
		string Mapping(string name);													// mapping logical name by physical, if it doesn't exist, just copy 
		bool Exists(string name);															// check if we have a specified logical name
		bool CanBeUpdated();																	// check the status
	}
	#endregion

	#region IRemotingResponse

	[Serializable]
	public class MsgSourceInfo 
	{
		Acknowledgment		m_Acknowledgment;
		DateTime					m_MessageSentTime;
		string						m_ResponseQueuePath;
		string						m_ReportQueuePath;

		public Acknowledgment Acknowledgment { get { return m_Acknowledgment;} set { m_Acknowledgment = value; }}
		public DateTime MessageSentTime { get { return m_MessageSentTime;} set { m_MessageSentTime = value; }}
		public string ResponseQueuePath { get { return m_ResponseQueuePath;} set { m_ResponseQueuePath = value; }}
		public string ReportQueuePath { get { return m_ReportQueuePath;} set { m_ReportQueuePath = value; }}
}
	public interface IRemotingResponse
	{
		void ResponseNotify(MsgSourceInfo src, object msg); // remoting notification
		void ResponseAck(object msg);													// remoting response notification
		void ResponseError(object msg, Exception ex);					// remoting exception notification
	}
	#endregion

	#region Constants Definitions
	public class MSMQChannelDefaults 
	{
		public const string ChannelName = "msmq";
		public const int    ChannelPriority = 1;
		public const int		TimeoutInSec = 60;
		public const int		RetryCounter = 0;
		public const int		MaxThreads = 20;									// max. number threads in the pool
		public const bool		CanBeUpdated = true;
		public const bool		UseTimeout = false;			  				// disable timeout over retry mechanism
		public const bool		UseHeaders = false;			  				// allow to pass a transport headers via msmq 
		public const bool		ACK = false;		
		public const string	EmptyStr = "";
		public const string	QueuePath = @".\Private$\ReqChannel";	
		public const string	EventLog	= "Application";				// Event Log Entry
		public const int		EventId	= 500;										// Event Id of the Channel 
		public const short	EventCategory	= 0;								// Event Category
	}

	public class MSMQChannelProperties 
	{	
		public const string ChannelName			= "name";
		public const string ChannelPriority = "priority";
		public const string Listener				= "listener";
		public const string NotifyTime			= "notifytime";
		public const string UseTimeout			= "usetimeout";
		public const string UseHeaders			= "useheaders";
		public const string RetryTime				= "retrytime";
		public const string Retry 					= "retry";
		public const string RetryFilter			= "retryfilter";
		public const string MaxThreads      = "maxthreads";
		public const string NotifyUrl       = "notifyurl";
		public const string AckUrl          = "acknowledgeurl";
		public const string ExceptionUrl    = "exceptionurl";
		public const string Timeout         = "timeout";
		public const string RcvAck					= "receiveack";
		public const string SendAck					= "sendack";
		public const string RcvTimeout      = "receivetimeout";
		public const string SendTimeout     = "sendtimeout";
		public const string AdminQueuePath  = "admin";
		public const string LURL            = "lurl";
		public const string Uri             = "__Uri";	
		public const string ObjectUri       = "__ObjectUri";				
		public const string MethodName      = "___MethodName";
		public const string MethodSignature = "___MethodSignature";
		public const string TypeName        = "___TypeName";
		public const string RetryCounter    = "___RetryCounter";
		public const string UpdateKB				= "updatekb";
	}
	#endregion

	#region CallContext Ticket
	[Serializable]
	public class LogicalTicket : NameValueCollection, ILogicalThreadAffinative
	{
		public LogicalTicket() { init(null); }
		public LogicalTicket(params string[] args) { init(args); }
		public LogicalTicket(IMessage imsg)
		{
			if(imsg != null) 
			{
				object obj = imsg.Properties["__CallContext"];
				if(obj != null && obj is LogicalCallContext) 
				{
					LogicalCallContext lcc = obj as LogicalCallContext;
					object ticket = lcc.GetData("___Ticket");
					if(ticket != null && ticket is LogicalTicket) 
					{
						LogicalTicket lt = ticket as LogicalTicket;
						for(int ii = 0; ii < lt.Count; ii++)
						{
							base[lt.GetKey(ii)] = lt.Get(ii);
						}
					}
				}	
			}
		}
		protected LogicalTicket(SerializationInfo info, StreamingContext context): base(info, context) {}
		public void Set() { CallContext.SetData("___Ticket", this);	}
		public void Reset() { CallContext.FreeNamedDataSlot("___Ticket"); }
		private void init(params string[] args)
		{
			object obj = CallContext.GetData("___Ticket"); 
			if(obj != null) 
			{
				LogicalTicket lt = obj as LogicalTicket;
				for(int ii = 0; ii < lt.Count; ii++)
				{
					base[lt.GetKey(ii)] = lt.Get(ii);
				}
			}	
			if(args != null) 
			{
				foreach(string s in args) 
				{
					string[] nv = s.Split(new char[]{'='}, 2);
					if(nv.Length != 2) 
						throw new Exception(string.Format("Wrong name/value pattern in the Ticket, arg={0}", s));
					base[nv[0].Trim()] = nv[1].Trim();
				}
			}
		}
	}
	#endregion

	#region helpers
	public sealed class RemotingConvert 
	{
		public static byte[] ToArray(IMessage imsg) 
		{
			byte[] buffer = null;

			//serialize IMessage
			BinaryFormatter bf = new BinaryFormatter();
			MemoryStream stream = new MemoryStream();
			bf.Serialize(stream, imsg);
			stream.Position = 0;

			//write stream to the buffer
			buffer = new byte[stream.Length];
			stream.Read(buffer, 0, buffer.Length);
			stream.Close();

			return buffer;
		}
		public static IMessage ToMessage(object msg) 
		{
			IMessage imsg = null;

			// deserialize IMessage
			if(msg is byte[]) 
			{
				byte[] buffer = msg as byte[];
				BinaryFormatter bf = new BinaryFormatter();
				MemoryStream stream = new MemoryStream();
				stream.Write(buffer, 0, buffer.Length);
				stream.Position = 0;
				imsg = (IMessage)bf.Deserialize(stream);
				stream.Close();
			}

			return imsg;
		}
	}
	#endregion	
}
