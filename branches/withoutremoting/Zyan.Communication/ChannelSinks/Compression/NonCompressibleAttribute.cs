/*
 THIS CODE IS BASED ON:
 -------------------------------------------------------------------------------------------------------------- 
 Remoting Compression Channel Sink

 November, 12, 2008 - Initial revision.
 Alexander Schmidt - http://www.alexschmidt.net

 Originally published at CodeProject:
 http://www.codeproject.com/KB/IP/remotingcompression.aspx

 Copyright © 2008 Alexander Schmidt. All Rights Reserved.
 Distributed under the terms of The Code Project Open License (CPOL).
 --------------------------------------------------------------------------------------------------------------
*/
using System;

namespace Zyan.Communication.ChannelSinks.Compression
{
	/// <summary>
	/// Marks the class as an exempt from compression.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class NonCompressibleAttribute : Attribute
	{
	}
}
