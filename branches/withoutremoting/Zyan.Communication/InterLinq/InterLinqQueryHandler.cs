using System;
using System.Linq;
using System.Reflection;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Abstract implementation of an <see cref="IQueryHandler"/>.
	/// This class provides methods to get an <see cref="IQueryable{T}"/>.
	/// </summary>
	/// <seealso cref="IQueryHandler"/>
	public abstract class InterLinqQueryHandler : IQueryHandler
	{
		#region Fields

		/// <summary>
		/// Gets the <see cref="IQueryProvider"/>.
		/// </summary>
		public abstract IQueryProvider QueryProvider { get; }

		#endregion

		#region IQueryHandler Members

		/// <summary>
		/// Returns an <see cref="IQueryable{T}"/>.
		/// </summary>
		/// <param name="type">Type of the returned <see cref="IQueryable{T}"/>.</param>
		/// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
		public IQueryable Get(Type type)
		{
			MethodInfo getTableMethod = GetType().GetMethod("Get", new Type[] { });
			MethodInfo genericGetTableMethod = getTableMethod.MakeGenericMethod(type);
			return (IQueryable)genericGetTableMethod.Invoke(this, new object[] { });
		}

		/// <summary>
		/// Returns an <see cref="IQueryable{T}"/>.
		/// </summary>
		/// <typeparam name="T">Generic Argument of the returned <see cref="IQueryable{T}"/>.</typeparam>
		/// <returns>Returns an <see cref="IQueryable{T}"/>.</returns>
		public IQueryable<T> Get<T>() where T : class
		{
			return new InterLinqQuery<T>(QueryProvider);
		}

		/// <summary>
		/// Tells the <see cref="IQueryHandler"/> to start a new the session.
		/// </summary>
		/// <returns>True, if the session creation was successful. False, if not.</returns>
		/// <seealso cref="IQueryHandler.StartSession"/>
		public virtual bool StartSession()
		{
			return true;
		}

		/// <summary>
		/// Tells the <see cref="IQueryHandler"/> to close the current session.
		/// </summary>
		/// <returns>True, if the session closing was successful. False, if not.</returns>
		/// <seealso cref="IQueryHandler.CloseSession"/>
		public virtual bool CloseSession()
		{
			return true;
		}

		#endregion
	}
}
