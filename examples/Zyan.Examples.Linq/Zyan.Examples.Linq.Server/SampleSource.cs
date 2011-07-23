using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zyan.Examples.Linq.Interfaces;

namespace Zyan.Examples.Linq.Server
{
	/// <summary>
	/// Sample queryable component.
	/// </summary>
	public class SampleSource : ISampleSource
	{
		/// <summary>
		/// Returns service assembly version.
		/// </summary>
		public string GetVersion()
		{
			return GetType().Assembly.GetName().Version.ToString();
		}

		/// <summary>
		/// Returns queryable information related to server process.
		/// </summary>
		public IEnumerable<T> GetProcessInfo<T>() where T : class
		{
			// Get<FileInfo>() returns file list of the current folder
			if (typeof(T) == typeof(FileInfo))
			{
				var list =
					from fileName in Directory.GetFiles(".")
					select new FileInfo(fileName);

				return list.OfType<T>();
			}

			// Get<Assembly>() returns list of loaded assemblies
			if (typeof(T) == typeof(Assembly))
			{
				// exclude dynamic assemblies (they are not serializable)
				// return only .NET framework assemblies
				var list =
					from asm in AppDomain.CurrentDomain.GetAssemblies()
					let name = asm.FullName
					where !(asm is AssemblyBuilder) &&
						(name.Contains("System") || name.Contains("Microsoft") || name.Contains("corlib"))
					select asm;

				return list.OfType<T>();
			}

			throw new NotSupportedException(string.Format("Type {0} is not supported", typeof(T).Name));
		}

		/// <summary>
		/// Returns queryable information about server user's desktop folder.
		/// </summary>
		public IQueryable<T> GetDesktopInfo<T>() where T : class
		{
			var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

			// query desktop files
			if (typeof(T) == typeof(FileInfo))
			{
				var list =
					from fileName in Directory.GetFiles(folder)
					select new FileInfo(fileName);

				return list.OfType<T>().AsQueryable();
			}

			// query desktop folders
			if (typeof(T) == typeof(DirectoryInfo))
			{
				var list =
					from dirName in Directory.GetDirectories(folder)
					select new DirectoryInfo(dirName);
				
				return list.OfType<T>().AsQueryable();
			}

			throw new NotSupportedException(string.Format("Type {0} is not supported", typeof(T).Name));
		}
	}
}
