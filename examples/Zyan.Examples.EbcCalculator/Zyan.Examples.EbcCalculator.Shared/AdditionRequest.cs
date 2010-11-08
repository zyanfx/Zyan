using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Examples.EbcCalculator
{
    [Serializable]
    public class AdditionRequest
    {
        public decimal Number1 { get; set; }
        public decimal Number2 { get; set; }
    }
}
