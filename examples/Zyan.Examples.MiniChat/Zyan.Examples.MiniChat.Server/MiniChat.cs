using System;
using Zyan.Examples.MiniChat.Shared;
using Zyan.Communication;

namespace Zyan.Examples.MiniChat.Server
{
	public class MiniChat : IMiniChat
	{
		public event Action<string, string> MessageReceived;

		public void SendMessage(string nickname, string text)
		{
			ServerSession session = ServerSession.CurrentSession;
			Console.WriteLine(string.Format("[{0} IP={1}] {2}:{3}", DateTime.Now.ToString(), session.ClientAddress, session.Identity.Name, text));

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
