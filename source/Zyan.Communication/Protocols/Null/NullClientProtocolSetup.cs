using System;
using System.Threading;

namespace Zyan.Communication.Protocols.Null
{
	/// <summary>
	/// <see cref="IClientProtocolSetup"/> implementation for the <see cref="NullChannel"/>.
	/// </summary>
	public class NullClientProtocolSetup : ClientProtocolSetup, IClientProtocolSetup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NullClientProtocolSetup"/> class.
		/// </summary>
		public NullClientProtocolSetup()
			: base((props, clientSinkProvider, serverSinkProvider) => new NullChannel(props, clientSinkProvider, serverSinkProvider))
		{
			_channelName = "NullClientChannel:" + Interlocked.Increment(ref FreePortCounter);
		}

		internal static int FreePortCounter;

		/// <summary>
		/// Formats the connection URL for this protocol.
		/// </summary>
		/// <param name="portNumber">The port number.</param>
		/// <param name="zyanHostName">Name of the zyan host.</param>
		/// <returns>
		/// Formatted URL supported by the protocol.
		/// </returns>
		public string FormatUrl(int portNumber, string zyanHostName)
		{
			return (this as IClientProtocolSetup).FormatUrl(portNumber, zyanHostName);
		}

		/// <summary>
		/// Formats the connection URL for this protocol.
		/// </summary>
		/// <param name="parts">The parts of the url, such as server name, port, etc.</param>
		/// <returns>
		/// Formatted URL supported by the protocol.
		/// </returns>
		string IClientProtocolSetup.FormatUrl(params object[] parts)
		{
			if (parts == null || parts.Length < 2)
				throw new ArgumentException(GetType().Name + " requires two arguments for URL: port number and ZyanHost name.");

			return string.Format("null://NullChannel:{0}/{1}", parts);
		}

		/// <summary>
		/// Checks whether the given URL is valid for this protocol.
		/// </summary>
		/// <param name="url">The URL to check.</param>
		/// <returns>
		/// True, if the URL is supported by the protocol, otherwise, False.
		/// </returns>
		public override bool IsUrlValid(string url)
		{
			return base.IsUrlValid(url) && url.StartsWith("null://NullChannel:");
		}
	}
}
