using System;
using Zyan.Communication;
using Zyan.Examples.DynamicEbcResponses.Shared;

namespace Zyan.Examples.DynamicEbcResponses.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            // print information
            Console.WriteLine("Connecting to Zyan Server on localhost:4567 and creating Proxy...");

            // connect to the Zyan ComponentHost and create a new Proxy for the service
            using (var connection = new ZyanConnection("tcp://localhost:4567/DynamicEbcResponses"))
            using (var service = connection.CreateProxy<IService>())
            {
                // Example 1: convert a number to a spelledNumber
                Example1(service);

                // Example 2: divide two integers
                Example2(service);
            }

            Console.ReadLine();
        }



        /// <summary>
        /// Example 1: convert a number to a spelledNumber
        /// </summary>
        /// <param name="service">an instance of the IService to use</param>
        static void Example1(IService service)
        {
            // buffer variable for receiving the result within the lambda delegate
            String spelledNumber = "";

            // build the request with the source argument (4711) 
            // and the lambda expression which is back called by the service to store the result
            var request = new Request<Int32, String>(
                                                        4711,
                                                        s => spelledNumber = s
                                                    );
            // send the request to the service
            service.SpellNumber(request);

            // output the result
            Console.WriteLine();
            Console.WriteLine("Requested the conversion of the number {0}", request.RequestData);
            Console.WriteLine("Returned the spelledNumber: {0}", spelledNumber);
        }



        /// <summary>
        /// Example 2: divide two integers
        /// </summary>
        /// <param name="service">an instance of the IService to use</param>
        private static void Example2(IService service)
        {
            // buffer variable for receiving the result
            DivisionRequest.Results divisionResults = null;

            // build the specialized request with the dividend, the divisor 
            // and the lambda expression for the callback with the resulting quotient
            var divisionRequest = new DivisionRequest(
                                                          26, 10,
                                                          res => divisionResults = res
                                                     );
            // send the request to the service
            service.Divide(divisionRequest);

            // output the result
            Console.WriteLine();
            Console.WriteLine("Requested the division of {0} by {1}", divisionRequest.RequestData.Dividend, divisionRequest.RequestData.Divisor);
            Console.WriteLine("Returned the resulting quotient: {0}", divisionResults.Quotient);
            Console.WriteLine("               integer quotient: {0}", divisionResults.IntegerQuotient);
            Console.WriteLine("              integer remainder: {0}", divisionResults.IntegerRemainder);
        }

    }
}
