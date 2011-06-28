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

		public ZyanObjectQueryHandler(Func<Type, IEnumerable> handler)
		{
			if (handler == null)
			{
				throw new ArgumentNullException("handler");
			}

			EnumerableHandler = handler;
		}

		public ZyanObjectQueryHandler(IObjectSource objectSource)
		{
			if (objectSource == null)
			{
				throw new ArgumentNullException("objectSource");
			}

			ObjectSource = objectSource;
		}

		public IQueryable<T> Get<T>() where T : class
		{
			if (ObjectSource == null)
			{
				return EnumerableHandler(typeof(T)).OfType<T>().AsQueryable();
			}

			return ObjectSource.Get<T>().AsQueryable();
		}

		public IQueryable Get(Type type)
		{
			var getTableMethod = GetType().GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
			var genericGetTableMethod = getTableMethod.MakeGenericMethod(type);
			return (IQueryable)genericGetTableMethod.Invoke(this, new object[0]);
		}

		public IQueryable GetTable(Type type)
		{
			return Get(type);
		}

		public IQueryable<T> GetTable<T>() where T : class
		{
			return Get<T>();
		}

		public bool CloseSession()
		{
			return true;
		}

		public bool StartSession()
		{
			return true;
		}
	}
}
