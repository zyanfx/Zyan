using System;

namespace Zyan.Examples.MiniChat.Shared
{
    public interface IMiniChat
    {
        event Action<string, string> MessageReceived;

        void SendMessage(string nickname, string text);
    }
}
