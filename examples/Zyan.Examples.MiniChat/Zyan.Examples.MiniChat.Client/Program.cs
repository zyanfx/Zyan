using System;
using System.Windows.Forms;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication;
using Zyan.Examples.MiniChat.Shared;
using System.Collections;
using System.Security;

namespace Zyan.Examples.MiniChat.Client
{
    static class Program
    {
        private static ZyanConnection _connection;

        public static ZyanConnection ServerConnection
        { get { return _connection; } }

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string nickname = string.Empty;

            LoginForm loginForm = new LoginForm();

            while (string.IsNullOrEmpty(nickname))
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                    break;
                else
                    nickname = loginForm.Nickname;
            }
            if (string.IsNullOrEmpty(nickname))
                return;

            Hashtable credentials = new Hashtable();
            credentials.Add("nickname", nickname);

            TcpDuplexClientProtocolSetup protocol = new TcpDuplexClientProtocolSetup(true);

            try
            {
                using (_connection = new ZyanConnection(Properties.Settings.Default.ServerUrl, protocol, credentials, false, true))
                {
                    _connection.CallInterceptors.For<IMiniChat>()
                        .Add<string, string>(
                            (chat, nickname2, text) => chat.SendMessage(nickname2, text),
                            (data, nickname2, text) =>
                            {
                                if (text.Contains("fuck") || text.Contains("sex"))
                                {
                                    MessageBox.Show("TEXT CONTAINS FORBIDDEN WORDS!");
                                    data.Intercepted = true;
                                }
                            });

                    _connection.CallInterceptionEnabled = true;

                    Application.Run(new ChatForm(nickname));
                }
            }
            catch (SecurityException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
