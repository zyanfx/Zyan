using System;
using System.Windows.Forms;
using ServerInterfaces;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;

namespace FirstServerApplication
{
	public partial class ServerForm : Form
	{
		public FirstTestService ClientService = new FirstTestService();

		public ServerForm()
		{
			InitializeComponent();

			var proto = new TcpCustomServerProtocolSetup(18888, new NullAuthenticationProvider(), false);
			var host = new ZyanComponentHost("FirstTestServer", proto);
			host.RegisterComponent<IFirstTestService>(() => ClientService, ActivationType.Singleton);
		}

		private void TestButton_Click(object sender, EventArgs e)
		{
			ClientService.OnTest(new FirstTestEventArgs { Date = DateTime.Now } );
		}
	}
}
