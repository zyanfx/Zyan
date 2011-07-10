using System;
using System.Linq;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Interface providing methods to get <see cref="IQueryable{T}"/>.
	/// </summary>
	public interface IQueryHandler
	{
		/// <summary>
		/// Returns an <see cref="IQueryable"/>.
		/// </summary>
		/// <param name="type">Type of the returned <see cref="IQueryable"/>.</param>
		/// <returns>Returns an <see cref="IQueryable"/>.</returns>
		IQueryable Get(Type type);

		/// <summary>
		/// Returns an <see cref="IQueryable{T}"/>.
		/// </summary>
		/// <typeparam name="T">Generic Argument of the returned <see cref="IQueryable{T}"/>.</typeparam>
		/// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
		IQueryable<T> Get<T>() where T : class;

		/// <summary>
		/// Tells the <see cref="IQueryHandler"/> to start a new the session.
		/// </summary>
		/// <returns>True, if the session creation was successful. False, if not.</returns>
		bool StartSession();

		/// <summary>
		/// Tells the <see cref="IQueryHandler"/> to close the current session.
		/// </summary>
		/// <returns>True, if the session closing was successful. False, if not.</returns>
		bool CloseSession();
	}
}
