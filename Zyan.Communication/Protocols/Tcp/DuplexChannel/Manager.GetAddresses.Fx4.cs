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
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	internal partial class Manager
	{
		public static string[] GetAddresses(int port, Guid guid)
		{
			ArrayList retVal = new ArrayList();
			if (guid != Guid.Empty)
				retVal.Add(string.Format("{0}", guid));

			string hostname = Dns.GetHostName();
			IPHostEntry hostEntry = Dns.GetHostEntry(hostname);

			if (port != 0)
			{
				AddressFamily addressFamily = Socket.OSSupportsIPv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
				foreach (IPAddress address in hostEntry.AddressList)
				{
					if (address.AddressFamily == addressFamily)
					{
						string hostAndPort = string.Format("{0}:{1}", address, port);

						if (!retVal.Contains(hostAndPort))
							retVal.Add(hostAndPort);
					}
				}
				if (!retVal.Contains(string.Format("{0}:{1}", IPAddress.Loopback, port)))
					retVal.Add(string.Format("{0}:{1}", IPAddress.Loopback, port));
			}

			return (string[])retVal.ToArray(typeof(string));
		}
	}
}
