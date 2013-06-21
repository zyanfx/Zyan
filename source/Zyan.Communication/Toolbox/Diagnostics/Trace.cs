/*
 THIS CODE IS BASED ON:
 --------------------------------------------------------------------------------------------------------------
 TcpEx Remoting Channel

 Version 1.2 - 18 November, 2003
 Richard Mason - r.mason@qut.edu.au

 Originally published at GotDotNet:
 http://www.gotdotnet.com/Community/UserSamples/Details.aspx?SampleGuid=3F46C102-9970-48B1-9225-8758C38905B1

 Copyright © 2003 Richard Mason. All Rights Reserved.
 --------------------------------------------------------------------------------------------------------------
*/

namespace Zyan.Communication.Toolbox.Diagnostics
{
	internal class Trace
	{
		const string TraceCategory = "Zyan.Communication";

		[System.Diagnostics.Conditional("DEBUG")]
		public static void WriteLine(object o)
		{
			System.Diagnostics.Trace.WriteLine(o, TraceCategory);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void WriteLine(string format, params object[] args)
		{
			System.Diagnostics.Trace.WriteLine(string.Format(format, args), TraceCategory);
		}
	}
}
