using System;
using System.Text;
using System.Collections.Generic;
using Zyan.Examples.DynamicEbcResponses.Shared;

namespace Zyan.Examples.DynamicEbcResponses.Server
{
    /// <summary>
    /// example service
    /// implements IDisposable to support using()
    /// </summary>
    class Service : IService
    {
        /// <summary>
        /// converts an integer number to spelled digits
        /// using the default generic request argument
        /// </summary>
        /// <param name="request">request providing the number and a delegate to send back the response</param>
        public void SpellNumber(Request<Int32, String> request)
        {

            // build helper dictionary to translate digit to spelledDigit
            var numbers = new Dictionary<Char, String>
                                  {
                                      {'1', "one"},
                                      {'2', "two"},
                                      {'3', "three"},
                                      {'4', "four"},
                                      {'5', "five"},
                                      {'6', "six"},
                                      {'7', "seven"},
                                      {'8', "eight"},
                                      {'9', "nine"},
                                      {'0', "zero"}
                                  };

            // prepate input data
            var digits = request.RequestData.ToString().ToCharArray();
            var spelledDigits = new StringBuilder();

            // translate each digit to the corresponding spelledDigit
            foreach (var digit in digits)
            {
                String spelledDigit;
                if (numbers.TryGetValue(digit, out spelledDigit))
                    spelledDigits.Append(spelledDigit).Append(' ');
            }

            // send response data via delegate provided with this request
            var responseData = spelledDigits.ToString().Trim();
            request.Response(responseData);
        }



        /// <summary>
        /// processes a division of two integer arguments
        /// using a specialized request argument
        /// </summary>
        /// <param name="divisionRequest">request providing dividend, divisor and a delegate to send back the response</param>
        public void Divide(DivisionRequest divisionRequest)
        {
            var operands = divisionRequest.RequestData;
            var results = new DivisionRequest.Results
                              {
                                  Quotient = (double)operands.Dividend / (double)operands.Divisor,
                                  IntegerQuotient = operands.Dividend / operands.Divisor,
                                  IntegerRemainder = operands.Dividend % operands.Divisor
                              };
            divisionRequest.Response(results);
        }


        /// <summary>
        /// empty implementation, primarily to support using() statement
        /// </summary>
        public void Dispose()
        {
        }
    }
}