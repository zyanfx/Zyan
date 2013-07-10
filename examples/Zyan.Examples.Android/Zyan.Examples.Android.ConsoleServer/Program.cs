using System;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;
using Zyan.Examples.Android.Shared;

namespace Zyan.Examples.Android.ConsoleServer
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var protocol = new TcpDuplexServerProtocolSetup(12345, new NullAuthenticationProvider(), true);
			using (var host = new ZyanComponentHost("Sample", protocol))
			{
				host.RegisterComponent<ISampleService, SampleService>();
				Console.WriteLine("Server started. Press Enter to exit.");
				Console.ReadLine();
			}
		}
	}
}
