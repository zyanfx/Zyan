using System.Collections.Generic;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Interface required for the built-in Linq to objects support
	/// </summary>
	public interface IObjectSource : IBaseSource
	{
		IEnumerable<T> Get<T>() where T : class;
	}
}
