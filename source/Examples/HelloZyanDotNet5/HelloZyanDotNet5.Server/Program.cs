using System;
using HelloZyanDotNet5.Shared;
using Zyan.Communication;
using Zyan.Communication.Protocols.Websocket;
using Zyan.Communication.Security;

namespace HelloZyanDotNet5.Server
{
	internal static class Program
	{
		public static void Main(string[] args)
		{
			using var host = new ZyanComponentHost(
				name: "HelloZyan.Server",
				protocolSetup: new WebsocketServerProtocolSetup
				{
					NetworkHostName = "localhost",
					TcpPort = 9091,
					MessageEncryption = false,
					KeySize = 4096,
					AuthenticationProvider = new NullAuthenticationProvider()
				});
			
			host.ComponentCatalog.RegisterComponent<IHelloService, HelloService>();
			
			Console.WriteLine("Server is running. Press [Enter] to quit.");
			Console.ReadLine();
		}
	}
}