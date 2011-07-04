using System;
using System.Linq;
using MemoDb;
using Zyan.InterLinq;

namespace Zyan.Tests
{
	/// <summary>
	/// In-memory database wrapper for Linq unit-tests
	/// </summary>
	public class DataWrapper : IEntitySource, IDisposable
	{
		static Memo Db { get; set; }

		public MemoSession Session { get; private set; }

		public DataWrapper()
		{
			Session = Db.CreateSession();
		}

		public IQueryable<T> Get<T>() where T : class
		{
			return Session.Query<T>();
		}

		public void Dispose()
		{
			Session.Dispose();
		}

		static DataWrapper()
		{
			Db = new Memo();
			Db.Map<SampleEntity>();

			// populate in-memory database
			using (var s = Db.CreateSession())
			{
				Array.ForEach(SampleEntity.GetSampleEntities().ToArray(), e => s.Insert(e));
				s.Flush();
			}
		}
	}
}
