//=============================================================================
//	The MSMQChannel - Listener Custom Channel 
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
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Messaging;
using System.Runtime.InteropServices;
//
using RKiss.CustomChannel;
#endregion

namespace RKiss.MSMQChannel
{
	
	// Listener
	public class Listener : KnowledgeBase
	{
		#region delegates
		private delegate void MessageWorkerDelegator(object objThis); 
		#endregion

		#region private members
		private string					m_ChannelName;
		private int							m_ChannelPriority;
		private EventLog				m_EventLog = null;											// EventLog object
		private MessageQueue		m_InQueue = null;
		private string					m_ListenerPath = "";
		private volatile bool		m_Listening = false;
		private int							m_NumberOfWorkers = 0;  
		private MessageQueue		m_RetryQueue = null;
		private string					m_RetryQueuePath = "";
		private volatile bool		m_RetryListening = false;		
		private AutoResetEvent	m_EventMgr = new AutoResetEvent(true);	
		private AutoResetEvent	m_EventRetryMgr = new AutoResetEvent(false);	
		#endregion

		#region Properties		
		public string ListenerPath
		{
			get	{ return m_ListenerPath;	 }
			set	{ m_ListenerPath = value; }
		}
		public string RetryQueuePath
		{
			get	{ return m_RetryQueuePath;	 }
			set	{ m_RetryQueuePath = value; }
		}
		public bool UseTimeout
		{
			get	{ return Convert.ToBoolean(KB[MSMQChannelProperties.UseTimeout]);	 }
			set	{ KB[MSMQChannelProperties.UseTimeout] = value; }
		}	
		public int RetryCounter
		{
			get	{ return Convert.ToInt32(KB[MSMQChannelProperties.Retry]); }
			set	{ KB[MSMQChannelProperties.Retry] = value; }
		}
		public int RetryTime
		{
			get	{ return Convert.ToInt32(KB[MSMQChannelProperties.RetryTime]); }
			set	{ KB[MSMQChannelProperties.RetryTime] = value; }
		}
		public string RetryFilter
		{
			get	{ return Convert.ToString(KB[MSMQChannelProperties.RetryFilter]);	 }
			set	{ KB[MSMQChannelProperties.RetryFilter] = value; }
		}
		public int MaxNumberOfWorkers
		{
			get	{ return Convert.ToInt32(KB[MSMQChannelProperties.MaxThreads]);	 }
			set	{ KB[MSMQChannelProperties.MaxThreads] = value; }
		}
		public int NotifyTime
		{
			get	{ return Convert.ToInt32(KB[MSMQChannelProperties.NotifyTime]);	 }
			set	{ KB[MSMQChannelProperties.NotifyTime] = value; }
		}
		public string NotifyUrl
		{
			get	{ return Convert.ToString(KB[MSMQChannelProperties.NotifyUrl]);	 }
			set	{ KB[MSMQChannelProperties.NotifyUrl] = value; }
		}
		public string AcknowledgeUrl
		{
			get	{ return Convert.ToString(KB[MSMQChannelProperties.AckUrl]);	 }
			set	{ KB[MSMQChannelProperties.AckUrl] = value; }
		}
		public string ExceptionUrl
		{
			get	{ return Convert.ToString(KB[MSMQChannelProperties.ExceptionUrl]);	 }
			set	{ KB[MSMQChannelProperties.ExceptionUrl] = value; }
		}
		#endregion

		#region constructor
		public Listener() 
		{ 
		}
		#endregion

		#region destructor
		~Listener() 
		{
			Stop();
		}
		#endregion

		#region Init
		public void Init()
		{
			// open Listener queue
			m_InQueue = new MessageQueue(ListenerPath);
			m_InQueue.Formatter = new BinaryMessageFormatter(); 
			m_InQueue.MessageReadPropertyFilter.AcknowledgeType = true;
			m_InQueue.MessageReadPropertyFilter.TimeToBeReceived = true;
			m_InQueue.MessageReadPropertyFilter.SourceMachine = true;
			m_InQueue.MessageReadPropertyFilter.SentTime = true;
			m_InQueue.MessageReadPropertyFilter.ResponseQueue = true;
			m_InQueue.PeekCompleted += new PeekCompletedEventHandler(Manager);
			m_Listening = true;
			m_InQueue.BeginPeek();
		
			// both queue have to be transactional for retry mechanism
			if(m_InQueue.Transactional) 
			{ 
				// retry queue path
				m_RetryQueuePath = ListenerPath + "_retry";
				m_RetryQueue = new MessageQueue(m_RetryQueuePath);
				if(m_RetryQueue.Transactional) 
				{
					m_RetryQueue.Formatter = new BinaryMessageFormatter(); 
					m_RetryQueue.MessageReadPropertyFilter.ArrivedTime = true;
					m_RetryQueue.PeekCompleted += new PeekCompletedEventHandler(RetryManager);
					m_RetryListening = true;
					m_RetryQueue.BeginPeek();	
				}
				else 
				{
					m_RetryQueue.Close();
					m_RetryQueue = null;
				}
			}
		}
		#endregion
		
