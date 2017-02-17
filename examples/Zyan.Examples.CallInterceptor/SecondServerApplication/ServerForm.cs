using System;
using System.Windows.Forms;
using ServerInterfaces;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Communication.Security;

namespace SecondServerApplication
{
	public partial class ServerForm : Form
	{
		public SecondTestService ClientService = new SecondTestService();

		public ServerForm()
		{
			InitializeComponent();

			var proto = new TcpDuplexServerProtocolSetup(18889, new NullAuthenticationProvider(), false);
			var host = new ZyanComponentHost("SecondTestServer", proto);
			host.RegisterComponent<ISecondTestService>(() => ClientService, ActivationType.Singleton);
		}

		private void TestButton_Click(object sender, EventArgs e)
		{
			ClientService.OnTest(new SecondTestEventArgs { Date = DateTime.Now } );
		}
	}
}
