using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zyan.Communication;
using Zyan.Communication.Protocols;
using Zyan.Communication.WcfTransport;
using Zyan.Examples.HelloWcfShared;

namespace Zyan.Examples.HelloWcfClient
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.Sleep(2000);

            using (var transport = new WcfClientTransportAdapter() { BaseAddress = "net.tcp://localhost:9091" })
            {
                var protocol = ClientProtocolSetup.WithTransportAdapter(x => transport);

                using (var connection = new ZyanConnection(transport.BaseAddress, protocol))
                {
                    var proxy = connection.CreateProxy<IEchoService>();
                    Console.WriteLine(proxy.Echo("Hello WCF"));
                    Console.ReadLine();
                }
            }
        }
    }
}
