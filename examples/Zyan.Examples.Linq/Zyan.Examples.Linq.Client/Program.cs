using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using Zyan.Communication;
using Zyan.Examples.Linq.Client.Properties;
using Zyan.InterLinq;

namespace Zyan.Examples.Linq.Client
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var conn = new ZyanConnection(Settings.Default.ServerUrl))
			{
				// add global error handler
				conn.Error += ConnectionErrorHandler;

				// create proxy for the default queryable service
				var proxy = conn.CreateQueryableProxy();

				// system assemblies loaded by server
				var assemblies =
					from asm in proxy.Get<Assembly>()
					where asm.FullName.ToLower().StartsWith("system")
					orderby asm.GetName().Name.Length ascending
					select asm;

				Console.WriteLine("System assemblies loaded by server:");
				foreach (var asm in assemblies)
				{
					Console.WriteLine("  {0} -> {1}", asm.GetName().Name, asm.ManifestModule.Name);
				}

				Console.WriteLine();

				// requesting list of files in server's MyDocuments folder
				var files =
					from file in proxy.Get<FileInfo>()
					where file.Length > 1024
					select new 
					{
						file.Name,
						file.Length
					};

				Console.WriteLine("Documents larger than 1 kb:");
				foreach (var fi in files)
				{
					Console.WriteLine("{0,15:#,0} | {1}", fi.Length, fi.Name);
				}

				Console.WriteLine();

				// request files in server's desktop folder
				var deskProxy = conn.CreateQueryableProxy("DesktopService");

				// requesting list of files in server's MyDocuments folder
				var links =
					from file in deskProxy.Get<FileInfo>()
					where file.Name.EndsWith(".lnk")
					orderby file.Name.Length
					select file.Name;

				Console.WriteLine("Desktop links:");
				foreach (var link in links)
				{
					Console.WriteLine("\t{0}", link);
				}

				Console.WriteLine();

				// test error handling
				var buggyProxy = conn.CreateProxy<IDynamicProperty>("BuggyService");
				Console.WriteLine("BuggyService.Name returns: {0}", buggyProxy.Name);

				Console.WriteLine();
				Console.WriteLine("Press ENTER to quit.");
				Console.ReadLine();
			}
		}

		static void ConnectionErrorHandler(object sender, ZyanErrorEventArgs args)
		{
			var exception = args.Exception.InnerException ?? args.Exception;
			Console.WriteLine("Exception caught: {0}", exception.Message);
			Console.WriteLine("Retry (default)? Ignore? Throw exception?");

			var c = char.ToUpperInvariant(Console.ReadKey(true).KeyChar);
			switch (c)
			{
				case 'I':
					args.Action = ZyanErrorAction.Ignore;
					break;

				case 'T':
					args.Action = ZyanErrorAction.ThrowException;
					break;

				default:
					args.Action = ZyanErrorAction.Retry;
					break;
			}
		}
	}
}