		#region control methods
		public virtual void Stop()
		{
			try 
			{
				WriteLogMsg(string.Format("[{0}]MSMQChannel.Receiver: StopListening...", ChannelName), EventLogEntryType.Information);
	
				// retry manager
				m_RetryListening = false;
				//m_EventRetryMgr.Set();
				if(m_RetryQueue != null) 
				{
					m_RetryQueue.PeekCompleted -= new PeekCompletedEventHandler(RetryManager);
					m_RetryQueue.Close();
				}

				// main manager
				m_Listening = false;
				//m_EventMgr.Set();
				if(m_InQueue != null) 
				{
					m_InQueue.PeekCompleted -= new PeekCompletedEventHandler(Manager);
					m_InQueue.Close();
				}
			}
			catch(Exception ex) 
			{
				string strErr = string.Format("[{0}]MSMQChannel.Receiver:Stop failed, error = {1}", ChannelName, ex.Message);
				WriteLogMsg(strErr, EventLogEntryType.Error);
			}
			finally 
			{
				WriteLogMsg(string.Format("[{0}]MSMQChannel.Receiver: StopListening done.",ChannelName), EventLogEntryType.Information);
			}
		}
		public virtual void Start()
		{
			if(m_Listening == false) 
			{
				Init();
				WriteLogMsg(string.Format("[{0}]MSMQChannel.Receiver: Listening...",ChannelName), EventLogEntryType.Information);
			}
		}
		#endregion

		#region WriteLogMsg
		public void WriteLogMsg(string strLogMsg, EventLogEntryType typeEvent) 
		{
			Process thisProcess = Process.GetCurrentProcess();
			strLogMsg = string.Format("{0} Process = {1}/{2}", strLogMsg, thisProcess.ProcessName, thisProcess.Id);
			Trace.WriteLine(strLogMsg);

			try 
			{
				if(m_EventLog == null) 
				{
					m_EventLog = new EventLog();
					m_EventLog.Source = thisProcess.ProcessName;
					m_EventLog.Log = MSMQChannelDefaults.EventLog;
				}

				m_EventLog.WriteEntry(strLogMsg, typeEvent, MSMQChannelDefaults.EventId, MSMQChannelDefaults.EventCategory);
			}
			catch(Exception ex) 
			{
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver: WriteLogMsg error = {1}", ChannelName, ex.Message));
			}
		}
		#endregion

		#region Knowledge base 
		public override void Remove(string name){ throw new NotSupportedException(); }
		public override void RemoveAll() { throw new NotSupportedException();	}
		public override void Store(string strLURL, bool bOverwrite)	{	throw new NotSupportedException(); }
		public override void Store(string name, string val, bool bOverwrite) {	throw new NotSupportedException(); }
		public override void OnChangedKnowledgeBase(string name, string val) 
		{
			if(name != null && name == MSMQChannelProperties.MaxThreads) 
			{
				// update manager state
				Interlocked.Increment(ref m_NumberOfWorkers);
				if(Interlocked.Decrement(ref m_NumberOfWorkers) < MaxNumberOfWorkers) 
				{	
					
					// signal to the threadpool controller
					m_EventMgr.Set();

					// make a message pump
					if(m_InQueue != null && m_Listening == false) 
					{
						m_Listening = true;
						m_InQueue.BeginPeek();
					}
				}
				else 
					m_EventMgr.Reset();
			}
			if(name != null && name == MSMQChannelProperties.RetryTime) 
			{
				// update a retry manager state
				m_EventRetryMgr.Set();
			}
			
		}
		public override bool OnBeforeChangeKnowledgeBase(string name, string val)
		{
			// validation of the selected properties
			if(name == null || name == "")
				throw new Exception("The name is not recognize (null/empty).");  

			if(name == MSMQChannelProperties.MaxThreads && MSMQChannelDefaults.MaxThreads < Convert.ToInt32(val)) 
				throw new Exception(string.Format("Max number of threads can be {0}", MSMQChannelDefaults.MaxThreads));  
			else 
			if(name == MSMQChannelProperties.Retry || 
				 name == MSMQChannelProperties.RetryTime ||
				 name == MSMQChannelProperties.NotifyTime) 
				Convert.ToInt32(val);
			else if(name == MSMQChannelProperties.UseTimeout) 
				Convert.ToBoolean(val);
			
			return true;
		}
		#endregion
	
