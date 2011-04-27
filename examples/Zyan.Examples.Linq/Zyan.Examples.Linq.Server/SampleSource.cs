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
					yield return new FileInfo(s).As<T>();
				}

				yield break;
			}

			// Get<Assembly>() returns list of loaded assemblies
			if (typeof(T) == typeof(Assembly))
			{
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					yield return assembly.As<T>();
				}

				yield break;
			}

			throw new NotSupportedException(string.Format("Type {0} is not supported", typeof(T).Name));
		}
	}

	internal static class ObjectExtensions
	{
		public static T As<T>(this object obj)
		{
			return (T)obj;
		}
	}
}
