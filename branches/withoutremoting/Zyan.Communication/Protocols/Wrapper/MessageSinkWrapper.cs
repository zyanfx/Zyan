using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace Zyan.Communication.Protocols.Wrapper
{
	/// <summary>
	/// Wraps around <see cref="IMessageSink"/> to normalize message urls.
	/// </summary>
	internal class MessageSinkWrapper : IMessageSink
	{
		public MessageSinkWrapper(IMessageSink innerSink)
		{
			if (innerSink == null)
			{
				throw new ArgumentNullException("innerSink");
			}

			InnerSink = innerSink;
		}

		private IMessageSink InnerSink { get; set; }

		private void NormalizeMessageUrl(IMessage msg)
		{
			// adjust message url
			var url = msg.Properties["__Uri"] as string;
			msg.Properties["__Uri"] = ChannelWrapper.NormalizeUrl(url);
		}

		public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
		{
			NormalizeMessageUrl(msg);
			return InnerSink.AsyncProcessMessage(msg, replySink);
		}

		public IMessageSink NextSink
		{
			get { return InnerSink.NextSink; }
		}

		public IMessage SyncProcessMessage(IMessage msg)
		{
			NormalizeMessageUrl(msg);
			return InnerSink.SyncProcessMessage(msg);
		}
	}
}
