//=============================================================================
//	The MSMQChannel - Receiver Custom Channel 
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
	// Receiver
	public class Receiver : Listener, IChannelReceiver
	{	
		#region private members
		private IServerChannelSinkProvider m_Provider = null;
		private MSMQServerTransportSink m_Sink = null;
		#endregion

		#region constructor
		public Receiver(IDictionary properties, IServerChannelSinkProvider serverSinkProvider) 
		{	
			// administratively setup using the config values
			// static knowledge base
			ChannelName = properties.Contains(MSMQChannelProperties.ChannelName) ? 
				Convert.ToString(properties[MSMQChannelProperties.ChannelName]) : MSMQChannelDefaults.ChannelName;
			ChannelPriority = properties.Contains(MSMQChannelProperties.ChannelPriority) ? 
				Convert.ToInt32(properties[MSMQChannelProperties.ChannelPriority]) : MSMQChannelDefaults.ChannelPriority;
			ListenerPath = properties.Contains(MSMQChannelProperties.Listener) ? 
				Convert.ToString(properties[MSMQChannelProperties.Listener]) : MSMQChannelDefaults.QueuePath;
			AllowToUpdate = properties.Contains(MSMQChannelProperties.UpdateKB) ? 
				Convert.ToBoolean(properties[MSMQChannelProperties.UpdateKB]) : MSMQChannelDefaults.CanBeUpdated;
			
			// dynamically knowledge base
			NotifyTime = properties.Contains(MSMQChannelProperties.NotifyTime) ? 
				Convert.ToInt32(properties[MSMQChannelProperties.NotifyTime]) : MSMQChannelDefaults.TimeoutInSec;
			RetryTime = properties.Contains(MSMQChannelProperties.RetryTime) ?
				Convert.ToInt32(properties[MSMQChannelProperties.RetryTime]) : MSMQChannelDefaults.TimeoutInSec;
			RetryCounter = properties.Contains(MSMQChannelProperties.Retry) ? 
				Convert.ToInt32(properties[MSMQChannelProperties.Retry]) : MSMQChannelDefaults.RetryCounter;
			RetryFilter = properties.Contains(MSMQChannelProperties.RetryFilter) ? 
				Convert.ToString(properties[MSMQChannelProperties.RetryFilter]) : MSMQChannelDefaults.EmptyStr;
			NotifyUrl = properties.Contains(MSMQChannelProperties.NotifyUrl) ? 
				Convert.ToString(properties[MSMQChannelProperties.NotifyUrl]) : MSMQChannelDefaults.EmptyStr;
			AcknowledgeUrl = properties.Contains(MSMQChannelProperties.AckUrl) ? 
				Convert.ToString(properties[MSMQChannelProperties.AckUrl]) : MSMQChannelDefaults.EmptyStr;
			ExceptionUrl = properties.Contains(MSMQChannelProperties.ExceptionUrl) ? 
				Convert.ToString(properties[MSMQChannelProperties.ExceptionUrl]) : MSMQChannelDefaults.EmptyStr;
			UseTimeout = properties.Contains(MSMQChannelProperties.UseTimeout) ? 
				Convert.ToBoolean(properties[MSMQChannelProperties.UseTimeout]) : MSMQChannelDefaults.UseTimeout;
			// validate number of threads
			MaxNumberOfWorkers = MSMQChannelDefaults.MaxThreads;
			if(properties.Contains(MSMQChannelProperties.MaxThreads)) 
			{
				string maxthreads = Convert.ToString(properties[MSMQChannelProperties.MaxThreads]);
				Update(MSMQChannelProperties.MaxThreads, maxthreads);
			}
					
			// channel provider
			m_Provider = serverSinkProvider == null ? new BinaryServerFormatterSinkProvider() : serverSinkProvider;

			/*
			// Collect the rest of the channel data:
			IServerChannelSinkProvider provider = m_Provider;
			while(provider != null)
			{
				provider.GetChannelData(_data);
				provider = provider.Next;
			}
			*/
		
			IServerChannelSink next = ChannelServices.CreateServerChannelSinkChain(m_Provider, this);
			m_Sink = new MSMQServerTransportSink(next);

			// publish the MSMQChannel endpoint using the channel name.
			base.Publish(ChannelName);

			// start Listener
			StartListening(null);
		}
		#endregion
	
		#region destructor
		~Receiver() 
		{
			
		}
		#endregion

		#region properties
		public IServerChannelSinkProvider Provider { get {return m_Provider; }}
		public IServerChannelSink TransportSink { get {return m_Sink; }} 
		#endregion

		#region IChannelReceiver
		public object ChannelData 
		{
			get {	return null;/*//todo: return m_data;*/ }
		}
		public virtual string[] GetUrlsForUri(string objectURI)
		{
			//return new string[] { m_URL + "/" + objectURI };
			return new string[] { objectURI };
		}
		public virtual void StopListening(object data)
		{
			Stop();
		}
		public virtual void StartListening(object data)
		{
			Start();
		}
		#endregion
	}
	
	internal class MSMQServerTransportSink : IServerChannelSink
	{
		#region private members
		private IServerChannelSink m_next;
		#endregion
    
		#region constructor
		public MSMQServerTransportSink(IServerChannelSink next)	{ m_next = next; }
		#endregion

		#region IServerChannelSink
		public IServerChannelSink NextChannelSink	{	get { return m_next; }}
		public IDictionary Properties	{	get { return null; }} 
		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack,
			IMessage requestMsg,
			ITransportHeaders requestHeaders, 
			Stream requestStream,
			out IMessage msg, 
			out ITransportHeaders responseHeaders,
			out Stream responseStream)
		{
			// this sink is first!
			throw new NotSupportedException();
		}
		public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, Object state,
			IMessage msg, ITransportHeaders headers, Stream stream)                 
		{
			throw new NotSupportedException();
		} 
		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, Object state,
			IMessage msg, ITransportHeaders headers)
		{            
			return null;
		} 
		#endregion    
	}
}