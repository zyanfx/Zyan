using System.Linq;
using System.Collections.Generic;

namespace Zyan.Examples.Linq.Interfaces
{
	/// <summary>
	/// Sample queryable data source interface.
	/// </summary>
	public interface ISampleSource
	{
		/// <summary>
		/// Returns service assembly version.
		/// </summary>
		string GetVersion();

		/// <summary>
		/// Returns queryable information related to server process.
		/// </summary>
		IEnumerable<T> GetProcessInfo<T>() where T : class;

		/// <summary>
		/// Returns queryable information about server user's desktop folder.
		/// </summary>
		IQueryable<T> GetDesktopInfo<T>() where T : class;
	}
}
