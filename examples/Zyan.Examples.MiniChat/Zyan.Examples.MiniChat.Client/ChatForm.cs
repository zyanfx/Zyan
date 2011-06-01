using System;
using System.Threading;
using System.Windows.Forms;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Examples.MiniChat.Shared;

namespace Zyan.Examples.MiniChat.Client
{
    public partial class ChatForm : Form
    {
        private ZyanConnection _connection;
        private IMiniChat _chatProxy;

        public ChatForm()
        {
            InitializeComponent();

            TcpDuplexClientProtocolSetup protocol = new TcpDuplexClientProtocolSetup(true);
            ZyanConnection _connection = new ZyanConnection(Properties.Settings.Default.ServerUrl, protocol);

            _connection.CallInterceptors.For<IMiniChat>()
                .Add<string, string>(
                    (chat, nickname, text) => chat.SendMessage(nickname, text),
                    (data, nickname, text) =>
                    {
                        if (text.Contains("fuck") || text.Contains("sex"))
                        {
                            MessageBox.Show("TEXT CONTAINS FORBIDDEN WORDS!");
                            data.Intercepted = true;
                        }
                    });

            _connection.CallInterceptionEnabled = true;
            
            _chatProxy = _connection.CreateProxy<IMiniChat>();
            _chatProxy.MessageReceived += new Action<string, string>(_chatProxy_MessageReceived);
        }

        private void _chatProxy_MessageReceived(string arg1, string arg2)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string>(_chatProxy_MessageReceived), arg1, arg2);
                return;
            }
            if (arg1!=_nickName.Text)
                _chatList.Items.Insert(0, string.Format("{0}: {1}", arg1, arg2));
        }

        private void _sendButton_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(x => _chatProxy.SendMessage(_nickName.Text, _sayBox.Text));
        }

        private void ChatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _chatProxy.MessageReceived -= new Action<string, string>(_chatProxy_MessageReceived);
        }
    }
}
