using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Tests
{
	public interface ISampleMethodSource
	{
		IQueryable<T> GetTable<T>() where T : class;
	}
}
