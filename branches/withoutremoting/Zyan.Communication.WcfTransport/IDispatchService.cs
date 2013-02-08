using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Zyan.Communication.Transport;

namespace Zyan.Communication.WcfTransport
{
    [ServiceContract]
    public interface IDispatchService
    {
        [OperationContract]
        byte[] SendMessage(byte[] message);
    }
}
