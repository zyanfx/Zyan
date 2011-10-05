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
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	internal partial class Manager
	{
		static Lazy<List<string>> _addresses = new Lazy<List<string>>(() =>
		{
			// prefer IPv4 address
			var addressFamily = Socket.OSSupportsIPv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;

			// GetAllNetworkInterfaces() may be slow, so execute it once and cache results
			var query =
				from nic in NetworkInterface.GetAllNetworkInterfaces()
				from ua in nic.GetIPProperties().UnicastAddresses
				where ua.Address.AddressFamily == addressFamily
				select ua.Address.ToString();

			return query.ToList();

		}, true);
	}
}
