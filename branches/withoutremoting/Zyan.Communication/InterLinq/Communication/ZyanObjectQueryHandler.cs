using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Zyan.InterLinq;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Simple IEnumerable POCO server query handler
	/// </summary>
	public class ZyanObjectQueryHandler : IQueryHandler
	{
		Func<Type, IEnumerable> EnumerableHandler { get; set; }

		IObjectSource ObjectSource { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanObjectQueryHandler"/> class.
		/// </summary>
		/// <param name="handler">The query handler (returns <see cref="IEnumerable"/> for the given <see cref="Type"/>).</param>
		public ZyanObjectQueryHandler(Func<Type, IEnumerable> handler)
		{
			if (handler == null)
			{
				throw new ArgumentNullException("handler");
			}

			EnumerableHandler = handler;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanObjectQueryHandler"/> class.
		/// </summary>
		/// <param name="objectSource">The object source.</param>
		public ZyanObjectQueryHandler(IObjectSource objectSource)
		{
			if (objectSource == null)
			{
				throw new ArgumentNullException("objectSource");
			}

			ObjectSource = objectSource;
		}

		/// <summary>
		/// Returns an <see cref="IQueryable{T}"/>.
		/// </summary>
		/// <typeparam name="T">Generic Argument of the returned <see cref="IQueryable{T}"/>.</typeparam>
		/// <returns>
		/// Returns an <see cref="IQueryable{T}"/>.
		/// </returns>
		public IQueryable<T> Get<T>() where T : class
		{
			if (ObjectSource == null)
			{
				return EnumerableHandler(typeof(T)).OfType<T>().AsQueryable();
			}

			return ObjectSource.Get<T>().AsQueryable();
		}

		/// <summary>
		/// Returns an <see cref="IQueryable"/>.
		/// </summary>
		/// <param name="type">Type of the returned <see cref="IQueryable"/>.</param>
		/// <returns>
		/// Returns an <see cref="IQueryable"/>.
		/// </returns>
		public IQueryable Get(Type type)
		{
			var getTableMethod = GetType().GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
			var genericGetTableMethod = getTableMethod.MakeGenericMethod(type);
			return (IQueryable)genericGetTableMethod.Invoke(this, new object[0]);
		}

		/// <summary>
		/// Tells the <see cref="IQueryHandler"/> to close the current session.
		/// </summary>
		/// <returns>
		/// True, if the session closing was successful. False, if not.
		/// </returns>
		public bool CloseSession()
		{
			return true;
		}

		/// <summary>
		/// Tells the <see cref="IQueryHandler"/> to start a new the session.
		/// </summary>
		/// <returns>
		/// True, if the session creation was successful. False, if not.
		/// </returns>
		public bool StartSession()
		{
			return true;
		}
	}
}
