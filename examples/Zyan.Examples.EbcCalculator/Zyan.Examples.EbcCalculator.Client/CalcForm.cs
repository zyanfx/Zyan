using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Zyan.Examples.EbcCalculator
{
    public partial class CalcForm : Form
    {
        public CalcForm()
        {
            InitializeComponent();
        }

        private void _buttonCalc_Click(object sender, EventArgs e)
        {
            Out_AddNumbers(new AdditionRequest()
                           {
                               Number1=Decimal.Parse(_textNumber1.Text),
                               Number2=Decimal.Parse(_textNumber2.Text)
                           });
        }

        public Action<AdditionRequest> Out_AddNumbers { get; set; }

        public void In_ReceiveResult(Decimal result)
        {
            _textResult.Text = result.ToString();
        }
    }
}
