using System;
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
using Zyan.Examples.MiniChat.Shared;
using System.Collections;
using System.Security;

namespace Zyan.Examples.MiniChat.Console
{
    class Program
    {
        private static string _nickName = string.Empty;

        static void Main(string[] args)
        {
            System.Console.Write("Nickname: ");
            _nickName = System.Console.ReadLine();

            System.Console.WriteLine("-----------------------------------------------");

            Hashtable credentials = new Hashtable();
            credentials.Add("nickname", _nickName);

            ZyanConnection connection = null;
            TcpDuplexClientProtocolSetup protocol = new TcpDuplexClientProtocolSetup(true);
            
            try
            {
                connection = new ZyanConnection(Properties.Settings.Default.ServerUrl, protocol, credentials, false, true);
            }
            catch (SecurityException ex)
            {
                System.Console.WriteLine(ex.Message);
                System.Console.ReadLine();
                return;
            }
            connection.CallInterceptors.For<IMiniChat>().Add(
                (IMiniChat chat, string nickname, string message) => chat.SendMessage(nickname, message),
                (data, nickname, message) =>
                {
                    if (message.Contains("fuck") || message.Contains("sex"))
                    {
                        System.Console.WriteLine("TEXT CONTAINS FORBIDDEN WORDS!");
                        data.Intercepted = true;
                    }
                });

            connection.CallInterceptionEnabled = true;
            
            IMiniChat chatProxy = connection.CreateProxy<IMiniChat>();
            chatProxy.MessageReceived += new Action<string, string>(chatProxy_MessageReceived);

            string text = string.Empty;

            while (text.ToLower() != "quit")
            {
                text = System.Console.ReadLine();
                chatProxy.SendMessage(_nickName, text);
            }
            chatProxy.MessageReceived -= new Action<string, string>(chatProxy_MessageReceived);

            connection.Dispose();        
        }

        private static void chatProxy_MessageReceived(string arg1, string arg2)
        {
            if (arg1!=_nickName)
                System.Console.WriteLine(string.Format("{0}: {1}", arg1, arg2));
        }
    }

}
