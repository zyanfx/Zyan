using System.Collections.Generic;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Interface required for the built-in Linq to objects support
	/// </summary>
	public interface IObjectSource : IBaseSource
	{
		/// <summary>
		/// Returns an <see cref="IEnumerable{T}"/>.
		/// </summary>
		/// <typeparam name="T">Generic Argument of the returned <see cref="IEnumerable{T}"/>.</typeparam>
		/// <returns>Returns an <see cref="IEnumerable{T}"/>.</returns>
		IEnumerable<T> Get<T>() where T : class;
	}
}
