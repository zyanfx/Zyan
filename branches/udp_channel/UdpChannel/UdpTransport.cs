using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.Protocols.Udp
{
	/// <summary>
	/// Writes and reads UDP datagrams
	/// </summary>
	class UdpTransport
	{
		public UdpTransport(UdpClient client)
		{
			UdpClient = client;
		}

		UdpClient UdpClient { get; set; }

		const short HeaderMarker = 0x1234;
		const short DataMarker = 0x4321;

		public void Write(ITransportHeaders headers, Stream stream, IPEndPoint endpoint)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				foreach (DictionaryEntry entry in headers)
				{
					bw.Write(HeaderMarker);
					bw.Write(entry.Key.ToString());
					bw.Write(entry.Value.ToString());
				}

				var length = (int)stream.Length;
				var br = new BinaryReader(stream);

				bw.Write(DataMarker);
				bw.Write(length);
				bw.Write(br.ReadBytes(length));
				bw.Flush();

				// FIXME: 1) reliability 2) check buffer size
				var buffer = ms.ToArray();
				UdpClient.Send(buffer, buffer.Length, endpoint);
			}
		}

		public IPEndPoint Read(out ITransportHeaders headers, out Stream stream, out IPEndPoint remote)
		{
			// FIXME: 1) reliability 2) exceptions
			remote = new IPEndPoint(IPAddress.Loopback, 0);
			var buffer = UdpClient.Receive(ref remote);

			using (var ms = new MemoryStream(buffer))
			using (var br = new BinaryReader(ms))
			{
				var marker = br.ReadInt16();
				if (marker != HeaderMarker && marker != DataMarker)
				{
					throw new InvalidDataException("Unexpected datagram format");
				}

				// read transport headers
				headers = new TransportHeaders();
				while (marker != DataMarker)
				{
					var name = br.ReadString();
					var value = br.ReadString();
					headers[name] = value;
					marker = br.ReadInt16();
				}

				// get response stream
				var length = br.ReadInt32();
				stream = new MemoryStream(buffer, (int)ms.Position, length);
			}

			return remote;
		}
	}
}
