using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;

namespace Zyan.Examples.EbcCalculator
{
    public class Calculator : ICalculator
    {
        public void In_AddNumbers(AdditionRequest message)
        {
            Out_SendResult(message.Number1 + message.Number2);
        }

        public Action<decimal> Out_SendResult { get; set; }
    }
}
