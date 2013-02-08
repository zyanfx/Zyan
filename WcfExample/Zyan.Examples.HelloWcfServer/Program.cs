using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.WcfTransport;
using Zyan.Examples.HelloWcfShared;

namespace Zyan.Examples.HelloWcfServer
{
    public class EchoService : IEchoService
    {
        public string Echo(string text)
        {
            return string.Format("{0}: {1}", DateTime.Now, text);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (var transport = new WcfServerTransportAdapter() { BaseAddress = "net.tcp://localhost:9091" })
            { 
                var protocol = ServerProtocolSetup.WithChannel(x => transport);

                using (var host = new ZyanComponentHost("HelloWcf", protocol))
                {
                    host.RegisterComponent<IEchoService, EchoService>();
                    Console.WriteLine("Server läuft!");
                    Console.ReadLine();
                }
            }
        }
    }
}
