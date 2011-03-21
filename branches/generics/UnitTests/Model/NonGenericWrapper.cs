using System;
using System.Diagnostics;
using GenericWrappers;

namespace UnitTests.Model
{
	class NonGenericWrapper : INonGenericInterface
	{
		// actual generic object
		IGenericInterface instance;

		// generic method holders
		GenericMethodHolder getDefaultHolder;
		GenericMethodHolder equalsHolder;
		GenericMethodHolder doSomethingHolder;
		GenericMethodHolder computeHolder;

		public NonGenericWrapper(IGenericInterface remote)
		{
			instance = remote;

			// wrap generic methods
			getDefaultHolder = new GenericMethodHolder(typeof(IGenericInterface), "GetDefault", 1, new Type[0]);
			equalsHolder = new GenericMethodHolder(typeof(IGenericInterface), "Equals", 1, new Type[] { null, null });
			doSomethingHolder = new GenericMethodHolder(typeof(IGenericInterface), "DoSomething", 3, new Type[] { null, null, null });
			computeHolder = new GenericMethodHolder(typeof(IGenericInterface), "Compute", 2, new Type[] { null, typeof(int), typeof(string) });
		}

		public object GetDefault_T(Type t1)
		{
			return getDefaultHolder.Invoke(instance, new[] { t1 }, new object[0]);
		}

		public bool Equals_T(Type t1, object a, object b)
		{
			return (bool)equalsHolder.Invoke(instance, new[] { t1 }, new[] { a, b });
		}

		public void DoSomething_A_B_C(Type A, Type B, Type C, object a, object b, object c)
		{
			doSomethingHolder.Invoke(instance, new[] { A, B, C }, a, b, c);
		}

		public object Compute_A_B(Type A, Type B, object a, int b, string c)
		{
			return computeHolder.Invoke(instance, new[] { A, B }, a, b, c);
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
