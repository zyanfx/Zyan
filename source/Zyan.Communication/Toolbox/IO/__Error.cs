/*
 THIS CODE IS COPIED FROM:
 --------------------------------------------------------------------------------------------------------------
 SmallBlockMemoryStream

 A MemoryStream replacement that avoids using the Large Object Heap (MIT-licensed)
 https://github.com/Aethon/SmallBlockMemoryStream

 Copyright (c) 2014 Brent McCullough.
 --------------------------------------------------------------------------------------------------------------
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zyan.Communication.Toolbox.IO
{
	internal static class __Error
	{
		public static Exception StreamIsClosed()
		{
			return new ObjectDisposedException(null, "Stream is closed");
		}

		public static Exception NeedNonNegNumber(string parameterName)
		{
			return new ArgumentOutOfRangeException(parameterName, "Must be greater than or equal to zero");
		}

		public static Exception NullArgument(string parameterName)
		{
			return new ArgumentNullException(parameterName);
		}

		public static Exception InvalidOffset(string parameterName)
		{
			return new ArgumentException("Invalid offset", parameterName);
		}

		public static Exception SeekBeforeBegin()
		{
			return new IOException("Cannot seek to a position before the the beginning of the stream");
		}

		public static Exception UnknownSeekOrigin(SeekOrigin origin, string parameterName)
		{
			return new ArgumentException(string.Format("Unknown origin: '{0}'", origin), parameterName);
		}

		public static Exception StreamCapacityLessThanLength(string parameterName)
		{
			return new ArgumentOutOfRangeException(parameterName, "Cannot set the capacity of the stream smaller than the length");
		}
	}
}
