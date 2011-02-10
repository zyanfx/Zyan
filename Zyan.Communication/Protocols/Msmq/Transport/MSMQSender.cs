//======================================================================================================
//	The library for Custom Remoting via MSMQ channel (Sender).
//	(C) Copyright 2001, Roman Kiss (rkiss@pathcom.com)
//	All rights reserved.
//	The code and information is provided "as-is" without waranty of any kind, either expresed or implied.
//------------------------------------------------------------------------------------------------------
//	History:
//			12-05-2001	RK	Initial Release	
//======================================================================================================
//
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Messaging;
using System.Runtime.InteropServices;


namespace RKiss.MSMQChannelLib
{
	public delegate void delegateAsyncWorker(IMessage msgReq, IMessageSink replySink); 

	// Sender
	public class MSMQSender : IChannelSender, IChannel
	{	
		// these are a hardcoded default values
		const int PATH_MINNUMOFFIELDS = 5;
		public const int DEFAULT_TIMEOUTINSEC = 60;
		public const string DEFAULT_RESPONDCHANNEL = @".\RspChannel";
		public const string DEFAULT_ADMINCHANNEL = @".\AdminChannel";
		public const string DEFAULT_CHANNELNAME = "msmq";
		public const int    DEFAULT_CHANNELPRIORITY = 1;
		
		//
		private string m_ChannelName;
		private int m_ChannelPriority;
		private TimeSpan m_ResponseTimeOutInSec;
		private MessageQueue m_AdminQueue = null;
		private MessageQueue m_ResponseQueue = null;

		// MSMQSender
		public MSMQSender() : 
			this(DEFAULT_CHANNELNAME, DEFAULT_RESPONDCHANNEL, DEFAULT_ADMINCHANNEL, DEFAULT_TIMEOUTINSEC, DEFAULT_CHANNELPRIORITY) {}
		public MSMQSender(string channelName) : 
			this(channelName, DEFAULT_RESPONDCHANNEL, DEFAULT_ADMINCHANNEL, DEFAULT_TIMEOUTINSEC, DEFAULT_CHANNELPRIORITY) {}
		public MSMQSender(string channelName, string pathRQ) : 
			this(channelName, pathRQ, DEFAULT_ADMINCHANNEL, DEFAULT_TIMEOUTINSEC, DEFAULT_CHANNELPRIORITY) {}
		public MSMQSender(string channelName, string pathRQ, string pathAQ) : 
			this(channelName, pathRQ, pathAQ, DEFAULT_TIMEOUTINSEC, DEFAULT_CHANNELPRIORITY) {}
		public MSMQSender(string channelName, string pathRQ, string pathAQ, int timeout) : 
			this(channelName, pathRQ, pathAQ, timeout, DEFAULT_CHANNELPRIORITY) {}
		public MSMQSender(string channelName, string pathRQ, string pathAQ, int timeout, int priority)
		{
			Init(channelName, pathRQ, pathAQ, timeout, priority);
		}
		public MSMQSender(IDictionary properties, IServerChannelSinkProvider serverSinkProvider) 
		{
			string pathRQ  = DEFAULT_RESPONDCHANNEL;
			string pathAQ = null;	
			int	timeout	= DEFAULT_TIMEOUTINSEC;
			string channelName = DEFAULT_CHANNELNAME;
			int channelPriority = DEFAULT_CHANNELPRIORITY;

			// administratively setup the config values (xxx.exe.config file)
			if(properties.Contains("name")) 
				channelName = properties["name"].ToString();
			if(properties.Contains("priority")) 
				channelPriority = Convert.ToInt32(properties["priority"]);
			if(properties.Contains("respond")) 
				pathRQ = properties["respond"].ToString();
			if(properties.Contains("admin")) 
				pathAQ = properties["admin"].ToString();
			if(properties.Contains("timeout")) 
				timeout = Convert.ToInt32(properties["timeout"]);

			Init(channelName, pathRQ, pathAQ, timeout, channelPriority);

			Trace.WriteLine(string.Format("Client-config: name={0}, respQ={1}, adminQ={2}, timeout={3}, priority={4}", 
										  m_ChannelName, pathRQ, pathAQ, timeout, m_ChannelPriority));
		}
		public void Init(string channelName, string pathRQ, string pathAQ, int timeout, int priority)
		{
			m_ChannelName = channelName;
			m_ChannelPriority = priority;
			long ticks = (timeout == Timeout.Infinite) ? TimeSpan.MaxValue.Ticks : timeout * TimeSpan.TicksPerSecond; 
			m_ResponseTimeOutInSec = new TimeSpan(ticks);
					
			// config an admin queue
			if(pathAQ != null) 
			{
				m_AdminQueue = new MessageQueue(pathAQ);
				m_AdminQueue.Formatter = new BinaryMessageFormatter();
			}
							
			// open incoming (Response) queue;
			m_ResponseQueue = new MessageQueue(pathRQ);		
			m_ResponseQueue.Formatter =  new BinaryMessageFormatter();
			m_ResponseQueue.MessageReadPropertyFilter.Body = true;
			m_ResponseQueue.MessageReadPropertyFilter.CorrelationId = true;
		}
		// access to the private members
		public MessageQueue ResponseQueue	{ get { return m_ResponseQueue; }}
		public MessageQueue AdminQueue		{ get { return m_AdminQueue; }}
		public TimeSpan ResponseTimeOut		{ get { return m_ResponseTimeOutInSec; }}

