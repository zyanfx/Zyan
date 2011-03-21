using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests.Model
{
	public interface INonGenericInterface
	{
		object GetDefault_T(Type t1);

		bool Equals_T(Type t1, object a, object b);

		void DoSomething_A_B_C(Type A, Type B, Type C, object a, object b, object c);

		object Compute_A_B(Type A, Type B, object a, int b, string c);

		string GetVersion();

		Guid CreateGuid(DateTime time, int seed);

		DateTime LastDate { get; set; }

		string Name { get; }

		int Priority { set; }

		event EventHandler<EventArgs> OnPrioritySet;
	}
}
