using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Windows.Forms;
using ServerInterfaces;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;

namespace WindowsFormsApplication2
{
	public partial class ClientForm : Form
	{
		private IFirstTestService firstService;
		private ISecondTestService secondService;

		public ClientForm()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			var firstConnection = CreateFirstConnection();
//			var secondConnection = CreateSecondConnection();
			firstService = firstConnection.CreateProxy<IFirstTestService>(null);
//			secondService = secondConnection.CreateProxy<ISecondTestService>(null);
//			firstService.Test += FirstService_Test;
//			secondService.Test += SecondService_Test;
			firstConnection.CallInterceptors.Add(CreateCallInterceptor());
		}

		private CallInterceptor CreateCallInterceptor()
		{
			var callInterceptor = CallInterceptor.For<IFirstTestService>().Action(service => service.TestMethod(), data =>
			{
				data.Intercepted = true;
				data.MakeRemoteCall();
			});
			callInterceptor.Enabled = true;
			return callInterceptor;
		}

		private void FirstService_Test(object sender, FirstTestEventArgs args)
		{
			if (InvokeRequired)
			{
				Invoke(new EventHandler<FirstTestEventArgs>(FirstService_Test), new object[] { sender, args });
				return;
			}

			MessageBox.Show(this, "First", args.Date.ToString());
		}

		private void SecondService_Test(object sender, SecondTestEventArgs args)
		{
			if (InvokeRequired)
			{
				Invoke(new EventHandler<SecondTestEventArgs>(SecondService_Test), new object[] { sender, args });
				return;
			}

			MessageBox.Show(this, "Second", args.Date.ToString());
		}

		private ZyanConnection CreateFirstConnection()
		{
			var proto = new TcpCustomClientProtocolSetup(false);
			var connection = new ZyanConnection("tcp://127.0.0.1:18888/FirstTestServer", proto, null, true, true);
			connection.CallInterceptionEnabled = true;
			return connection;
		}

		private ZyanConnection CreateSecondConnection()
		{
			var proto = new TcpDuplexClientProtocolSetup(false);
			var connection = new ZyanConnection("tcpex://127.0.0.1:18889/SecondTestServer", proto);
			return connection;
		}

		private void FirstServiceTestMethodBtn_Click(object sender, EventArgs e)
		{
			firstService.TestMethod();
		}
	}
}
