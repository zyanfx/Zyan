using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;

namespace Zyan.Examples.EbcCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ZyanComponentHost host = new ZyanComponentHost("EbcCalc", 8081))
            {
                host.RegisterComponent<ICalculator, Calculator>();
                
                Console.ReadLine();
            }
        }
    }
}
