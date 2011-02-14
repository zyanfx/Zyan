//=============================================================================
//	The MSMQChannel - Client Channel Sink Provider
//	(C) Copyright 2003, Roman Kiss (rkiss@pathcom.com)
//	All rights reserved.
//	The code and information is provided "as-is" without waranty of any kind,
//	either expresed or implied.
//
//-----------------------------------------------------------------------------
//	History:
//		05/05/2003	Roman Kiss				Initial Revision
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
using RKiss.CustomChannel;
#endregion


namespace RKiss.MSMQChannel
{	
	#region MSMQClientProvider
	public class MSMQClientProvider: IClientChannelSinkProvider
	{	
		public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData) 
		{
			// create the Message Sink 
			IClientChannelSink sink = new MSMQClientTransportSink(channel, url);

			// echo
			Trace.WriteLine(string.Format("[{0}]MSMQClientProvider.CreateSink has been initiated. url={1}", 
				channel.ChannelName, url));

			// successful result
			return sink;	
		}
		
		public IClientChannelSinkProvider Next 
		{ 
			get { return null;	} 
			set{ throw new NotSupportedException(); }
		}
	}
	#endregion

	#region MSMQClientTransportSink
	public class MSMQClientTransportSink: IClientChannelSink
	{
		#region private members
		private Sender				m_Sender;							// channel
		private string				m_LogicalUri;						// endpoint
		private MessageQueue	m_OutQueue = new MessageQueue();	// outgoing queue 
		#endregion

		#region properties
		Sender Sender { get { return m_Sender; }}
		#endregion

		#region constructor
		public MSMQClientTransportSink(IChannelSender channel, string url) 
		{
			// state
			m_Sender = channel as Sender;		// parent
			m_LogicalUri = url;					// url address trimed by channel name

			// echo
			Trace.WriteLine(string.Format("[{0}]MSMQClientTransportSink has been contructed", m_Sender.ChannelName));
		}
		#endregion

		#region destructor
		~MSMQClientTransportSink() 
		{
			m_OutQueue.Close();
			Trace.WriteLine(string.Format("[{0}]MSMQClientTransportSink has been destroyed", m_Sender.ChannelName));
		}
		#endregion
		
		#region helper - url parser
		private string ParseLogicalUrl(string url, out string strQueuePath)
		{
			// endpoint or chained channels
			string strObjectURI = null;

			// queuepath (primary address)
			strQueuePath = null;

			// split the logical url adress (chaining channels)
			string[] logUrl = url.Split(new char[]{';'}, 2);		

			if(logUrl.Length > 1) 
			{
				#region channing channels
				string strPrimaryUrl = logUrl[0].Trim();
		
				// logical mapping to the physhical Uri address
				strPrimaryUrl = Sender.Mapping(strPrimaryUrl);

				// full physical Uri address
				url = strPrimaryUrl.Trim() + ";" + logUrl[1].Trim();
				#endregion
			}
			else 
			{
				#region check endpoint and logical mapping
				// check endpoint in the url address
				int intEndpoint = url.LastIndexOf('/');		
	
				if(intEndpoint < 0 || url.Length == intEndpoint + 1)  
				{
					#region no endpoint in the logical address
					// logical mapping to the physhical Uri address
					url = Sender.Mapping(url);
					#endregion
				}
				else 
				{
					#region the logical address has an endpoint
					// split into the url address and endpoint			
					string strEndpoint = url.Substring(intEndpoint + 1);
					string strLogUrl = url.Substring(0, intEndpoint);

					// logical mapping to the physhical Uri address
					string strPhysUrl = Sender.Mapping(strLogUrl);

					// full physical Uri address
					url = strPhysUrl.Trim() + "/" + strEndpoint.Trim();
					#endregion
				}
				#endregion
			}

			#region split the physical url addresses (chaining channels)
			logUrl = url.Split(new char[]{';'}, 2);	
	
			if(logUrl.Length > 1) 
			{
				// channing channel
				strQueuePath = logUrl[0].Trim();
				strObjectURI = logUrl[1].Trim();
			}
			else 
			{
				// check endpoint in the url address
				int intEndpoint = url.LastIndexOf('/');		
				if(intEndpoint < 0 || url.Length == intEndpoint + 1)  
				{
					// missing the endpoint
					throw new Exception("Missing the endpoint in the url = " + url);
				}
				else 
				{
					// split into the queue pathname and endpoint			
					strObjectURI = url.Substring(intEndpoint + 1).Trim();
					strQueuePath = url.Substring(0, intEndpoint).Trim();
				}
			}
			#endregion

			return strObjectURI;
		}
		#endregion

		#region IClientChannelSink	
		public void ProcessMessage(IMessage msgReq, ITransportHeaders requestHeaders, Stream requestStream, 
			out ITransportHeaders responseHeaders, out Stream responseStream)
		{	
			IMessage msgRsp = null;

			try 
			{
				#region send a remoting message 
				AsyncProcessRequest(null, msgReq, requestHeaders, requestStream);
				#endregion

				#region this is a Fire&Forget call, so we have to simulate a null return message
				object retVal = null;

				// generating a null return message
				IMethodCallMessage mcm = msgReq as IMethodCallMessage;
				MethodInfo mi = mcm.MethodBase as MethodInfo;
				if(mi.ReturnType != Type.GetType("System.Void"))
					retVal = mi.ReturnType.IsValueType ? Convert.ChangeType(0, mi.ReturnType) : null;	

				// return message
				msgRsp = (IMessage)new ReturnMessage(retVal, null, 0, null, mcm);
				#endregion
			}
			catch(Exception ex) 
			{
				Trace.WriteLine(string.Format("[{0}]MSMQClientTransportSink.SyncProcessMessage error = {1}", m_Sender.ChannelName, ex.Message));
				msgRsp = new ReturnMessage(ex, (IMethodCallMessage)msgReq);
			}
			finally
			{
				#region serialize IMessage response to return back
				// Note that the BinaryFormatter is a mandatory formatter for MSMQChannel!
				BinaryFormatter bf = new BinaryFormatter();
				MemoryStream rspstream = new MemoryStream();
				bf.Serialize(rspstream, msgRsp);
				rspstream.Position = 0;
								
				// returns
				responseStream = rspstream;
				responseHeaders = requestHeaders;
				#endregion
			}
		}

		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msgReq, ITransportHeaders headers, Stream stream) 
		{
			// scope state
			string strQueuePath = null;
			string strObjectUri = null;
			Message outMsg = null;
			MessageQueueTransaction mqtx = new MessageQueueTransaction();

			try 
			{
				#region pre-processor (mapping url address)
				// split into the queuepath and endpoint
				strObjectUri = ParseLogicalUrl(m_LogicalUri, out strQueuePath);
			
				// update Uri property
				msgReq.Properties[MSMQChannelProperties.ObjectUri] = strObjectUri;

				// pass TransportHeaders to the receiver
				if(m_Sender.AllowHeaders == true) 
				{
					headers["__RequestUri"] = strObjectUri;
					msgReq.Properties["__RequestHeaders"] = headers;
				}
				#endregion

				#region send a remoting message 
				// set the destination queue
				m_OutQueue.Path = strQueuePath;

				// create a message
				outMsg = new Message(msgReq, new BinaryMessageFormatter());					

				// option: timeout to pick-up a message (receive message)
				int intTimeToBeReceived = m_Sender.TimeToBeReceived;
				if(intTimeToBeReceived > 0)
					outMsg.TimeToBeReceived = TimeSpan.FromSeconds(intTimeToBeReceived);							

				// option: timeout to reach destination queue (send message) 
				int intTimeToReachQueue = m_Sender.TimeToReachQueue;
				if(intTimeToReachQueue > 0)
					outMsg.TimeToReachQueue = TimeSpan.FromSeconds(intTimeToReachQueue);	

				// option: notify a negative receive on the client/server side
				if(m_Sender.AdminQueuePath != MSMQChannelDefaults.EmptyStr) 
				{
					// acknowledge type (mandatory)
					outMsg.AcknowledgeType = AcknowledgeTypes.NegativeReceive | AcknowledgeTypes.NotAcknowledgeReachQueue;
				
					// admin queue for a time-expired messages
					outMsg.AdministrationQueue = m_Sender.AdminQueue;	
				}

				// message label
				string label = string.Format("{0}/{1}, url={2}", Convert.ToString(msgReq.Properties["__TypeName"]).Split(',')[0], 
					Convert.ToString(msgReq.Properties["__MethodName"]), strObjectUri);

                //// Send message based on the transaction context
                //if(ContextUtil.IsInTransaction == true) 
                //{
                //    // we are already in the transaction - automatic (DTC) transactional message
                //    m_OutQueue.Send(outMsg, label, MessageQueueTransactionType.Automatic);	
                //}
                //else
                //{
					// this is a single transactional message	
					mqtx.Begin();
					m_OutQueue.Send(outMsg, label, mqtx);																
					mqtx.Commit();
                //}
				#endregion
			}
			catch(Exception ex) 
			{
				string strError = string.Format("[{0}]MSMQClientTransportSink.AsyncProcessRequest error = {1}, queuepath={2},", 
					m_Sender.ChannelName, ex.Message, strQueuePath);

				m_Sender.WriteLogMsg(strError, EventLogEntryType.Error);			
				throw new Exception(strError);
			}
			finally
			{
				#region clean-up
				if(mqtx.Status == MessageQueueTransactionStatus.Pending) 
				{	
					mqtx.Abort();
					Trace.WriteLine(string.Format("[{0}]MSMQClientTransportSink.AsyncProcessRequest Aborted, msgId = {1}", m_Sender.ChannelName, outMsg.Id));
				}

				if(outMsg != null)
					outMsg.Dispose();
				#endregion
			}
		}

		public Stream GetRequestStream(IMessage msg, ITransportHeaders headers) { return null; }
		public IDictionary Properties	{	get { return null; }}
		public IClientChannelSink NextChannelSink { get	{ return null;	}}
		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
		{
			throw new RemotingException("Wrong sequence in config file - clientProviders");
		}
		#endregion
	}
	#endregion
}