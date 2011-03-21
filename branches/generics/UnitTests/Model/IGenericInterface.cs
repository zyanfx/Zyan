using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests.Model
{
	public interface IGenericInterface
	{
		T GetDefault<T>();

		bool Equals<T>(T a, T b);

		void DoSomething<A, B, C>(A a, B b, C c);

		B Compute<A, B>(A a, int b, string c);

		string GetVersion();

		Guid CreateGuid(DateTime time, int seed);

		DateTime LastDate { get; set; }

		string Name { get; }

		int Priority { set; }

		event EventHandler<EventArgs> OnPrioritySet;
	}
}
