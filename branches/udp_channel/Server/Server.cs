using System;
using System.Runtime.Remoting;

namespace ServerApp
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Server
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length > 0 && args[0] == "-cao")
			{
				RemotingConfiguration.Configure(@"..\Server\ServerCAO.exe.config");
			}
			else
			{
				RemotingConfiguration.Configure(@"..\Server\Server.exe.config");
			}

			// keep running until told to quit
			System.Console.WriteLine("Press enter to exit");
			System.Console.ReadLine();
		}
	}
}
