using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace Util
{
    public class CompressHelper
    {
        // The size of the buffer.
        private const int BUFFER_SIZE = 4096;

        public static Stream Compress(Stream inputStream)
        {
            Stream stream = new MemoryStream();
            using (GZipStream output = new GZipStream(stream, CompressionMode.Compress, true))
            {
                int read;
                byte[] buffer = new byte[BUFFER_SIZE];

                while ((read = inputStream.Read(buffer, 0, BUFFER_SIZE)) > 0)
                {
                    output.Write(buffer, 0, read);
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static Stream Decompress(Stream inputStream)
        {
            Stream stream = new MemoryStream();
            using (GZipStream output = new GZipStream(inputStream, CompressionMode.Decompress, true))
            {
                int read;
                byte[] buffer = new byte[BUFFER_SIZE];

                while ((read = output.Read(buffer, 0, BUFFER_SIZE)) > 0)
                {
                    stream.Write(buffer, 0, read);
                }
            }

            // Rewind the response stream.
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}
