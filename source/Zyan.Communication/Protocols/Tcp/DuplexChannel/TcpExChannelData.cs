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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	/// <summary>
	/// Describes configuration data of a TcpEx channel.
	/// </summary>
	[Serializable]
	public class TcpExChannelData : IChannelDataStore
	{	
		private HybridDictionary _properties;
		
		[NonSerialized]
		private string[] _channelUris;

		/// <summary>
		/// Creates a new instance of the TcpExChannelData class.
		/// </summary>
		/// <param name="channel">Remoting channel</param>
		public TcpExChannelData(TcpExChannel channel)
		{
			Port = channel.Port;
			ChannelID = channel.ChannelID;

			if (Port != 0)
			{
				Addresses = new List<string>();
				IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());

				foreach (IPAddress address in hostEntry.AddressList)
				{
					Addresses.Add(string.Format("{0}:{1}", address, Port));
				}
			}
		}
		
		#region Properties

		/// <summary>
		/// Gets the TCP port number of the channel.
		/// </summary>
		public int Port
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the unique identifier of the channel.
		/// </summary>
		public Guid ChannelID
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the registered addresses of the channel.
		/// </summary>
		public List<string> Addresses
		{
			get;
			private set;
		}
		
		#endregion

		#region IChannelDataStore Members
		
		/// <summary>
		/// Gets the value of a channel property by its name.
		/// </summary>
		/// <param name="key">Property name</param>
		/// <returns>Property value</returns>
		public object this[object key]
		{
			get
			{
				if (_properties == null)
					return null;
				
				return _properties[key];
			}
			set
			{
				if (_properties == null)
					_properties = new HybridDictionary();
				
				_properties[key] = value;
			}
		}

		/// <summary>
		/// Gets an array of all registered channel URIs.
		/// </summary>
		public string[] ChannelUris
		{
			get
			{
				if (_channelUris == null)
					_channelUris = Manager.GetUrlsForUri(null, Port, ChannelID);
				
				return _channelUris;
			}
		}

		#endregion
	}
}