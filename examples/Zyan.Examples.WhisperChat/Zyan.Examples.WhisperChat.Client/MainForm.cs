using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Examples.WhisperChat.Shared;
using System.Threading.Tasks;

namespace Zyan.Examples.WhisperChat.Client
{
    public partial class MainForm : Form
    {
        private ZyanConnection _connection;
        private IWhisperChatService _proxy;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            var protocol = new TcpDuplexClientProtocolSetup(true);
            _connection = new ZyanConnection(Properties.Settings.Default.ServerUrl, protocol);
            _proxy = _connection.CreateProxy<IWhisperChatService>();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        private void _buttonRegister_Click(object sender, EventArgs e)
        {
            bool success = _proxy.Register(_textboxName.Text, ReceiveWhisper);

            _textboxName.Enabled = !success;
            _textboxTo.Enabled = success;
            _textboxText.Enabled = success;
            _buttonRegister.Enabled = !success;
            _buttonUnregister.Enabled = success;
            _buttonWhisper.Enabled = success;
        }

        private void _buttonUnregister_Click(object sender, EventArgs e)
        {
            bool success = _proxy.Unregister(_textboxName.Text);

            _textboxName.Enabled = success;
            _textboxTo.Enabled = !success;
            _textboxText.Enabled = !success;
            _buttonRegister.Enabled = success;
            _buttonUnregister.Enabled = !success;
            _buttonWhisper.Enabled = !success;
        }

        private void ReceiveWhisper(string from, string text)
        {
            if (InvokeRequired)
                Invoke(new Action<string,string>(ReceiveWhisper), from, text);
            else
                _textboxChat.Text = string.Format("{0} whispers to you: {1}\n\r\n\r{2}", from, text, _textboxChat.Text);
        }

        private void _buttonWhisper_Click(object sender, EventArgs e)
        {
            string from = _textboxName.Text;
            string to = _textboxTo.Text;
            string text = _textboxText.Text;

            new Task(() => _proxy.Whisper(from, to, text)).Start();
        }
    }
}
