// /*
//  THIS CODE IS BASED ON:
//  -------------------------------------------------------------------------------------------------------------- 
//  Remoting Compression Channel Sink
//
//  November, 12, 2008 - Initial revision.
//  Alexander Schmidt - http://www.alexschmidt.net
//
//  Originally published at CodeProject:
//  http://www.codeproject.com/KB/IP/remotingcompression.aspx
//
//  Copyright � 2008 Alexander Schmidt. All Rights Reserved.
//  Distributed under the terms of The Code Project Open License (CPOL).
//  --------------------------------------------------------------------------------------------------------------
// */
// using System;
// using System.IO;
// using System.IO.Compression;
// using Zyan.Communication.Toolbox.IO;
//
// namespace Zyan.Communication.Toolbox.Compression
// {
// 	internal class CompressionHelper
// 	{
// 		// The default value for the compression threshold: 64K.
// 		public const int CompressionThreshold = 1 << 16;
//
// 		// The size of the buffer: 16K.
// 		private const int BufferSize = 1 << 14;
//
// 		// The size of 16-bit integer.
// 		private const int ShortSize = sizeof(short);
//
// 		public static Stream Compress(Stream inputStream, CompressionMethod level = CompressionMethod.Default)
// 		{
// 			switch (level)
// 			{
// 				// bypass compression
// 				case CompressionMethod.None:
// 					return inputStream;
//
// 				// average compression using DeflateStream
// 				case CompressionMethod.DeflateStream:
// 				{
// 					var stream = new SmallBlockMemoryStream();
// 					using (var output = new DeflateStream(stream, CompressionMode.Compress, true))
// 					{
// 						int read;
// 						var buffer = new byte[BufferSize];
//
// 						while ((read = inputStream.Read(buffer, 0, BufferSize)) > 0)
// 						{
// 							output.Write(buffer, 0, read);
// 						}
// 					}
//
// 					stream.Seek(0, SeekOrigin.Begin);
// 					return stream;
// 				}
//
// 				// fast compression using LZF
// 				case CompressionMethod.LZF:
// 				{
// 					var buffer = new byte[BufferSize];
// 					var output = new byte[BufferSize * 2]; // safe value for uncompressible data
// 					var outStream = new SmallBlockMemoryStream();
// 					var lzf = new LZF();
//
// 					while (true)
// 					{
// 						var readCount = (short)inputStream.Read(buffer, 0, buffer.Length);
// 						if (readCount == 0)
// 						{
// 							break;
// 						}
//
// 						var writeCount = (short)lzf.Compress(buffer, readCount, output, output.Length);
// 						if (writeCount == 0)
// 						{
// 							throw new InvalidOperationException("Cannot compress input stream.");
// 						}
//
// 						// write source size
// 						var temp = BitConverter.GetBytes(readCount);
// 						outStream.Write(temp, 0, ShortSize);
//
// 						// write destination size
// 						temp = BitConverter.GetBytes(writeCount);
// 						outStream.Write(temp, 0, ShortSize);
//
// 						// write data chunk
// 						outStream.Write(output, 0, writeCount);
// 					}
//
// 					// rewind the output stream
// 					outStream.Seek(0, SeekOrigin.Begin);
// 					return outStream;
// 				}
// 			}
//
// 			// unknown compression method
// 			throw new InvalidOperationException();
// 		}
//
// 		public static Stream Decompress(Stream inputStream, CompressionMethod level = CompressionMethod.Default)
// 		{
// 			switch (level)
// 			{
// 				// bypass decompression
// 				case CompressionMethod.None:
// 					return inputStream;
//
// 				// decompress using DeflateStream
// 				case CompressionMethod.DeflateStream:
// 				{
// 					var stream = new SmallBlockMemoryStream();
// 					using (var output = new DeflateStream(inputStream, CompressionMode.Decompress, true))
// 					{
// 						int read;
// 						var buffer = new byte[BufferSize];
//
// 						while ((read = output.Read(buffer, 0, BufferSize)) > 0)
// 						{
// 							stream.Write(buffer, 0, read);
// 						}
// 					}
//
// 					stream.Seek(0, SeekOrigin.Begin);
// 					return stream;
// 				}
//
// 				// decompress using LZF
// 				case CompressionMethod.LZF:
// 				{
// 					var buffer = new byte[BufferSize * 2];
// 					var output = new byte[BufferSize];
// 					var temp = new byte[ShortSize * 2];
// 					var outStream = new SmallBlockMemoryStream();
// 					var lzf = new LZF();
//
// 					while (true)
// 					{
// 						// read chunk sizes
// 						if (inputStream.Read(temp, 0, ShortSize * 2) == 0)
// 						{
// 							break;
// 						}
//
// 						var sourceSize = BitConverter.ToInt16(temp, 0);
// 						var destSize = BitConverter.ToInt16(temp, ShortSize);
//
// 						var readCount = inputStream.Read(buffer, 0, destSize);
// 						if (readCount != destSize)
// 						{
// 							throw new InvalidOperationException("Cannot read input stream.");
// 						}
//
// 						var writeCount = lzf.Decompress(buffer, readCount, output, output.Length);
// 						if (writeCount != sourceSize)
// 						{
// 							throw new InvalidOperationException("Cannot decompress input stream.");
// 						}
//
// 						outStream.Write(output, 0, writeCount);
// 					}
//
// 					// rewind the output stream
// 					outStream.Seek(0, SeekOrigin.Begin);
// 					return outStream;
// 				}
// 			}
//
// 			// unknown compression method
// 			throw new InvalidOperationException();
// 		}
// 	}
// }
