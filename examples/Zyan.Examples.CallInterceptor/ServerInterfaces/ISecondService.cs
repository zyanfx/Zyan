using System;

namespace ServerInterfaces
{
    public interface ISecondTestService
    {
		event EventHandler<SecondTestEventArgs> Test;
    }
}
