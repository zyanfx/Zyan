//======================================================================================================
//	The library for Custom Remoting via MSMQ channel (Receiver).
//	(C) Copyright 2001, Roman Kiss (rkiss@pathcom.com)
//	All rights reserved.
//	The code and information is provided "as-is" without waranty of any kind, either expresed or implied.
//------------------------------------------------------------------------------------------------------
//	History:
//			12-05-2001	RK	Initial Release	
//======================================================================================================
//
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Messaging;
using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;


namespace RKiss.MSMQChannelLib
{
	// Receiver
	public class MSMQReceiver : MSMQListener
	{	
		// these are a hardcoded default values
		const string OBJECTURI = "__objectUri";
		public const string DEFAULT_REQCHANNEL = @".\ReqChannel";
		public const string DEFAULT_CHANNELNAME = "msmq";
		public const int    DEFAULT_CHANNELPRIORITY = 1;

		public MSMQReceiver() : base(DEFAULT_CHANNELNAME, DEFAULT_REQCHANNEL, DEFAULT_CHANNELPRIORITY) 
		{ 
			// default setup the config values
		}
		public MSMQReceiver(string channelName, string path, int priority)
		{ 
			// programmatically setup the config values 
			base.Init(channelName, path, priority);
		}
		public MSMQReceiver(IDictionary properties, IServerChannelSinkProvider serverSinkProvider) 
		{	
			string listenerPath = DEFAULT_REQCHANNEL;
			string channelName  = DEFAULT_CHANNELNAME;
			int	channelPriority = DEFAULT_CHANNELPRIORITY;
	
			// administratively setup the config values (xxx.exe.config file)
			if(properties.Contains("listener")) 
				listenerPath = properties["listener"].ToString();
			if(properties.Contains("name")) 
				channelName = properties["name"].ToString();
			if(properties.Contains("priority")) 
				channelPriority = Convert.ToInt32(properties["priority"]);
		
			base.Init(channelName, listenerPath, channelPriority);
		}
		//
		// Listener worker
		public override void DispatchMessage(Message msg)
		{
			//Message msg = null;
			MessageQueueTransaction mqtx = new MessageQueueTransaction();

			try 
			{
				// message from the client
				IMessage msgReq = msg.Body as IMessage;
				msgReq.Properties["__Uri"] = msgReq.Properties[OBJECTURI];		// work around!!!
				Trace.WriteLine(string.Format("RemoteObject:PRE-CALL, msgId={0}", msg.Id));
					
				// Dispatch message to the remoting object
				IMessage msgRsp = ChannelServices.SyncDispatchMessage(msgReq);
				if(msgRsp != null) 
				{
					// update this msg with the return value and send it back to the caller
					mqtx.Begin();
					msg.BodyStream.Close();										// clean-up
					msg.Body = msgRsp;											// serialize IMessage
					msg.CorrelationId = msg.Id;									// cookie
					msg.ResponseQueue.Send(msg, "RET: " + msg.Label, mqtx);		// send message
					mqtx.Commit();
					msg.ResponseQueue.Close();
					Trace.WriteLine(string.Format("RemoteObject:POST-CALL, msgId={0}", msg.CorrelationId));
				}
				else
					Trace.WriteLine(string.Format("RemoteObject-[OneWay]: msgId = {0}", msg.Id));	// dump MessageId
			}
			catch (MessageQueueException ex) 
			{
				Trace.WriteLine(string.Format("RemoteObject error = {0}, msgId = {1}", ex.Message, msg.Id));
			}
			catch(Exception ex) 
			{
				Trace.WriteLine(string.Format("RemoteObject error = {0}, msgId = {1}", ex.Message, msg.Id));
			}
			finally
			{
				if(mqtx.Status == MessageQueueTransactionStatus.Pending) 
				{	
					mqtx.Abort();
				}
			}
		}// DispatchMessage
	}// MSMQReceiver
}// RKiss.MSMQchannelLib
