using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Threading;
using IDictionary = System.Collections.IDictionary;

namespace Zyan.Communication.Protocols.Null
{
	/// <summary>
	/// <see cref="IClientProtocolSetup"/> implementation for the <see cref="NullChannel"/>.
	/// </summary>
	public class NullClientProtocolSetup : ClientProtocolSetup
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
	}
}
