using System.Collections.Generic;
using System.Linq;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Interface required for the built-in Linq to entities support.
	/// </summary>
	public interface IEntitySource : IBaseSource
	{
		/// <summary>
		/// Returns an <see cref="IQueryable{T}"/>.
		/// </summary>
		/// <typeparam name="T">Generic Argument of the returned <see cref="IQueryable{T}"/>.</typeparam>
		/// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
		IQueryable<T> Get<T>() where T : class;
	}
}
