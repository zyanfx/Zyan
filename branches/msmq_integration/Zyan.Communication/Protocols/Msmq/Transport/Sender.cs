//=============================================================================
//	The MSMQChannel - Sender Custom Channel 
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
	#region Sender
	public class Sender : KnowledgeBase, IChannelSender
	{
		#region private members
		private string				m_ChannelName = "";							// channel name
		private int						m_ChannelPriority = 0;					// channel priority
		private bool					m_AllowHeaders = false;					// allow to send a transport headers
		private EventLog			m_EventLog = null;							// EventLog object
		private MessageQueue	m_AdminQueue = null;						// administration queue
		private IClientChannelSinkProvider m_Provider = null;	// channel sink provider
		#endregion	

		#region properties
		public MessageQueue AdminQueue		{ get { return m_AdminQueue; }}
		public IClientChannelSinkProvider Provider { get { return m_Provider; }}
		public bool AllowHeaders  { get { return m_AllowHeaders; } set { m_AllowHeaders = value; }}
		public int TimeToBeReceived
		{
			get	{ return Convert.ToInt32(KB[MSMQChannelProperties.RcvTimeout]); }
			set	{ KB[MSMQChannelProperties.RcvTimeout] = value; }
		}
		public int TimeToReachQueue
		{
			get	{ return Convert.ToInt32(KB[MSMQChannelProperties.SendTimeout]); }
			set	{ KB[MSMQChannelProperties.SendTimeout] = value; }
		}
		public string AdminQueuePath
		{
			get	{ return Convert.ToString(KB[MSMQChannelProperties.AdminQueuePath]);	 }
			set	{ KB[MSMQChannelProperties.AdminQueuePath] = value; }
		}
		#endregion

		#region constructor
		public Sender(IDictionary properties, IClientChannelSinkProvider clientSinkProvider) 
		{	
			// administratively setup using the config values
			// static knowledge base
			m_ChannelName = properties.Contains(MSMQChannelProperties.ChannelName) ? 
				Convert.ToString(properties[MSMQChannelProperties.ChannelName]) : MSMQChannelDefaults.ChannelName;
			m_ChannelPriority = properties.Contains(MSMQChannelProperties.ChannelPriority) ? 
				Convert.ToInt32(properties[MSMQChannelProperties.ChannelPriority]) : MSMQChannelDefaults.ChannelPriority;
			AllowHeaders = properties.Contains(MSMQChannelProperties.UseHeaders) ? 
				Convert.ToBoolean(properties[MSMQChannelProperties.UseHeaders]) : MSMQChannelDefaults.UseHeaders;
			AllowToUpdate = properties.Contains(MSMQChannelProperties.UpdateKB) ? 
				Convert.ToBoolean(properties[MSMQChannelProperties.UpdateKB]) : MSMQChannelDefaults.CanBeUpdated;
			
			// dynamically knowledge base
			AdminQueuePath = properties.Contains(MSMQChannelProperties.AdminQueuePath) ? 
				Convert.ToString(properties[MSMQChannelProperties.AdminQueuePath]) : MSMQChannelDefaults.EmptyStr;
			TimeToBeReceived = properties.Contains(MSMQChannelProperties.RcvTimeout) ? 
				Convert.ToInt32(properties[MSMQChannelProperties.RcvTimeout]) : MSMQChannelDefaults.TimeoutInSec;
			TimeToReachQueue = properties.Contains(MSMQChannelProperties.SendTimeout) ? 
				Convert.ToInt32(properties[MSMQChannelProperties.SendTimeout]) : MSMQChannelDefaults.TimeoutInSec;
		
			// create admin queue
			m_AdminQueue = new MessageQueue();
			m_AdminQueue.Formatter = new BinaryMessageFormatter();
			m_AdminQueue.Path = AdminQueuePath;

			// knowledge base of the connectivity
			string strLURL = "none";
			if(properties.Contains(MSMQChannelProperties.LURL))
			{
				strLURL = Convert.ToString(properties[MSMQChannelProperties.LURL]);
				Store(strLURL, true);
			}

			// publish the MSMQChannel endpoint using the channel name.
			Publish(ChannelName);
			
			// channel provider
			m_Provider = clientSinkProvider == null ? new BinaryClientFormatterSinkProvider() : clientSinkProvider;

			// add the MSMQClientProvider at the end
			IClientChannelSinkProvider provider = m_Provider;
			while(provider.Next != null)
				provider = provider.Next;
			provider.Next = new MSMQClientProvider();

			// event log
			string strLogMsg = string.Format("[{0}]MSMQChannel.Sender has been initiated. adminQueue={1}, timeout={2}, lurl={3}", 
				ChannelName, m_AdminQueue == null ? "none" : m_AdminQueue.QueueName, Convert.ToString(TimeToBeReceived), strLURL);
			WriteLogMsg(strLogMsg, EventLogEntryType.Information);
		}
		#endregion

		#region cleanup
		~Sender()
		{
			if(m_AdminQueue != null) 
			{
				m_AdminQueue.Close();
			}

			//echo
			string strLogMsg = string.Format("[{0}]MSMQChannel.Sender has been destroyed", ChannelName);
			Trace.WriteLine(strLogMsg);
		}
		#endregion

		#region KnowledgeBase override
		public override bool OnBeforeChangeKnowledgeBase(string name, string val)
		{
			// validation of the selected properties
			if(name == null || name == "")
				throw new Exception("The name is not recognize (null/empty).");  

			if(name == MSMQChannelProperties.RetryTime || name == MSMQChannelProperties.SendTimeout) 
				Convert.ToInt32(val);
	
			return true;
		}
		public override void OnChangedKnowledgeBase(string name, string val)
		{
			// update state
			if(name == MSMQChannelProperties.AdminQueuePath) 
				m_AdminQueue.Path = val;
		}
		public override void OnClearKnowledgeBase(string name) 
		{
			// perform a default state
			if(name == null) 
			{
				m_AdminQueue.Path = MSMQChannelDefaults.EmptyStr;	
				TimeToBeReceived = MSMQChannelDefaults.TimeoutInSec;
				TimeToReachQueue = MSMQChannelDefaults.TimeoutInSec;
			}
			else
			if(name == MSMQChannelProperties.AdminQueuePath) 
				m_AdminQueue.Path = MSMQChannelDefaults.EmptyStr;			
			else if(name == MSMQChannelProperties.RcvTimeout) 
				TimeToBeReceived = MSMQChannelDefaults.TimeoutInSec;
			else if(name == MSMQChannelProperties.SendTimeout) 
				TimeToReachQueue = MSMQChannelDefaults.TimeoutInSec;
		}
		#endregion

		#region IChannelSender
		public string ChannelName		{ get { return m_ChannelName; }}
		public int ChannelPriority	{	get { return m_ChannelPriority; }}
		public string Parse(string url, out string objectURI)	{ return objectURI = null; }
		public virtual IMessageSink CreateMessageSink(string url, object data, out string objectURI)
		{
			// endpoint
			objectURI = null;

			// check the logical url address
			if(url.Length == 0)
				return null;	

			// split channel name
			string[] urlsplit = url.Split(new char[]{':'}, 2);

			// check url for this channel
			if(urlsplit.Length < 2 || urlsplit[0].ToLower().Trim() != ChannelName.ToLower()) 
				return null;

			// trim by delimiter '//'
			url = urlsplit[1].Trim('/').Trim('/');
		
			// create the Provider Sink 
			IMessageSink sink = (IMessageSink)Provider.CreateSink(this, url, data);

			// echo
			Trace.WriteLine(string.Format("[{0}]MSMQChannel.CreateMessageSink has been initiated for {1}.", ChannelName, url));

			// successful result
			return sink;					
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
				Trace.WriteLine(string.Format("{0}.WriteLogMsg error = {1}", ChannelName, ex.Message));
			}
		}
		#endregion
	}
	#endregion
}