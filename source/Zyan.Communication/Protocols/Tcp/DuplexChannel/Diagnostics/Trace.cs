using System;

namespace Zyan.Communication.Protocols.Tcp.DuplexChannel.Diagnostics
{
	public class Trace
	{
		const string TraceCategory = "TcpEx";

		public static void WriteLine(object o)
		{
			System.Diagnostics.Trace.WriteLine(o, TraceCategory);
		}

		public static void WriteLine(string format, params object[] args)
		{
			System.Diagnostics.Trace.WriteLine(string.Format(format, args), TraceCategory);
		}
	}
}
