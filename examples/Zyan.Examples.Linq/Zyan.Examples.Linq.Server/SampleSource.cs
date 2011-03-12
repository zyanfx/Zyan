using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Zyan.InterLinq;

namespace Zyan.Examples.Linq.Server
{
	public class SampleSource : IObjectSource
	{
		public IEnumerable<T> Get<T>() where T : class
		{
			// Get<FileInfo>() returns file list of MyDocuments folder
			if (typeof(T) == typeof(FileInfo))
			{
				var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				foreach (var s in Directory.GetFiles(folder))
				{
					var fi = new FileInfo(s);
					yield return (T)(object)fi;
				}

				yield break;
			}

			// Get<Assembly>() returns list of loaded assemblies
			if (typeof(T) == typeof(Assembly))
			{
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					var asm = (T)(object)assembly;
					yield return asm;
				}

				yield break;
			}

			throw new NotSupportedException(string.Format("Type {0} is not supported", typeof(T).Name));
		}
	}
}
