using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Zyan.SafeDeserializationHelpers;

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
		}
		.Safe();

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
				var key = entry.Key;
				var value = entry.Value;

				// work around IPAddress serialization MonoDroid interoperability issue
				if (key != null && key.ToString() == CommonTransportKeys.IPAddress)
				{
					value = entry.Value.ToString();
				}

				keys.Add(key);
				values.Add(value);
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
					var key = Keys[i];
					var value = Values[i];

					// work around IPAddress serialization MonoDroid interoperability issue
					if (key != null && value != null && key.ToString() == CommonTransportKeys.IPAddress)
					{
						value = IPAddress.Parse(value.ToString());
					}

					headers[key] = value;
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
