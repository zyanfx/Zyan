using System;
using Zyan.Communication;
using Zyan.Examples.Linq.Interfaces;
using Zyan.Examples.Linq.Server.Properties;

namespace Zyan.Examples.Linq.Server
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var host = new ZyanComponentHost(Settings.Default.ServiceUri, Settings.Default.TcpPort))
			{
				// queryable service
				host.RegisterComponent<ISampleSource, SampleSource>();

				// buggy service (to demonstrate error handling)
				host.RegisterComponent<INamedService, BuggyService>("BuggyService");

				Console.WriteLine("Linq server started. Press ENTER to quit.");
				Console.ReadLine();
			}
		}
	}
}
