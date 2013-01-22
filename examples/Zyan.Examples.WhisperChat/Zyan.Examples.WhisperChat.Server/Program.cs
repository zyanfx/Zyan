using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Examples.WhisperChat.Shared;

namespace Zyan.Examples.WhisperChat.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var protocol = new TcpDuplexServerProtocolSetup(8081,null,true);

            using (var host = new ZyanComponentHost("WhisperChat", protocol))
            {
                host.RegisterComponent<IWhisperChatService, WhisperChatService>(ActivationType.SingleCall);

                Console.WriteLine("Server running.");
                Console.ReadLine();
            }
        }
    }
}
