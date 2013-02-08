using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zyan.Communication.Transport;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Zyan.Communication.WcfTransport
{
    public class WcfClientTransportAdapter : IClientTransportAdapter, IDisposable
    {
        private IDispatchService _proxy = null;
        private object _proxyLock=new object();

        public string BaseAddress
        {
            get;
            set;
        }

        private IDispatchService DispatchServiceProxy
        {
            get
            {
                if (string.IsNullOrEmpty(BaseAddress))
                    throw new InvalidOperationException();

                if (_proxy == null)
                {
                    lock (_proxyLock)
                    {
                        if (_proxy == null)
                            _proxy = ChannelFactory<IDispatchService>.CreateChannel(new NetTcpBinding(SecurityMode.None), new EndpointAddress(BaseAddress));
                    }
                }
                return _proxy;
            }
        }
        
        public string UniqueName
        {
            get { return "WcfTransportClient"; }
        }

        public bool Ready
        {
            get { return _proxy != null; }
        }

        public IResponseMessage SendRequest(IRequestMessage request)
        {
            var serializer = new BinaryFormatter();

            byte[] rawRequest;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, request);
                rawRequest = stream.ToArray();
            }

            var rawResponse = DispatchServiceProxy.SendMessage(rawRequest);
            IResponseMessage response = null;

            using (var stream = new MemoryStream(rawResponse))
            {
                response = (IResponseMessage)serializer.Deserialize(stream);
            }            
            return response;
        }

        public void SendRequestAsync(IRequestMessage request, Action<IResponseMessage> responseHandler)
        {
            throw new NotImplementedException();
        }

        private bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            lock (_proxyLock)
            {
                if (_proxy != null)
                {
                    var channel = ((IClientChannel)_proxy);

                    if (channel.State==CommunicationState.Opened)
                        channel.Close();
                    
                    ((IDisposable)_proxy).Dispose();
                    _proxy = null;
                }
            }
        }
    }
}
