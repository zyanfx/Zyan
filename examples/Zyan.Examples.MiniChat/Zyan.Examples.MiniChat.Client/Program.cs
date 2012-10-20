using System;
using System.Windows.Forms;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication;
using Zyan.Examples.MiniChat.Shared;
using System.Collections;
using System.Security;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.Remoting;

namespace Zyan.Examples.MiniChat.Client
{
	static class Program
	{
		private static Hashtable Credentials { get; set; }

		public static ZyanConnection Connection { get; private set; }

		/// <summary>
		/// Der Haupteinstiegspunkt für die Anwendung.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			string nickname = string.Empty;
			string serverUrl = string.Empty;

			LoginForm loginForm = new LoginForm();

			while (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(serverUrl))
			{
				if (loginForm.ShowDialog() != DialogResult.OK)
					break;

				nickname = loginForm.Nickname;
				serverUrl = loginForm.ServerUrl;
			}

			if (string.IsNullOrEmpty(nickname))
				return;

			Credentials = new Hashtable();
			Credentials.Add("nickname", nickname);

			TcpDuplexClientProtocolSetup protocol = new TcpDuplexClientProtocolSetup(true);

			try
			{
				using (Connection = new ZyanConnection(serverUrl, protocol, Credentials, false, true))
				{
					Connection.PollingInterval = new TimeSpan(0, 0, 30);
					Connection.PollingEnabled = true;
					Connection.Disconnected += new EventHandler<DisconnectedEventArgs>(Connection_Disconnected);
					Connection.NewLogonNeeded += new EventHandler<NewLogonNeededEventArgs>(Connection_NewLogonNeeded);
					Connection.Error += new EventHandler<ZyanErrorEventArgs>(Connection_Error);

					Connection.CallInterceptors.For<IMiniChat>()
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

					Connection.CallInterceptionEnabled = true;

					Application.Run(new ChatForm(nickname));
				}
			}
			catch (SecurityException ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		static void Connection_Error(object sender, ZyanErrorEventArgs e)
		{
			if (e.Exception is SocketException || e.Exception is InvalidSessionException || e.Exception is RemotingException)
			{
				int retryCount = 0;
				bool problemSolved = false;

				while (!problemSolved && retryCount < 10)
				{
					Thread.Sleep(5000);
					problemSolved = Connection.Reconnect();
					retryCount++;
				}
				if (problemSolved)
					e.Action = ZyanErrorAction.Retry;
				else
					e.Action = ZyanErrorAction.ThrowException;
			}
			else
				e.Action = ZyanErrorAction.ThrowException;
		}

		static void Connection_NewLogonNeeded(object sender, NewLogonNeededEventArgs e)
		{
			e.Credentials = Credentials;
		}

		static void Connection_Disconnected(object sender, DisconnectedEventArgs e)
		{
			e.Retry = e.RetryCount < 6;
		}
	}
}
