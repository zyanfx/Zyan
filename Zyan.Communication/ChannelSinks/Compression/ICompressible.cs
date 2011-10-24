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
namespace Zyan.Communication.ChannelSinks.Compression
{
	/// <summary>
	/// The ICompressible interface implements a property that returns a flag,
	/// which determines if the request or response should be compressed.
	/// This interface is used in conjuction with the compression sink implementation
	/// and allows to determine dynamically if the request or response is
	/// to be compressed.
	/// </summary>
	/// <remarks>
	/// The following is the order, in which the criteria are evaluated to determine
	/// if the request or response is to be compressed: Threshold should be greater than
	/// zero. NonCompressible marks the object as an exempt. If object size is
	/// greater than threshold and not marked as NonCompressible, the ICompressible is evaluated.
	/// </remarks>
	public interface ICompressible
	{
		/// <summary>
		/// Gets a value indicating whether the data can be compressed.
		/// </summary>
		/// <value>
		///   <c>true</c> if the data should be compressed; otherwise, <c>false</c>.
		/// </value>
		bool PerformCompression { get; }
	}
}
