using System;
using System.Diagnostics;

namespace UnitTests.Model
{
	class GenericWrapper : IGenericInterface
	{
		INonGenericInterface instance;

		public GenericWrapper(INonGenericInterface remote)
		{
			instance = remote;
		}

		public T GetDefault<T>()
		{
			return (T)(object)instance.GetDefault_T(typeof(T));
		}

		public bool Equals<T>(T a, T b)
		{
			return instance.Equals_T(typeof(T), a, b);
		}

		public void DoSomething<A, B, C>(A a, B b, C c)
		{
			instance.DoSomething_A_B_C(typeof(A), typeof(B), typeof(C), a, b, c);
		}

		public B Compute<A, B>(A a, int b, string c)
		{
			return (B)(object)instance.Compute_A_B(typeof(A), typeof(B), a, b, c);
		}

		public string GetVersion()
		{
			return instance.GetVersion();
		}

		public Guid CreateGuid(DateTime time, int seed)
		{
			return instance.CreateGuid(time, seed);
		}

		public DateTime LastDate
		{
			get { return instance.LastDate; }
			set { instance.LastDate = value; }
		}

		public string Name
		{
			get { return instance.Name; }
		}

		public int Priority
		{
			set
			{
				instance.Priority = value;
			}
		}

		public event EventHandler<EventArgs> OnPrioritySet
		{
			add
			{
				instance.OnPrioritySet += value;
			}
			remove
			{
				instance.OnPrioritySet -= value;
			}
		}
	}
}
