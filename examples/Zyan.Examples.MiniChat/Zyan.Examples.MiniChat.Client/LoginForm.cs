using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Zyan.Communication;
using Zyan.Communication.Discovery.Metadata;

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

		private const string discoveryMessage = "Looking for available servers...";

		protected override void OnLoad(EventArgs args)
		{
			base.OnLoad(args);

			_comboServerUrl.Items.Insert(0, discoveryMessage);
			_comboServerUrl.SelectedIndex = 0;

			// start service discovery
			ZyanConnection.DiscoverHosts("MiniChat", ServerDiscovered);
		}

		private void ServerDiscovered(DiscoveryResponse response)
		{
			if (InvokeRequired)
			{
				BeginInvoke(new Action<DiscoveryResponse>(ServerDiscovered), response);
				return;
			}

			_comboServerUrl.Items.Remove(discoveryMessage);
			_comboServerUrl.SelectedIndex = _comboServerUrl.Items.Add(response.HostUrl);
		}
	}
}
