using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zyan.Examples.WhisperChat.Shared;

namespace Zyan.Examples.WhisperChat.Server
{
    public class WhisperChatService : IWhisperChatService
    {
        public bool Register(string name, Action<string, string> callback)
        {
            return CallbackRegistry.Instance.Register(name, callback);
        }

        public bool Unregister(string name)
        {
            return CallbackRegistry.Instance.Unregister(name);
        }

        public void Whisper(string from, string to, string text)
        {
            Action<string, string> callback = CallbackRegistry.Instance.GetCallbackByName(to);

            if (callback != null)
            {
                try
                {
                    callback(from, text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{2}: Error whispering to client '{0}': {1}", to, ex.Message, DateTime.Now.ToString());
                }
            }
        }
    }
}
