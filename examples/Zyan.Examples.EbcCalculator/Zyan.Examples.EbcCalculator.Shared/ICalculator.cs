using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Examples.EbcCalculator
{
    public interface ICalculator
    {
        void In_AddNumbers(AdditionRequest message);

        Action<Decimal> Out_SendResult { get; set; }
    }
}
