using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

				Console.WriteLine("Files larger than 1 kb:");
				foreach (var fi in files)
				{
					Console.WriteLine("{0,15:#,0} | {1}", fi.Length, fi.Name);
				}

				Console.ReadLine();
			}
		}
	}
}
