using System;
using System.Collections;
using System.IO;
using InterLinq;
using Zyan.Communication;
using Zyan.Examples.Linq.Server.Properties;
using Zyan.InterLinq;
using System.Runtime.Remoting.Contexts;

namespace Zyan.Examples.Linq.Server
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var host = new ZyanComponentHost(Settings.Default.ServiceUri, Settings.Default.TcpPort))
			{
				// default queryable service
				host.RegisterQueryableComponent(() => new SampleSource());

				// named queryable service
				host.RegisterQueryableComponent("DesktopService", t => GetDesktopData(t));

				// buggy service (to test error handling)
				host.RegisterComponent<IDynamicProperty, BuggyService>("BuggyService");

				Console.WriteLine("Linq server started. Press ENTER to quit.");
				Console.ReadLine();
			}
		}

		/// <summary>
		/// Query handler for DesktopService
		/// Any Func{Type, IEnumerable} or Func{Type, IQueryable} can be used as remote query handler, i.e:
		/// host.RegisterQueryableComponent(t => GetData(t));
		/// </summary>
		/// <param name="t">Element type</param>
		static IEnumerable GetDesktopData(Type t)
		{
			var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

			// query desktop files
			if (t == typeof(FileInfo))
			{
				foreach (string s in Directory.GetFiles(folder))
				{
					yield return new FileInfo(s);
				}

				yield break;
			}

			// query folders
			if (t == typeof(DirectoryInfo))
			{
				foreach (string s in Directory.GetDirectories(folder))
				{
					yield return new DirectoryInfo(s);
				}

				yield break;
			}

			throw new NotSupportedException(string.Format("Type {0} is not supported", t.Name));
		}
	}
}
