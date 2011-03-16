using System;

namespace Zyan.Examples.DynamicEbcResponses.Shared
{
    /// <summary>
    /// specialized request class
    /// explains how to encapsulate more then one service argument (dividend and divisor)
    /// explains how to return more then one value back to the client (Quotient, IntegerQuotient and IntegerRemainder)
    /// </summary>
    [Serializable]
    public class DivisionRequest
        : Request<DivisionRequest.Operands, DivisionRequest.Results>
    {

        /// <summary>
        /// Convinient constructor, allows to pass the individual arguments
        /// and builds the combined argument type Operands.
        /// </summary>
        /// <param name="dividend">number which should be divided</param>
        /// <param name="divisor">number representing the divisor</param>
        /// <param name="responseDelegate">callback delegate</param>
        public DivisionRequest(Int32 dividend, Int32 divisor, Action<Results> responseDelegate)
            : base(new Operands { Dividend = dividend, Divisor = divisor }, responseDelegate)
        {
        }

        /// <summary>
        /// argument values for the service
        /// </summary>
        [Serializable]
        public class Operands
        {
            public Int32 Dividend { get; set; }
            public Int32 Divisor { get; set; }
        }

        /// <summary>
        /// resulting values to pass back to the client
        /// </summary>
        [Serializable]
        public class Results
        {
            public Double Quotient { get; set; }
            public Int32 IntegerQuotient { get; set; }
            public Int32 IntegerRemainder { get; set; }
        }

    }
}