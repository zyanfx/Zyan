using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zyan.Communication.Transport;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Zyan.Communication.WcfTransport
{
    public class WcfServerTransportAdapter : IServerTransportAdapter, IDisposable
    {
        private ServiceHost _wcfHost = null;
        private object _wcfHostLock = new object();

        private IDispatchService _dispatchService = null;

        public WcfServerTransportAdapter()
        {
            _dispatchService = new DispatchService(this);
        }

        public string BaseAddress
        {
            get;
            set;
        }

        private ServiceHost WcfHost
        {
            get
            {
                if (_wcfHost == null)
                {
                    lock (_wcfHostLock)
                    {
                        if (_wcfHost == null)
                        {
                            _wcfHost = new ServiceHost(_dispatchService, new Uri(BaseAddress));
                            _wcfHost.AddServiceEndpoint(typeof(IDispatchService), new NetTcpBinding(SecurityMode.None), BaseAddress);
                            
                            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                            smb.HttpGetEnabled = false;
                            smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                            _wcfHost.Description.Behaviors.Add(smb);

                        }
                    }
                }
                return _wcfHost;
            }
        }

        public string UniqueName
        {
            get { return "WcfTransportServer"; }
        }

        public void StartListening()
        {
            WcfHost.Open(new TimeSpan(0,0,10));
        }

        public void StopListening()
        {
            lock (_wcfHostLock)
            {
                if (_wcfHost == null)
                    return;

                _wcfHost.Close();
                _wcfHost = null;
            }
        }

        public Func<IRequestMessage, IResponseMessage> ReceiveRequest
        {
            get;
            set;
        }

        private bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            StopListening();
            _dispatchService = null;
        }
    }
}
