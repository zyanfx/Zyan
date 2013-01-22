using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Examples.WhisperChat.Shared
{
    public interface IWhisperChatService
    {
        bool Register(string name, Action<string, string> callback);

        bool Unregister(string name);

        void Whisper(string from, string to, string text);
    }
}
