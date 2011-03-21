using System;
using System.Runtime.Remoting;
using DemonstrationObjects;

namespace ClientApp
{
	class Client
	{
		private delegate int DemoAsyncDelegate(int SomeValue);

		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length > 0 && args[0] == "-cao")
			{
				RemotingConfiguration.Configure(@"..\Client\clientCAO.exe.config");
			}
			else
			{
				RemotingConfiguration.Configure(@"..\Client\client.exe.config");
			}

			Demo DemoObj = new Demo();

			if (DemoObj == null)
			{
				Console.WriteLine("Error: DemoObj == null");
			}

			Console.WriteLine("\nDemonstrate a standard method call on a remote object");
			Console.WriteLine(DemoObj.PrintText("Hello from Client"));

			Console.WriteLine("\nDemonstrate an async method call on a remote object");
			Console.WriteLine("Client Value Sent to Server: {0}", 10);
			DemoAsyncDelegate Del = new DemoAsyncDelegate(DemoObj.TestAsync);
			IAsyncResult Result = Del.BeginInvoke(10, null, null);

			Result.AsyncWaitHandle.WaitOne(30000, false);
			Console.WriteLine("Client Value + 10 Added by Server: " + (Del.EndInvoke(Result)).ToString());

			Console.WriteLine("\nDemonstrate a one way async method call on a remote object");
			Console.WriteLine("Returns immediatetly");
			DemoObj.TestOneWay();
		}
	}
}
