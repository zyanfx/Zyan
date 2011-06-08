using System;
using System.Linq;
using InterLinq.Communication.Wcf;
using InterLinq.Examples.Simple.Artefacts;

namespace InterLinq.Examples.Simple.Client
{
    /// <summary>
    /// This class contains an Example of a client application using InterLINQ.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The main method.
        /// </summary>
        static void Main()
        {
            Console.WriteLine("InterLinq.Examples.Simple.Client is going to start...");

            // Connect to the server
            // using the default connection (TCP-Binding, Binary, tcp://localhost:7890/InterLinqService)
            Console.WriteLine("Connecting to the server...");
            ClientQueryWcfHandler clientQueryHandler = new ClientQueryWcfHandler();
            clientQueryHandler.Connect("InterLinqServiceNetTcp");

            // Create a SimpleExampleContext
            Console.WriteLine("Creating a SimpleExampleContext...");
            SimpleExampleContext simpleExampleContext = new SimpleExampleContext(clientQueryHandler);
            Console.WriteLine("Client is connected.");
            Console.WriteLine();

            #region Execute some LINQ statements
            Console.WriteLine("Execute some LINQ statements...");

            #region Query 1

            var selectAllSimpleObjects = from so in simpleExampleContext.SimpleObjects
                                         select so;
            Console.WriteLine("{0} SimpleObjects are stored on the server.", selectAllSimpleObjects.Count());

            #endregion

            #region Query 2

            var valueOver9 = from so in simpleExampleContext.SimpleObjects
                             where so.Value > 9
                             select so;
            Console.WriteLine("{0} SimpleObjects have a value over 9.", valueOver9.Count());

            #endregion

            #region Query 3

            SimpleObject lastObject = (from so in simpleExampleContext.SimpleObjects
                                       select so).Last();
            Console.WriteLine("The name of the last Object is '{0}' and the value '{1}'.", lastObject.Name, lastObject.Value);

            #endregion

            #endregion

            // Wait for user input
            Console.WriteLine();
            Console.WriteLine("Press [Enter] to quit.");
            Console.ReadLine();
        }
    }
}
