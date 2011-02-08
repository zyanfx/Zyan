using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;
using Zyan.Examples.MiniChat.Shared;

namespace Zyan.Examples.MiniChat.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ZyanComponentHost host = new ZyanComponentHost("MiniChat", Properties.Settings.Default.TcpPort))
            {
                host.RegisterComponent<IMiniChat, MiniChat>(ActivationType.Singleton);
                Console.ReadLine();
            }
        }
    }
}
