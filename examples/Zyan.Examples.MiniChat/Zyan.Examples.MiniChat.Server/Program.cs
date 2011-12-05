using System;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Examples.MiniChat.Shared;
using System.Collections.Generic;

namespace Zyan.Examples.MiniChat.Server
{
    class Program
    {
        public static List<string> ActiveNicknames
        {
            get;
            set;
        }

        static void Main(string[] args)
        {
            ActiveNicknames = new List<string>();

            TcpDuplexServerProtocolSetup protocol = new TcpDuplexServerProtocolSetup(Properties.Settings.Default.TcpPort, new NicknameAuthProvider(), true);
            
            using (ZyanComponentHost host = new ZyanComponentHost("MiniChat",protocol))
            {
                host.PollingEventTracingEnabled = true;
                host.ClientHeartbeatReceived += new EventHandler<ClientHeartbeatEventArgs>(host_ClientHeartbeatReceived);

                host.RegisterComponent<IMiniChat, MiniChat>(ActivationType.Singleton);

                host.ClientLoggedOn += new EventHandler<LoginEventArgs>((sender, e) => 
                    {
                        Console.WriteLine(string.Format("{0}: User '{1}' with IP {2} logged on.", e.Timestamp.ToString(), e.Identity.Name, e.ClientAddress));
                        ActiveNicknames.Add(e.Identity.Name);
                    });
                host.ClientLoggedOff += new EventHandler<LoginEventArgs>((sender, e) =>
                    {
                        Console.WriteLine(string.Format("{0}: User '{1}' with IP {2} logged off.", e.Timestamp.ToString(), e.Identity.Name, e.ClientAddress));
                        ActiveNicknames.Remove(e.Identity.Name);
                    });

                Console.WriteLine("Chat server started. Press Enter to exit.");
                Console.ReadLine();
            }
        }

        static void host_ClientHeartbeatReceived(object sender, ClientHeartbeatEventArgs e)
        {
            Console.WriteLine(string.Format("{0}: Received heartbeat from session {1}.", e.HeartbeatReceiveTime.ToString(), e.SessionID.ToString()));
        }
    }
}
