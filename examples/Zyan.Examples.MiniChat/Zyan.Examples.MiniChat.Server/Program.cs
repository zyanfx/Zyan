using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Examples.MiniChat.Shared;

namespace Zyan.Examples.MiniChat.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpDuplexServerProtocolSetup protocol = new TcpDuplexServerProtocolSetup(Properties.Settings.Default.TcpPort, new NullAuthenticationProvider(), false);

            using (ZyanComponentHost host = new ZyanComponentHost("MiniChat",protocol))
            {
                host.RegisterComponent<IMiniChat, MiniChat>(ActivationType.Singleton);
                Console.WriteLine("Chat server started. Press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
