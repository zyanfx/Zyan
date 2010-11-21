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
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        public string Message
        {
            get { return _messageLabel.Text; }
            set 
            {
                _messageLabel.ForeColor = Color.Red;
                _messageLabel.Text = value; 
            }
        }

        public string UserName
        {
            get { return _userBox.Text; }
            set { _userBox.Text = value; }
        }

        public string Password
        {
            get { return _passwordBox.Text; }
            set { _passwordBox.Text = value; }
        }
    }
}
