using System;
using System.Linq;
using Zyan.InterLinq;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Abstract query handler implementation
	/// </summary>
	public abstract class ZyanQueryHandler : IQueryHandler
	{
		/// <summary>
		/// Returns an <see cref="IQueryable{T}"/>.
		/// </summary>
		/// <typeparam name="T">Generic Argument of the returned <see cref="IQueryable{T}"/>.</typeparam>
		/// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
		public IQueryable<T> Get<T>() where T : class
		{
			// should be implemented in ancestors
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns an <see cref="IQueryable{T}"/>.
		/// </summary>
		/// <typeparam name="T">Generic Argument of the returned <see cref="IQueryable{T}"/>.</typeparam>
		/// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
		public IQueryable Get(Type type)
		{
			var getTableMethod = GetType().GetMethod("Get", new Type[] { });
			var genericGetTableMethod = getTableMethod.MakeGenericMethod(type);
			return (IQueryable)genericGetTableMethod.Invoke(this, new object[] { });
		}

		/// <summary>
		/// Tells the <see cref="IQueryHandler"/> to close the current session.
		/// </summary>
		/// <returns>True, if the session closing was successful. False, if not.</returns>
		public virtual bool CloseSession()
		{
			return true;
		}

		/// <summary>
		/// Tells the <see cref="IQueryHandler"/> to start a new the session.
		/// </summary>
		/// <returns>True, if the session creation was successful. False, if not.</returns>
		public virtual bool StartSession()
		{
			return true;
		}
	}
}
