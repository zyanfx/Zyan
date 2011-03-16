using System;
using Zyan.Communication;
using Zyan.Examples.DynamicEbcResponses.Shared;

namespace Zyan.Examples.DynamicEbcResponses.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // create a new Zyan ComponentHost
            using (var host = new ZyanComponentHost("DynamicEbcResponses", 4567))
            {
                // register the service implementation by its interface
                host.RegisterComponent<IService>(
                                                    () => new Service(), 
                                                    ActivationType.SingleCall
                                                );
                
                // print information and keep server process running
                Console.WriteLine("Stated Zyan Server on localhost:4567, press any key to stop.");
                Console.ReadKey();
            }
        }
    }
}
