using System.Collections;
using System.Runtime.Remoting.Channels;

namespace Zyan.Communication.ChannelSinks.ClientAddress
{
	internal class ClientAddressServerChannelSinkProvider: IServerChannelSinkProvider
	{
		private IServerChannelSinkProvider _nextProvider = null;

		public ClientAddressServerChannelSinkProvider()
		{
		}

		public ClientAddressServerChannelSinkProvider(IDictionary properties, ICollection providerData)
		{
		}

		public IServerChannelSinkProvider Next
		{
			get { return _nextProvider; }
			set { _nextProvider = value; }
		}

		public IServerChannelSink CreateSink(IChannelReceiver channel)
		{
			IServerChannelSink nextSink = null;
			if (_nextProvider != null)
			{
				nextSink = _nextProvider.CreateSink(channel);
			}
			return new ClientAddressServerChannelSink(nextSink);
		}

		public void GetChannelData(IChannelDataStore channelData)
		{
		}
	}
}