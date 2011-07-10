using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.IO;
using System.Collections;
using Zyan.Communication.Protocols.Tcp.DuplexChannel.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel
{
	/// <summary>
	/// Helper class for TransportHeaders serialization (TransportHeaders binary
	/// serialization format is not compatible between .NET and Mono Framework).
	/// </summary>
	[Serializable]
	internal class TransportHeaderWrapper
	{
		private static readonly BinaryFormatter formatter = new BinaryFormatter
		{
			AssemblyFormat = FormatterAssemblyStyle.Simple,
			FilterLevel = TypeFilterLevel.Low,
			TypeFormat = FormatterTypeStyle.TypesWhenNeeded
		};

		object[] Keys = new object[0];

		object[] Values = new object[0];

		public TransportHeaderWrapper()
		{
		}

		public TransportHeaderWrapper(ITransportHeaders headers)
		{
			var keys = new List<object>();
			var values = new List<object>();

			foreach (DictionaryEntry entry in headers)
			{
				keys.Add(entry.Key);
				values.Add(entry.Value);
			}

			Keys = keys.ToArray();
			Values = values.ToArray();
		}

		public ITransportHeaders Headers
		{
			get
			{
				var headers = new TransportHeaders();

				for (int i = 0; i < Math.Min(Keys.Length, Values.Length); i++)
				{
					headers[Keys[i]] = Values[i];
				}

				return headers;
			}
		}

		public static MemoryStream Serialize(ITransportHeaders headers)
		{
			var ms = new MemoryStream();
			formatter.Serialize(ms, new TransportHeaderWrapper(headers));
			return ms;
		}

		public static ITransportHeaders Deserialize(Stream stream)
		{
			var wrapper = (TransportHeaderWrapper)formatter.Deserialize(stream);
			return wrapper.Headers;
		}
	}
}
