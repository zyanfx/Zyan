using System;
using System.ServiceModel;
using System.Collections;
using System.Runtime.Remoting.Channels;
using InterLinq.UnitTests.Artefacts.EntityFramework4;
using InterLinq.EntityFramework4;
using Zyan.InterLinq.Communication.Wcf;
using Zyan.InterLinq.Communication.Remoting;

namespace InterLinq.UnitTests.Server
{
    public class TestServerEntityFramework4 : TestServer
    {

        #region Fields

        private CompanyEntities dataContext;

        #endregion

        #region Properties

        public override string DatabaseName
        {
            get { return "Company"; }
        }

        public override string CreateScriptName
        {
            get { throw new NotImplementedException(); }
        }

        public override string IntegrityScriptName
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        public override void Start()
        {
            dataContext = new CompanyEntities();
        }

        public override void Publish()
        {
            // Create the QueryHandler
            IQueryHandler queryHandler = new EntityFrameworkQueryHandler(dataContext);

            #region Start the WCF server

            ServerQueryWcfHandler wcfServer = new ServerQueryWcfHandler(queryHandler);

            NetTcpBinding netTcpBinding = ServiceHelper.GetNetTcpBinding();
            string serviceUri = ServiceHelper.GetServiceUri(null, null, Artefacts.ServiceConstants.EntityFramework4ServiceName);

            wcfServer.Start(netTcpBinding, serviceUri);

            #endregion

            #region Start the remoting server

            ServerQueryRemotingHandlerEntityFramework4 remotingServer = new ServerQueryRemotingHandlerEntityFramework4(queryHandler);
            // Register default channel for remote access
            Hashtable properties = new Hashtable();
            properties["name"] = Artefacts.ServiceConstants.EntityFramework4ServiceName;
            properties["port"] = Artefacts.ServiceConstants.EntityFramework4Port;
            IChannel currentChannel = RemotingConstants.GetDefaultChannel(properties);
            ChannelServices.RegisterChannel(currentChannel, false);
            remotingServer.Start(Artefacts.ServiceConstants.EntityFramework4ServiceName, false);

            #endregion
        }

    }
}
