using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Zyan.Communication;
using Zyan.Communication.Security;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Examples.MiniChat.Shared;
using Zyan.Examples.MiniChat.Server;

namespace Zyan.Examples.MiniChat.AzureWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        public static List<string> ActiveNicknames
        {
            get;
            set;
        }

        public override void Run()
        {            
            Trace.WriteLine("Zyan.Examples.MiniChat.AzureWorkerRole entry point called", "Information");

            IPEndPoint endPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["MiniChat"].IPEndpoint;
            TcpDuplexServerProtocolSetup protocol = new TcpDuplexServerProtocolSetup(endPoint.Port, new NicknameAuthProvider(), true);
            
            ActiveNicknames = new List<string>();

            ZyanComponentHost host = new ZyanComponentHost("MiniChat", protocol);            
            host.RegisterComponent<IMiniChat, Zyan.Examples.MiniChat.Server.MiniChat>(ActivationType.Singleton);

            host.ClientLoggedOn += new EventHandler<LoginEventArgs>((sender, e) => 
                {
                    Trace.WriteLine(string.Format("{0}: User '{1}' with IP {2} logged on.", e.Timestamp.ToString(), e.Identity.Name, e.ClientAddress));
                    ActiveNicknames.Add(e.Identity.Name);
                });
            host.ClientLoggedOff += new EventHandler<LoginEventArgs>((sender, e) =>
                {
                    Trace.WriteLine(string.Format("{0}: User '{1}' with IP {2} logged off.", e.Timestamp.ToString(), e.Identity.Name, e.ClientAddress));
                    ActiveNicknames.Remove(e.Identity.Name);
                });

            while (true)
            {
                Thread.Sleep(10000);
                Trace.WriteLine("Working", "Information");
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }
    }
}
