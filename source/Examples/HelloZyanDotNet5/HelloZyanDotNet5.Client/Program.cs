using System;
using HelloZyanDotNet5.Shared;
using Zyan.Communication;
using Zyan.Communication.Protocols.Websocket;

namespace HelloZyanDotNet5.Client
{
	internal static class Program
	{
		public static void Main(string[] args)
		{
			using var connection = new ZyanConnection(
				serverUrl: "http://localhost:9091/HelloZyan.Server",
				protocolSetup: new WebsocketClientProtocolSetup
				{
					ServerTcpPort = 9091,
					ServerHostName = "localhost",
					MessageEncryption = false,
					KeySize = 4096
				});

			var helloServiceProxy = connection.CreateProxy<IHelloService>();
			
			Console.Write("What's your name:");
			var name = Console.ReadLine();

			var greeting = helloServiceProxy.Greet(name);
			
			Console.WriteLine(greeting);
			Console.ReadLine();
		}
	}
}