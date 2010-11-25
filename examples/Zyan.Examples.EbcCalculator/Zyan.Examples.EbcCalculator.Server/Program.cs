using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;
using Zyan.Communication.SessionMgmt;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols.Http;

namespace Zyan.Examples.EbcCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpCustomServerProtocolSetup protocol = new HttpCustomServerProtocolSetup(8081, new BasicWindowsAuthProvider(), true);
            
            using (ZyanComponentHost host = new ZyanComponentHost("EbcCalc", protocol))
            {
                host.RegisterComponent<ICalculator, Calculator>();
                
                Console.ReadLine();
            }
        }
    }
}
