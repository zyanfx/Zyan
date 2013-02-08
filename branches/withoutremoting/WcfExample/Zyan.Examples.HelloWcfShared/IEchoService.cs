using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyan.Examples.HelloWcfShared
{
    public interface IEchoService
    {
        string Echo(string text);
    }
}
