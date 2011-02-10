//======================================================================================================
//	The library for Custom Remoting via MSMQ channel (Listener).
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
using System.Runtime.Remoting.Channels;

namespace RKiss.MSMQChannelLib
{
	// Listener
	public class MSMQListener :IChannelReceiver, IChannel
	{
		private string			m_ChannelName;
		private int				m_ChannelPriority;
		private string			m_URL = string.Empty;
		private bool			m_Listening = false;
		private string			m_ListenerPath = string.Empty;
		private Thread			m_Listener = null;
		private MessageQueue	m_InQueue = null;
		private delegate void delegateDispatchMessage(Message msg); 

		// MSMQListener
		public MSMQListener(){}
		public MSMQListener(string channelName, string listenerPath, int priority)
		{
			Init(channelName, listenerPath, priority);
		}
		public void Init(string channelName, string listenerPath, int priority)
		{
			m_ChannelName = channelName;
			m_ChannelPriority = priority;

			// queue pathname
			string[] cp = listenerPath.Split(new char[]{'\\'});
			if(listenerPath == string.Empty) 
				throw new Exception("Missing the Queue Pathname of the Remoting Object.");
			else
			if(cp.Length == 1) 
				m_ListenerPath = @".\" + cp[0];			// local machine and public queue
			else
			if(cp.Length == 2 && cp[0] == "Private$") 
				m_ListenerPath = @".\Private$\" + cp[1];	// local machine and private queue
			else
			{
				foreach(string s in cp)					// others
				{
					m_ListenerPath += s; 
					m_ListenerPath += @"\";
				}
				m_ListenerPath = m_ListenerPath.TrimEnd(new char[]{'\\'});
			}
			// create url
			cp = m_ListenerPath.Split(new char[]{'\\'});
			m_URL = m_ChannelName + "://";
			foreach(string s in cp)					
			{
				m_URL += s; 
				m_URL += @"/";
			}
			m_URL = m_URL.TrimEnd(new char[]{'/'});
			
			// open Listener queue
			m_InQueue = new MessageQueue(m_ListenerPath);
			m_InQueue.Formatter = new BinaryMessageFormatter(); 
			m_InQueue.MessageReadPropertyFilter.AcknowledgeType = true;
			m_InQueue.MessageReadPropertyFilter.TimeToBeReceived = true;
			m_InQueue.MessageReadPropertyFilter.CorrelationId = true;
		}
		public string ListenerPath { get { return m_ListenerPath; } }

		//IChannelReceiver
		public string ChannelName
		{
			get	{ return m_ChannelName; }
			set	{ m_ChannelName = value; }
		}
		public int ChannelPriority
		{
			get	{ return m_ChannelPriority;	}
		}
		public virtual string[] GetUrlsForUri(string objectURI)
		{
			return new string[] { m_URL + "/" + objectURI };
		}
		public string Parse(string url, out string objectURI)
		{
			objectURI = null;
			return url;
		}
		public object ChannelData 
		{
			get 
			{
				if(m_Listening == false) 
				{
					m_Listening = true;
					StartListening(null);
				}
				return null;
			}
		}
		public virtual void StopListening(object data)
		{
			Trace.WriteLine("RemoteObject-MSMQListener: StopListening...");
			m_Listener.Abort();
			m_InQueue.Close();
			m_Listener.Join(5000);
			m_Listening = false;
			Trace.WriteLine("RemoteObject-MSMQListener: StopListening done");
		}
		//
		public virtual void StartListening(object data)
		{
			Trace.WriteLine("RemoteObject-MSMQListener: Listening...");
			m_Listener = new Thread(new ThreadStart(this.Run));
			m_Listener.Start();
		}

		// Listener
		private void Run()
		{
			try 
			{
				Thread.CurrentThread.IsBackground = true;
		
				while(true) 
				{
					// Wait for client's call
					Message msg = m_InQueue.Receive();			// local queue!
					delegateDispatchMessage ddm = new delegateDispatchMessage(DispatchMessage);
					ddm.BeginInvoke(msg, null, null);

				}
			}
			catch(Exception ex) 
			{
				Trace.WriteLine(string.Format("RemoteObject-MSMQListener error = {0}", ex.Message));
			}
		}
		// worker
		public virtual void DispatchMessage(Message msg) { }

	}// MSMQListener
}// RKiss.MSMQchannelLib
