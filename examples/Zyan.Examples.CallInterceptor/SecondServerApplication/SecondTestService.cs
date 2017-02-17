using System;
using ServerInterfaces;

namespace SecondServerApplication
{
	public class SecondTestService : ISecondTestService
	{
		public event EventHandler<SecondTestEventArgs> Test;

		public void OnTest(SecondTestEventArgs args)
		{
			if (Test != null)
			{
				Test(null, args);
			}
		}
	}
}
