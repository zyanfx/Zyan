using System;
using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Net;

namespace Zyan.Communication.Protocols.Udp
{
	/// <summary>
	/// UDP channel utility class
	/// </summary>
	static class UdpChannelHelper
	{
		public const string DefaultName = "udp";

		public const int DefaultPriority = 1;

		public const int DefaultPort = 12345;

		public static T GetValue<T>(this IDictionary properties, string name, T defaultValue)
		{
			if (properties != null && properties.Contains(name))
			{
				return (T)Convert.ChangeType(properties[name], typeof(T));
			}

			return defaultValue;
		}

		public static T GetValue<T>(this IDictionary properties, string name)
		{
			return properties.GetValue<T>(name, default(T));
		}

		public static string Parse(string url, out string objectUri)
		{
			try
			{
				var uri = new Uri(url);
				objectUri = uri.AbsolutePath;
				return uri.Authority;
			}
			catch
			{
				objectUri = null;
				return null;
			}
		}

		public static IClientChannelSinkProvider CreateClientSinkProvider()
		{
			return new BinaryClientFormatterSinkProvider();
		}

		public static IServerChannelSinkProvider CreateServerSinkProvider()
		{
			return new BinaryServerFormatterSinkProvider();
		}

		public static ChannelDataStore CreateChannelDataStore(string host, int port)
		{
			var url = string.Format("udp://{0}:{1}", host, port);
			return new ChannelDataStore(new[] { url });
		}
	}
}
