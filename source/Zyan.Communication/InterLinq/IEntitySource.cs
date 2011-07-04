using System.Collections.Generic;
using System.Linq;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Interface required for the built-in Linq to entities support.
	/// </summary>
	public interface IEntitySource : IBaseSource
	{
		IQueryable<T> Get<T>() where T : class;
	}
}
