using System;
using InterLinq.Communication.Wcf;
using InterLinq.Examples.Simple.Artefacts;
using InterLinq.Objects;

namespace InterLinq.Examples.Simple.Server
{
    /// <summary>
    /// This class contains an Example of a server application using InterLINQ to share access to a data base (a list of object in this case).
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main method.
        /// </summary>
        static void Main()
        {
            Console.WriteLine("InterLinq.Examples.Simple.Server is going to start...");

            // Create an ExampleObjectSource
            Console.WriteLine("Creating an ExampleObjectSource...");
            ExampleObjectSource exampleObjectSource = new ExampleObjectSource();

            // Create 20 SimpleObjects and store them in the objectDataSource
            Console.WriteLine("Creating some SimpleObjects into ExampleObjectSource...");
            for (int i = 0; i < 20; i++)
            {
                SimpleObject simpleObject = new SimpleObject();
                simpleObject.Name = string.Format("Object #{0}", i);
                simpleObject.Value = i;
                exampleObjectSource.SimpleObjects.Add(simpleObject);
            }

            // Create a IQueryHandler for requests sent to this server
            Console.WriteLine("Creating an ObjectQueryHandler...");
            IQueryHandler queryHandler = new ObjectQueryHandler(exampleObjectSource);

            // Publish the IQueryHandler by InterLINQ over WCF
            Console.WriteLine("Publishing service...");
            using (ServerQueryWcfHandler serverQueryHandler = new ServerQueryWcfHandler(queryHandler))
            {
                serverQueryHandler.Start(true);
                Console.WriteLine("Server is started and running.");
                Console.WriteLine();

                // Wait for user input
                Console.WriteLine("Press [Enter] to quit.");
                Console.ReadLine();

                // Close the service
                Console.WriteLine("Closing service...");
            }
            Console.WriteLine("Bye");
        }
    }
}
