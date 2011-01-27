using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Examples.MiniChat.Shared
{
    public interface IMiniChat
    {
        event Action<string, string> MessageReceived;

        void SendMessage(string nickname, string text);
    }
}