		// IChannelSender
		public string ChannelName	{ get { return m_ChannelName; } }
		public int ChannelPriority	{ get { return m_ChannelPriority; }	}
		public string Parse(string url, out string objectURI) { return objectURI = null; }

		// IChannelSender (activation)
		public virtual IMessageSink CreateMessageSink(String url, Object data, out string objectURI)
		{
			objectURI = null;
			string[] s = url.Split(new Char[]{'/'});

			if(s.Length < PATH_MINNUMOFFIELDS || m_ChannelName + ":" != s[0])
				return null;		// this is not correct channel
			
			string outpath = "";
			for(int ii = 2; ii < s.Length-1; ii++) 
			{
				outpath += s[ii]; 
				outpath += "\\"; 	
			}
			outpath = outpath.TrimEnd(new Char[]{'\\'});
			objectURI = s[s.Length-1];

			MSMQMessageSink msgsink = new MSMQMessageSink(this, objectURI, outpath);
			Trace.WriteLine(string.Format("Client-CreateMessageSink: url = {0}, sink = {1}", url, msgsink.GetHashCode()));
			return msgsink;			
		}
	}//MSMQSender



	// MSMQMessageSink
	public class MSMQMessageSink : IMessageSink, IDictionary
	{
		private const string OBJECTURI = "__objectUri";
		private MSMQSender		m_parent = null; 
		private MessageQueue	m_OutQueue = null;
		private string			m_ObjectUri;
		
