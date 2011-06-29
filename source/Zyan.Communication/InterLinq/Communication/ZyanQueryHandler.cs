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
		public IQueryable<T> Get<T>() where T : class
		{
			// should be implemented in ancestors
			throw new NotImplementedException();
		}

		public IQueryable Get(Type type)
		{
			var getTableMethod = GetType().GetMethod("Get", new Type[] { });
			var genericGetTableMethod = getTableMethod.MakeGenericMethod(type);
			return (IQueryable)genericGetTableMethod.Invoke(this, new object[] { });
		}

		public virtual bool CloseSession()
		{
			return true;
		}

		public virtual bool StartSession()
		{
			return true;
		}
	}
}
