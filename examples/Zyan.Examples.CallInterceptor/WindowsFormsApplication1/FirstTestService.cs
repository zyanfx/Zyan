using System;
using System.Windows.Forms;
using ServerInterfaces;

namespace FirstServerApplication
{
	public class FirstTestService : IFirstTestService
	{
		public event EventHandler<FirstTestEventArgs> Test;

		public void OnTest(FirstTestEventArgs args)
		{
			if (Test != null)
			{
				Test(null, args);
			}
		}

		public void TestMethod()
		{
			MessageBox.Show("Test Method");
		}
	}
}