		#region RetryManager
		private void RetryManager(object source, PeekCompletedEventArgs asyncResult)
		{
			// the message is going to move using the transactional manner
			MessageQueueTransaction mqtx = null;

			// Connect to the queue.
			MessageQueue mq = (MessageQueue)source;

			try 
			{				
				// End the asynchronous peek operation.
				Message msg = mq.EndPeek(asyncResult.AsyncResult);

				// check the message lifetime
				DateTime elapsedTime = msg.ArrivedTime.AddSeconds(RetryTime);

				if(DateTime.Compare(DateTime.Now, elapsedTime) >= 0) 
				{
					// it's the time to move it
					// create destination queue
					MessageQueue InQueue = new MessageQueue(ListenerPath);
					InQueue.Formatter = new BinaryMessageFormatter(); 
					mqtx = new MessageQueueTransaction();

					// echo 
					Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.RetryManager - Moveing message \"{1}\"", ChannelName, msg.Label));

					// move it
					mqtx.Begin();			
					Message msgToMove = mq.Receive(new TimeSpan(0,0,0,1));
					InQueue.Send(msgToMove, mqtx);
					mqtx.Commit();
				}
				else 
				{
					// it's the time to sleep
					TimeSpan deltaTime = elapsedTime.Subtract(DateTime.Now);

					// the sleep time
					int sleeptime =  Convert.ToInt32(deltaTime.TotalMilliseconds);

					// echo 
					Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.RetryManager - Sleeping for {1}ms", ChannelName, sleeptime));
					
					// wait for timeout or signal such as change retrytime value or shutdown process
					m_EventRetryMgr.WaitOne(sleeptime, true);
				}		
			}
			catch(Exception ex) 
			{
				if(mqtx != null && mqtx.Status == MessageQueueTransactionStatus.Pending) 
					mqtx.Abort();

				string strErr = string.Format("[{0}]MSMQChannel.Receiver:RetryMamager failed, error = {1}", ChannelName, ex.Message);
				WriteLogMsg(strErr, EventLogEntryType.Error);
			}
			finally 
			{
				// Restart the asynchronous peek operation
				if(m_RetryListening == true)
					mq.BeginPeek();	
				else
					Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.RetryManager has been disconnected", ChannelName));	
			}
		}
		#endregion
		
		#region Manager
		private void Manager(object source, PeekCompletedEventArgs asyncResult)
		{
			// Connect to the queue.
			MessageQueue mq = source as MessageQueue;

			try 
			{
				// The manager has responsibility to delegate a work to the receiver for incomming messages
				// The number of the workers can be managed administratively or on the fly.

				// End the asynchronous peek operation.
				mq.EndPeek(asyncResult.AsyncResult);
				
				// threadpool controller
				while(true) 
				{		
					// wait for condition (workers are ready, etc.)
					if(m_EventMgr.WaitOne(NotifyTime * 1000, true) == false) 
					{
						// timeout handler
						if(MaxNumberOfWorkers > 0) 
						{
							string strWarning = string.Format("[{0}]MSMQChannel.Receiver: All Message Workers are busy", ChannelName);
							WriteLogMsg(strWarning, EventLogEntryType.Warning);

							// threadpool is closed, try again
							continue;
						}
						else 
						{
							m_Listening = false;
							return;
						}
					}
					
					// threadpool is open
					break;
				}

				// delegate work to the worker
				MessageWorkerDelegator mwd = new MessageWorkerDelegator(MessageWorker);
				mwd.BeginInvoke(source, null, null);
				
			}
			catch(Exception ex) 
			{
				string strErr = string.Format("[{0}]MSMQChannel.Receiver:Mamager failed, error = {1}", ChannelName, ex.Message);
				WriteLogMsg(strErr, EventLogEntryType.Error);
			}
			finally 
			{
				// Restart the asynchronous peek operation
				if(m_Listening == true)
					mq.BeginPeek();	
				else
					Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.Manager has been disconnected", ChannelName));	
			}
		}
		#endregion
		
		#region MessageWorker
		public void MessageWorker(object source)
		{
			// scope state
			int intNumberOfWorkers = 0;	
			bool bTransactional = true;
			Exception exception = null;
			IMessage msgReq = null;
			IMessage msgRsp = null;
			Message msg = null;	
			MessageQueue mq = source as MessageQueue;
			MessageQueueTransaction mqtx = new MessageQueueTransaction();

			try 
			{	
				// check-in
				intNumberOfWorkers = Interlocked.Increment(ref m_NumberOfWorkers);
			
				// transactional flag
				bTransactional = mq.Transactional;

				// echo
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver:MessageWorker check-in #{1}, isTxQueue={2}", 
					ChannelName, intNumberOfWorkers, bTransactional));

				// begin single transaction 
				if(bTransactional)
					mqtx.Begin();

				// retrieve transactional message (message has been retrieved from the queue)
				msg = mq.Receive(TimeSpan.FromSeconds(1), mqtx);

				// signal to manager
				if(intNumberOfWorkers < MaxNumberOfWorkers)
					m_EventMgr.Set();
				else 
					m_EventMgr.Reset();
				
				// remoting message from the client
				msgReq = msg.Body as IMessage;

				// work around!!!
				msgReq.Properties[MSMQChannelProperties.Uri] = msgReq.Properties[MSMQChannelProperties.ObjectUri];	

				// option: target object 
				if(NotifyUrl == MSMQChannelDefaults.EmptyStr) 
				{
					// Dispatch message to the remoting object
					msgRsp = MessageDispatcher(msgReq, msg.BodyStream);
				}
				else 
				{
					// pack the source info
					MsgSourceInfo src = new MsgSourceInfo();
					src.Acknowledgment = msg.Acknowledgment;
					src.MessageSentTime = msg.SentTime;
					src.ResponseQueuePath =  msg.ResponseQueue == null ? "null" : msg.ResponseQueue.Path;
					src.ReportQueuePath = mq.Path;

					// Call Notification Object
					MessageDispatcher_NOTIFY(src, msgReq);
				}

				// commit transaction (message is going to be removed from the queue)
				if(bTransactional)
					mqtx.Commit();

				// echo 
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.MessageWorker The call has been invoked on {1}", 
					ChannelName, msg.Label));
				
				// response
				if(AcknowledgeUrl != MSMQChannelDefaults.EmptyStr) 
				{
					// workaround
					if(msgRsp != null) 
					{
						msgRsp.Properties[MSMQChannelProperties.Uri]        = msgReq.Properties[MSMQChannelProperties.Uri];
						msgRsp.Properties[MSMQChannelProperties.MethodName] = msgReq.Properties["__MethodName"];
						msgRsp.Properties[MSMQChannelProperties.TypeName]   = msgReq.Properties["__TypeName"];
					}

					// remoting notification 
					MessageDispatcher_ACK(msgRsp == null ? msgReq : msgRsp);
				}
			}
			catch(MessageQueueException ex) 
			{
				// save for a notification issue
				exception = ex;

				// echo
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver:MessageWorker error/warning = {1}", ChannelName, ex.Message));
			}
			catch(Exception ex) 
			{
				// save for a notification issue
				exception = ex;

				// echo
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver:MessageWorker error/warning = {1}", ChannelName, ex.Message));
			}
			finally
			{
				bool bRetryDone = false;

				if(bTransactional && mqtx.Status == MessageQueueTransactionStatus.Pending) 
				{	
					// check the retry filter
					int posRules = (RetryFilter == MSMQChannelDefaults.EmptyStr) ? 0 : RetryFilter.IndexOf(exception.Message);
					if(posRules < 0 && exception.InnerException != null)
						posRules = RetryFilter.IndexOf(exception.InnerException.Message);

					// send message to the retry queue
					if(msg != null && msgReq != null && RetryCounter > 0 && posRules < 0) 
					{
						// remoting notification 
						bRetryDone = MessageDispatcher_RETRY(mqtx, msg, msgReq);
					}

					// commit transaction (message is going to be removed from the queue)
					mqtx.Commit();
				}
				
				// exception notification
				if(exception != null && msg != null && msgReq != null && bRetryDone == false && ExceptionUrl != MSMQChannelDefaults.EmptyStr) 
				{
					// remoting notification 
					MessageDispatcher_ERROR(msgReq, exception);
				}

				// clean-up
				if(msg != null) 
					msg.Dispose();

				// check-out
				intNumberOfWorkers = Interlocked.Decrement(ref m_NumberOfWorkers);
				if(intNumberOfWorkers < MaxNumberOfWorkers)
					m_EventMgr.Set();
				else 
					m_EventMgr.Reset();

				// echo
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver:MessageWorker check-out #{1}, isTxQueue={2}", 
					ChannelName, intNumberOfWorkers, bTransactional));		
			}
		}
		#endregion

		#region MessageDispatcher
		private IMessage MessageDispatcher(IMessage iMsgReq, Stream requestStream) 
		{
			IMessage iMsgRsp = null;
			
			if(iMsgReq.Properties["__Uri"] != null) 
			{ 
				// parse url address
				string strObjectUrl = iMsgReq.Properties["__Uri"].ToString();	
				string[] urlpath = strObjectUrl.Split(';');	
				string[] s = urlpath[0].Split('/');							

				// check endpoint
				if(urlpath.Length == 1 && s.Length == 1) 
				{
					//this is an end channel
					Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher: Local endpoint={1}", 
						ChannelName, strObjectUrl));

					// channel
					Receiver rcv = this as Receiver;

					// stack
					ServerChannelSinkStack stack = new ServerChannelSinkStack();
					stack.Push(rcv.TransportSink, null);

					// request (workaround)
					ITransportHeaders requestHeaders = null;
					if(iMsgReq.Properties.Contains("__RequestHeaders")) 
					{
						requestHeaders = iMsgReq.Properties["__RequestHeaders"] as ITransportHeaders; 
					}
					else 
					{
						requestHeaders = new TransportHeaders();
						requestHeaders["__ContentType"] = "application/octet-stream";
					}
					requestHeaders["__RequestUri"] = "/" + strObjectUrl;
					requestStream.Position = 0;

					// response
					Stream responseStream = null;
					ITransportHeaders responseHeaders = null;

					// skip the transport sink
					rcv.TransportSink.NextChannelSink.ProcessMessage(stack, null, requestHeaders, requestStream, 
						out iMsgRsp, out responseHeaders, out responseStream);
				}
				else									
				{
					// chaining channel
					string strDummy = null;
					IMessageSink sink = null;

					// find a properly channel
					foreach(IChannel ch in ChannelServices.RegisteredChannels)
					{					
						if(ch is IChannelSender)
						{
							IChannelSender iChannelSender = ch as IChannelSender;
							sink = iChannelSender.CreateMessageSink(strObjectUrl, null, out strDummy);
							if(sink != null)
							{
								//this is a next channel
								Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher: Chained channel is url={1}", 
									ChannelName, strObjectUrl));
								break;				
							}
						}
					}

					if(sink == null)
					{
						//no channel found it
						string strError = string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher: A supported channel could not be found for {1}", 
							ChannelName, strObjectUrl);
						iMsgRsp = new ReturnMessage(new Exception(strError), (IMethodCallMessage)iMsgReq);
						Trace.WriteLine(strError);
					}
					else 
					{
						//check for an oneway attribute
						IMethodCallMessage mcm = iMsgReq as IMethodCallMessage;
					
						if(RemotingServices.IsOneWay(mcm.MethodBase) == true)
							iMsgRsp = (IMessage)sink.AsyncProcessMessage(iMsgReq, null);
						else
							iMsgRsp = sink.SyncProcessMessage(iMsgReq);
					}
				}
			}
			else
			{
				//exception
				Exception ex = new Exception(string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher: The Uri address is null", ChannelName));
				iMsgRsp = new ReturnMessage(ex, (IMethodCallMessage)iMsgReq);
			}

			// check the response message
			if(iMsgRsp != null) 
			{
				IMethodReturnMessage mrm = iMsgRsp as IMethodReturnMessage;
				if(mrm.Exception != null)
					throw mrm.Exception;
			}

			return iMsgRsp;
		}
		#endregion

		#region MessageDispatcher_NOTIFY
		private void MessageDispatcher_NOTIFY(MsgSourceInfo src, IMessage imsg) 
		{
			try 
			{
				// convert remoting message to the array
				byte[] buffer = RemotingConvert.ToArray(imsg);
								
				// notify
				IRemotingResponse ro = (IRemotingResponse)Activator.GetObject(typeof(IRemotingResponse), NotifyUrl);
				ro.ResponseNotify(src, buffer);

				// echo
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher_NOTIFY done, url={1}",
					ChannelName, NotifyUrl));
			}
			catch(Exception ex) 
			{
				string strErr = string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher_NOTIFY failed at url={1}, error={2};",
					ChannelName, NotifyUrl, ex.Message);
				WriteLogMsg(strErr, EventLogEntryType.Error);
			}
		}
		#endregion

		#region MessageDispatcher_ACK
		private void MessageDispatcher_ACK(IMessage imsg) 
		{
			try 
			{
				// convert remoting message to the array
				byte[] buffer = RemotingConvert.ToArray(imsg);
				
				// notify
				IRemotingResponse ro = (IRemotingResponse)Activator.GetObject(typeof(IRemotingResponse), AcknowledgeUrl);
				ro.ResponseAck(buffer);

				// echo
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher_ACK done, url={1}",
					ChannelName, AcknowledgeUrl));
			}
			catch(Exception ex) 
			{
				string strErr = string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher_ACK failed at url={1}, error={2};",
					ChannelName, AcknowledgeUrl, ex.Message);
				WriteLogMsg(strErr, EventLogEntryType.Error);
			}
		}
		#endregion

		#region MessageDispatcher_ERROR
		private void MessageDispatcher_ERROR(IMessage imsg, Exception error) 
		{
			try 
			{
				// convert remoting message to the array
				byte[] buffer = RemotingConvert.ToArray(imsg);
				
				// notify
				IRemotingResponse ro = (IRemotingResponse)Activator.GetObject(typeof(IRemotingResponse), ExceptionUrl);
				ro.ResponseError(buffer, error);

				// echo
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher_ERROR done, url={1}",
					ChannelName, ExceptionUrl));
			}
			catch(Exception ex) 
			{
				string strErr = string.Format("[{0}]MSMQChannel.Receiver.MessageDispatcher_ERROR failed at url={1}, error={2};",
					ChannelName, ExceptionUrl, ex.Message);
				WriteLogMsg(strErr, EventLogEntryType.Error);
			}
		}
		#endregion

		#region MessageDispatcher_RETRY
		private bool MessageDispatcher_RETRY(MessageQueueTransaction mqtx, Message msg, IMessage imsgReq) 
		{
			// scope state
			bool result = false;
			int intRetryCounter = 0;
			MessageQueue RetryQueue = null;

			try 
			{ 
				// check the message state (retry property)
				if(imsgReq.Properties.Contains(MSMQChannelProperties.RetryCounter) && 
					(imsgReq.Properties[MSMQChannelProperties.RetryCounter] is int)) 
				{
					intRetryCounter = Convert.ToInt32(imsgReq.Properties[MSMQChannelProperties.RetryCounter]);
				}

				// check the retry progress
				if(intRetryCounter < RetryCounter) 
				{
					// update message state
					imsgReq.Properties[MSMQChannelProperties.RetryCounter] = ++intRetryCounter;

					// open the queue
					RetryQueue = new MessageQueue();
					RetryQueue.Path = m_RetryQueuePath;
					RetryQueue.Formatter = new BinaryMessageFormatter(); 

					// create message label 
					string label = string.Format("Retry*{0}", msg.Label);

					// option: disable timeout, now the message has a lifetime forever
					if(UseTimeout == false)
						msg.TimeToBeReceived =  Message.InfiniteTimeout;

					// send message (move message to the waiting queue, after mqtx.commit of course)
					RetryQueue.Send(msg, label, mqtx);

					// echo
					Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver:MessageDispatcher_RETRY done for {1}x", ChannelName, intRetryCounter-1));

					// result (retry is in progress)
					result = true;
				}
			}
			catch(Exception ex) 
			{
				Trace.WriteLine(string.Format("[{0}]MSMQChannel.Receiver:MessageDispatcher_RETRY failed, error={1}", ChannelName, ex.Message));
			}
			finally 
			{
				if(RetryQueue != null) 
					RetryQueue.Close();			
			}	
	
			return result;
		}
		#endregion

		#region IChannel
		public string ChannelName
		{
			get	{ return m_ChannelName; }
			set	{ m_ChannelName = value; }
		}
		public int ChannelPriority
		{
			get	{ return m_ChannelPriority;	 }
			set	{ m_ChannelPriority = value; }
		}	
	
		public string Parse(string url, out string objectURI)
		{
			objectURI = null;
			return url;
		}
		#endregion	
	}	
}
