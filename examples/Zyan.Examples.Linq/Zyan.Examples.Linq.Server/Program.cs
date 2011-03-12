using System;
using System.Collections;
using System.IO;
using InterLinq;
using Zyan.Communication;
using Zyan.Examples.Linq.Server.Properties;
using Zyan.InterLinq;

namespace Zyan.Examples.Linq.Server
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var host = new ZyanComponentHost(Settings.Default.ServiceUri, Settings.Default.TcpPort))
			{
				host.RegisterComponent<IQueryRemoteHandler>(() => new ZyanServerQueryHandler(new SampleSource()));

				Console.WriteLine("Linq server started. Press ENTER to quit.");
				Console.ReadLine();
			}
		}

		/// <summary>
		/// Any method matching Func{Type, IEnumerable} delegate can be used as remote query handler, i.e:
		/// host.RegisterComponent{IQueryRemoteHandler}(() => new ZyanServerQueryHandler(GetData));
		/// </summary>
		/// <param name="t">IEnumerable element type</param>
		static IEnumerable GetData(Type t)
		{
			if (t == typeof(FileInfo))
			{
				var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				foreach (string s in Directory.GetFiles(folder))
				{
					var fi = new FileInfo(s);
					yield return fi;
				}

				yield break;
			}

			throw new NotSupportedException(string.Format("Type {0} is not supported", t.Name));
		}
	}
}
