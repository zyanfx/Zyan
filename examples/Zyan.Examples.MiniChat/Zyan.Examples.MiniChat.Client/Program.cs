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
		private static ZyanConnection _connection;

		private static Hashtable _credentials;

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

			_credentials = new Hashtable();
			_credentials.Add("nickname", nickname);

			TcpDuplexClientProtocolSetup protocol = new TcpDuplexClientProtocolSetup(true);

			try
			{
				using (_connection = new ZyanConnection(serverUrl, protocol, _credentials, false, true))
				{
					_connection.PollingInterval = new TimeSpan(0, 0, 30);
					_connection.PollingEnabled = true;
					_connection.Disconnected += new EventHandler<DisconnectedEventArgs>(_connection_Disconnected);
					_connection.NewLogonNeeded += new EventHandler<NewLogonNeededEventArgs>(_connection_NewLogonNeeded);
					_connection.Error += new EventHandler<ZyanErrorEventArgs>(_connection_Error);

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

		static void _connection_Error(object sender, ZyanErrorEventArgs e)
		{
			if (e.Exception is SocketException || e.Exception is InvalidSessionException || e.Exception is RemotingException)
			{
				int retryCount = 0;
				bool problemSolved = false;

				while (!problemSolved && retryCount < 10)
				{
					Thread.Sleep(5000);
					problemSolved = _connection.Reconnect();
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

		static void _connection_NewLogonNeeded(object sender, NewLogonNeededEventArgs e)
		{
			e.Credentials = _credentials;
		}

		static void _connection_Disconnected(object sender, DisconnectedEventArgs e)
		{
			e.Retry = e.RetryCount < 6;
		}
	}
}
