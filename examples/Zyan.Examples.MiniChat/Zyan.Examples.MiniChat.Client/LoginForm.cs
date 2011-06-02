using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Zyan.Examples.MiniChat.Client
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            _textNickname.DataBindings.Add("Text", this, "Nickname");
        }

        public string Nickname
        {
            get;
            set;
        }
    }
}