		// MSMQMessageSink
		public MSMQMessageSink(MSMQSender parent, string objuri, string outpath) 
		{
			m_parent = parent;
			m_ObjectUri = objuri;
	
			// open an outgoing queue for the sync or async Calls
			m_OutQueue = new MessageQueue(outpath);	
			m_OutQueue.Formatter = new BinaryMessageFormatter();
			m_OutQueue.DefaultPropertiesToSend.AdministrationQueue = m_parent.AdminQueue;
			m_OutQueue.DefaultPropertiesToSend.AcknowledgeType = AcknowledgeTypes.NegativeReceive;
			m_OutQueue.DefaultPropertiesToSend.TimeToBeReceived = m_parent.ResponseTimeOut;
		}
		// helper
		// this is a work around for the ReceiveByCorrelationId function  
		// to prevent overwriting some properties in the read filter 
		private IMessage ReceiveReturnMessage(string cookie)
		{
			MessageQueueTransaction mqtx = new MessageQueueTransaction();
			MessageQueue ResponseQueue = new MessageQueue(m_parent.ResponseQueue.Path);		
			ResponseQueue.Formatter = new BinaryMessageFormatter();
			
			while(true) 
			{
				try
				{
					mqtx.Begin();
					ResponseQueue.MessageReadPropertyFilter.Body = true;
					ResponseQueue.MessageReadPropertyFilter.CorrelationId = true;

					Message msg = ResponseQueue.ReceiveByCorrelationId(cookie, m_parent.ResponseTimeOut, mqtx);
					IMessage imsg = msg.Body as IMessage;
					mqtx.Commit();
					ResponseQueue.Close();
					return imsg;
				}
				catch (MessageQueueException ex) 
				{
					mqtx.Abort();
                    if(ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
						throw ex;
				}
				catch(Exception ex) 
				{
					mqtx.Abort();
					Trace.WriteLine(string.Format("Client:ReceiveReturnMessage error = {0},  msgId = {1}", ex.Message, cookie));
				}
			}
		}
		
		// handler of the AsyncProcessMessage worker
		private void handlerAsyncWorker(IMessage msgReq, IMessageSink replySink) 
		{
			AsyncResult ar = replySink.NextSink as AsyncResult;
			// call Remote Method and wait for its Return Value 
			IMessage msgRsp = SyncProcessMessage(msgReq);
			// update AsyncResult state (_replyMsg, IsCompleted, waitstate, etc.)
			replySink.SyncProcessMessage(msgRsp);
		}
	
		// IMessageSink (MethodCall)
		public virtual IMessage SyncProcessMessage(IMessage msgReq)
		{	
			IMessage msgRsp = null;
			Message outMsg = null;
			MessageQueueTransaction mqtx = new MessageQueueTransaction();

			try 
			{
				msgReq.Properties[OBJECTURI] = m_ObjectUri;					// work around!
				// send a remoting message 
				mqtx.Begin();
				outMsg = new Message(msgReq, new BinaryMessageFormatter());	// create a message
				outMsg.ResponseQueue = m_parent.ResponseQueue;				// response queue
				outMsg.TimeToBeReceived = m_parent.ResponseTimeOut;			// timeout to pick-up a message
				outMsg.AcknowledgeType = AcknowledgeTypes.NegativeReceive;	// notify negative receive on the client/server side
				outMsg.AdministrationQueue = m_parent.AdminQueue;			// admin queue for a time-expired messages
				string label = msgReq.Properties["__TypeName"] + "." + msgReq.Properties["__MethodName"];
				m_OutQueue.Send(outMsg, label, mqtx);						// transactional message			
				mqtx.Commit();
				Trace.WriteLine(string.Format("Client-Sync:PRE-CALL, msgId={0}", outMsg.Id));
				
				// Wait for a Return Message
				Thread.Sleep(0);
				msgRsp = ReceiveReturnMessage(outMsg.Id);
				Trace.WriteLine(string.Format("Client-Sync:POST-CALL, msgId={0}", outMsg.Id));
			}
			catch (MessageQueueException ex) 
			{
				Trace.WriteLine(string.Format("Client:SyncProcessMessage error = {0}, msgId = {1}", ex.Message, outMsg.Id));
			}
			catch(Exception ex) 
			{
				Trace.WriteLine(string.Format("Client:SyncProcessMessage error = {0}, msgId = {1}", ex.Message, outMsg.Id));
			}
			finally
			{
				if(mqtx.Status == MessageQueueTransactionStatus.Pending) 
				{	
					mqtx.Abort();
					Trace.WriteLine(string.Format("Client:SyncProcessMessage Aborted, msgId = {0}", outMsg.Id));
				}
				m_OutQueue.Close();
			}
			
			return msgRsp;
		}

		public virtual IMessageCtrl AsyncProcessMessage(IMessage msgReq, IMessageSink replySink)
		{
			IMessageCtrl imc = null;

			if(replySink == null)									// OneWayAttribute
			{	
				MessageQueueTransaction mqtx = new MessageQueueTransaction();
				
				try 
				{
					mqtx.Begin();
					msgReq.Properties[OBJECTURI] = m_ObjectUri;		// work around!
					string label = msgReq.Properties["__TypeName"] + "." + msgReq.Properties["__MethodName"];
					m_OutQueue.Send(msgReq, label, mqtx);			// transacional message
					mqtx.Commit();
					Trace.WriteLine("Client-[OneWay]Async:CALL");
				}
				catch(Exception ex) 
				{
					Trace.WriteLine(string.Format("Client:AsyncProcessMessage error = {0}", ex.Message));
				}
				finally 
				{
					if(mqtx.Status == MessageQueueTransactionStatus.Pending) 
					{	
						mqtx.Abort();
					}
					m_OutQueue.Close();
				}
			}
			else
			{
				// spawn thread (delegate work)
				delegateAsyncWorker daw = new delegateAsyncWorker(handlerAsyncWorker);
				daw.BeginInvoke(msgReq, replySink, null, null);
			}
			
			return imc;
		}
		        
		// IDictionary (not implemented)
		public virtual IMessageSink NextSink {	get { return null; } }
		public virtual bool Contains(Object key) { return false; }
		public virtual void Add(Object key, Object value){}
		public virtual void Remove(Object key){}
		public virtual void Clear()	{}
		public virtual IDictionaryEnumerator GetEnumerator() { return null;	}
		IEnumerator IEnumerable.GetEnumerator()	{ return null; }
		public int Count { get { return 0; }}
		public void CopyTo(Array array, int index){}
		public ICollection Keys { get { return null; }}
		public ICollection Values { get { return null; }}
		public Object SyncRoot { get { return null; }}
		public  bool IsReadOnly { get { return true; }}
		public  bool IsFixedSize { get { return true; }}
		public bool IsSynchronized { get { return true;	}}
		public Object this[Object key] { get { return null; } set {} }
	}// MSMQMessageSink
}//namespace RKiss.MSMQchannelLib


