using System;
using System.Diagnostics;

namespace UnitTests.Model
{
	class GenericClass : IGenericInterface
	{
		string version = "DoSomething wasn't called yet";
		DateTime lastDate = DateTime.Now;
		int priority = 123;

		public T GetDefault<T>()
		{
			return default(T);
		}

		public bool Equals<T>(T a, T b)
		{
			return a.Equals(b);
		}

		public void DoSomething<A, B, C>(A a, B b, C c)
		{
			version = string.Format("DoSomething: A = {0}, B = {1}, C = {2}", a, b, c);
		}

		public B Compute<A, B>(A a, int b, string c)
		{
			return default(B);
		}

		public string GetVersion()
		{
			return version;
		}

		public Guid CreateGuid(DateTime time, int seed)
		{
			lastDate = time;
			var bytes = BitConverter.GetBytes(time.TimeOfDay.Ticks);
			return new Guid(seed, (short)time.Month, (short)time.Day, bytes);
		}

		public DateTime LastDate
		{
			get { return lastDate; }
			set { lastDate = value; }
		}

		public string Name
		{
			get { return "GenericClass, priority = " + priority; }
		}

		public int Priority
		{
			set
			{
				priority = value;

				if (OnPrioritySet != null)
				{
					OnPrioritySet(this, EventArgs.Empty);
				}
			}
		}

		public event EventHandler<EventArgs> OnPrioritySet;
	}
}
