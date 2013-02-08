using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Zyan.Communication.Transport;

namespace Zyan.Communication.WcfTransport
{
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    public class DispatchService : IDispatchService
    {
        public WcfServerTransportAdapter _transportAdapter = null;

        public DispatchService(WcfServerTransportAdapter transportAdapter)
        {
            if (transportAdapter == null)
                throw new ArgumentNullException("transportAdapter");

            _transportAdapter = transportAdapter;
        }

        public byte[] SendMessage(byte[] message)
        {
            var serializer = new BinaryFormatter();
            
            IRequestMessage request=null;

            using(var stream = new MemoryStream(message))
            {
                request = (IRequestMessage)serializer.Deserialize(stream);
            }
            var response = _transportAdapter.ReceiveRequest(request);
            
            byte[] rawResponse;

            using(var stream = new MemoryStream())
            {
                serializer.Serialize(stream, response);
                rawResponse = stream.ToArray();
            }
            return rawResponse;
        }
    }
}
