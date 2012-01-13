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

			_textNickname.DataBindings.Add("Text", this, "Nickname", false, DataSourceUpdateMode.OnPropertyChanged);
			_comboServerUrl.DataBindings.Add("Text", this, "ServerUrl", false, DataSourceUpdateMode.OnPropertyChanged);
		}

		public string Nickname
		{
			get;
			set;
		}

		public string ServerUrl
		{
			get;
			set;
		}

		protected override void OnLoad(EventArgs args)
		{
			base.OnLoad(args);

			// select the first address from the list automatically
			if (_comboServerUrl.Items.Count > 0)
			{
				_comboServerUrl.SelectedIndex = 0;
			}
		}
	}
}
