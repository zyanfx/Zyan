using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Zyan.Communication;
using Zyan.Communication.Toolbox;
using System.Threading.Tasks;

namespace Zyan.Examples.EbcCalculator
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (ZyanConnection connection = new ZyanConnection("tcp://localhost:8081/EbcCalc"))
            {
                CalcForm form = new CalcForm();
                ICalculator proxy = connection.CreateProxy<ICalculator>();

                form.Out_AddNumbers = Asynchronizer<AdditionRequest>.WireUp(new Action<AdditionRequest>(proxy.In_AddNumbers));
                proxy.Out_SendResult = SyncContextSwitcher<decimal>.WireUp(new Action<decimal>(form.In_ReceiveResult));
                
                Application.Run(form);
            }
        }
    }
}
