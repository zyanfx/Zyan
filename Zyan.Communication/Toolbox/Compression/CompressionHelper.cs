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
using System.IO;
using System.IO.Compression;

namespace Zyan.Communication.Toolbox.Compression
{
	internal class CompressionHelper
	{
		// The size of the buffer.
		private const int BUFFER_SIZE = 4096;

		// The size of 32-bit integer.
		private const int INT_SIZE = sizeof(System.Int32);

		public static Stream Compress(Stream inputStream, CompressionMethod level = CompressionMethod.Default)
		{
			switch (level)
			{
				// bypass compression
				case CompressionMethod.None:
					inputStream.Seek(0, SeekOrigin.Begin);
					return inputStream;

				// average compression using DeflateStream
				case CompressionMethod.Average:
					{
						var stream = new MemoryStream();
						using (var output = new DeflateStream(stream, CompressionMode.Compress, true))
						{
							int read;
							var buffer = new byte[BUFFER_SIZE];

							while ((read = inputStream.Read(buffer, 0, BUFFER_SIZE)) > 0)
							{
								output.Write(buffer, 0, read);
							}
						}

						stream.Seek(0, SeekOrigin.Begin);
						return stream;
					}

				// fast compression using LZF
				case CompressionMethod.Fast:
					{
						// prepare input data
						var input = new byte[(int)inputStream.Length];
						inputStream.Read(input, 0, input.Length);

						// allocate output buffer (1.5 size of the input buffer should be sufficient)
						var output = new byte[input.Length + input.Length / 2];
						var size = new LZF().Compress(input, input.Length, output, output.Length);
						Array.Resize(ref output, size + INT_SIZE);

						// save the original data size
						var temp = BitConverter.GetBytes(input.Length);
						Array.Copy(temp, 0, output, size, INT_SIZE);

						// return as MemoryStream
						var outStream = new MemoryStream(output);
						return outStream;
					}
			}

			// unknown compression method
			throw new InvalidOperationException();
		}

		public static Stream Decompress(Stream inputStream, CompressionMethod level = CompressionMethod.Default)
		{
			switch (level)
			{
				// bypass decompression
				case CompressionMethod.None:
					inputStream.Seek(0, SeekOrigin.Begin);
					return inputStream;

				// decompress using DeflateStream
				case CompressionMethod.Average:
					{
						var stream = new MemoryStream();
						using (var output = new DeflateStream(inputStream, CompressionMode.Decompress, true))
						{
							int read;
							var buffer = new byte[BUFFER_SIZE];

							while ((read = output.Read(buffer, 0, BUFFER_SIZE)) > 0)
							{
								stream.Write(buffer, 0, read);
							}
						}

						stream.Seek(0, SeekOrigin.Begin);
						return stream;
					}

				// decompress using LZF
				case CompressionMethod.Fast:
					{
						// read compressed data
						var input = new byte[(int)inputStream.Length - INT_SIZE];
						inputStream.Read(input, 0, input.Length);

						// read decompressed size
						var temp = new byte[INT_SIZE];
						inputStream.Read(temp, 0, INT_SIZE);
						var outputLength = BitConverter.ToInt32(temp, 0);

						// prepare output buffer
						var output = new byte[outputLength];
						var size = new LZF().Decompress(input, input.Length, output, output.Length);
						return new MemoryStream(output);
					}
			}

			// unknown compression method
			throw new InvalidOperationException();
		}
	}
}
