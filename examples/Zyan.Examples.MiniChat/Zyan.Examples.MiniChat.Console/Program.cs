using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Examples.MiniChat.Shared;

namespace Zyan.Examples.MiniChat.Console
{
    class Program
    {
        private static string _nickName = string.Empty;

        static void Main(string[] args)
        {
            System.Console.Write("Nickname:");
            _nickName = System.Console.ReadLine();

            System.Console.WriteLine("-----------------------------------------------");


            TcpCustomClientProtocolSetup protocol = new TcpCustomClientProtocolSetup(false);
            ZyanConnection connection = new ZyanConnection(Properties.Settings.Default.ServerUrl,protocol);

            connection.CallInterceptors.Add(new CallInterceptor(typeof(IMiniChat),
                                                                System.Reflection.MemberTypes.Method,
                                                                "SendMessage",
                                                                new Type[] { typeof(string), typeof(string) },
                                                                data =>
                                                                {
                                                                    string text2 = (string)data.Parameters[1];

                                                                    if (text2.Contains("fuck") || text2.Contains("sex"))
                                                                    {
                                                                        System.Console.WriteLine("TEXT CONTAINS FORBIDDEN WORDS!");
                                                                        data.Intercepted = true;
                                                                    }
                                                                }));
            connection.CallInterceptionEnabled = true;
            
            IMiniChat chatProxy = connection.CreateProxy<IMiniChat>();
            chatProxy.MessageReceived += new Action<string, string>(chatProxy_MessageReceived);

            string text = string.Empty;

            while (text.ToLower() != "quit")
            {
                text = System.Console.ReadLine();

                chatProxy.SendMessage(_nickName, text);
            }

            connection.Dispose();        
        }

        private static void chatProxy_MessageReceived(string arg1, string arg2)
        {
            if (arg1!=_nickName)
                System.Console.WriteLine(string.Format("{0}: {1}", arg1, arg2));
        }
    }

}
