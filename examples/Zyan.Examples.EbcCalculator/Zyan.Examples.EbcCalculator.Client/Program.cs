using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Zyan.Communication;
using Zyan.Communication.Toolbox;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols.Http;

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

            ZyanConnection connection = LoginAndConnect();

            if (connection != null)
            {
                try
                {
                    CalcForm form = new CalcForm();
                    ICalculator proxy = connection.CreateProxy<ICalculator>();

                    form.Out_AddNumbers = Asynchronizer<AdditionRequest>.WireUp(proxy.In_AddNumbers);
                    proxy.Out_SendResult = SyncContextSwitcher<decimal>.WireUp(form.In_ReceiveResult);

                    Application.Run(form);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    connection.Dispose();
                }
            }
        }

        private static ZyanConnection LoginAndConnect()
        {
            HttpCustomClientProtocolSetup protocol = new HttpCustomClientProtocolSetup(true);

            ZyanConnection connection = null;
            bool success = false;
            string message = string.Empty;

            while (!success)
            {
                LoginForm loginForm = new LoginForm();

                if (!string.IsNullOrEmpty(message))
                    loginForm.Message = message;

                DialogResult result = loginForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    Hashtable credentials = new Hashtable();
                    credentials.Add(AuthRequestMessage.CREDENTIAL_USERNAME, loginForm.UserName);
                    credentials.Add(AuthRequestMessage.CREDENTIAL_PASSWORD, loginForm.Password);

                    try
                    {
                        connection = new ZyanConnection("http://localhost:8081/EbcCalc", protocol, credentials, false, true);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
                else
                    return null;
            }
            return connection;
        }
    }
}
