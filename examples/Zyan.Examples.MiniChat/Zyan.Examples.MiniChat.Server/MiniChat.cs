using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Examples.MiniChat.Shared;

namespace Zyan.Examples.MiniChat.Server
{
    public class MiniChat : IMiniChat
    {
        public event Action<string, string> MessageReceived;

        public void SendMessage(string nickname, string text)
        {
            if (MessageReceived != null)
            {
                try
                {
                    MessageReceived(nickname, text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
