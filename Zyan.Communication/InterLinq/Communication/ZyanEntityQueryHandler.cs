using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Zyan.InterLinq;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Simple IQueryable server query handler
	/// </summary>
	public class ZyanEntityQueryHandler : IQueryHandler
	{
		Func<Type, IQueryable> QueryableHandler { get; set; }

		IEntitySource EntitySource { get; set; }

		public ZyanEntityQueryHandler(Func<Type, IQueryable> handler)
		{
			if (handler == null)
			{
				throw new ArgumentNullException("handler");
			}

			QueryableHandler = handler;
		}

		public ZyanEntityQueryHandler(IEntitySource entitySource)
		{
			if (entitySource == null)
			{
				throw new ArgumentNullException("entitySource");
			}

			EntitySource = entitySource;
		}

		public IQueryable<T> Get<T>() where T : class
		{
			if (EntitySource == null)
			{
				return QueryableHandler(typeof(T)).OfType<T>();
			}

			return EntitySource.Get<T>().AsQueryable();
		}

		public IQueryable Get(Type type)
		{
			var getTableMethod = GetType().GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
			var genericGetTableMethod = getTableMethod.MakeGenericMethod(type);
			return (IQueryable)genericGetTableMethod.Invoke(this, new object[0]);
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
