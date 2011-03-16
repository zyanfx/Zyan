using System;

namespace Zyan.Examples.DynamicEbcResponses.Shared
{
    /// <summary>
    /// example service interface
    /// implements IDisposable to support using()
    /// </summary>
    public interface IService : IDisposable
    {
        /// <summary>
        /// converts an integer number to spelled digits
        /// using the default generic request argument
        /// </summary>
        /// <param name="request">request providing the number and a delegate to send back the response</param>
        void SpellNumber(Request<Int32, String> request);

        /// <summary>
        /// processes a division of two integer arguments
        /// using a specialized request argument
        /// </summary>
        /// <param name="divisionRequest">request providing dividend, divisor and a delegate to send back the response</param>
        void Divide(DivisionRequest divisionRequest);
    }
}