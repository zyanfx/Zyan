using System;

namespace ServerInterfaces
{
    public interface IFirstTestService
    {
		event EventHandler<FirstTestEventArgs> Test;
		void TestMethod();
    }
}
