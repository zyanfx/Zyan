using System;
using System.ServiceModel;
using System.Text;
using InterLinq.Communication.Wcf;
using InterLinq.Sql;
using InterLinq.UnitTests.Artefacts.Sql;
using InterLinq.Communication.Remoting;
using System.Collections;
using System.Runtime.Remoting.Channels;

namespace InterLinq.UnitTests.Server
{
    public class TestServerSql : TestServer
    {

        #region Fields

        private CompanyDataContext dataContext;

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

            string connString = string.Format(@"AttachDBFileName='{0}{1}';Server='{2}';Integrated Security=SSPI;Connection Timeout=30",
                                                System.IO.Path.GetDirectoryName(GetType().Assembly.Location),
                                                @"\Sql\Company.mdf",
                                                @".");

            dataContext = new CompanyDataContext(connString);
        }

        public override void Publish()
        {
            // Create the QueryHandler
            IQueryHandler queryHandler = new SqlQueryHandler(dataContext);

            #region Start the WCF server

            ServerQueryWcfHandler wcfServer = new ServerQueryWcfHandler(queryHandler);

            NetTcpBinding netTcpBinding = ServiceHelper.GetNetTcpBinding();
            string serviceUri = ServiceHelper.GetServiceUri(null, null, Artefacts.ServiceConstants.SqlServiceName);

            wcfServer.Start(netTcpBinding, serviceUri);

            #endregion

            #region Start the remoting server

            ServerQueryRemotingHandlerSql remotingServer = new ServerQueryRemotingHandlerSql(queryHandler);
            // Register default channel for remote access
            Hashtable properties = new Hashtable();
            properties["name"] = Artefacts.ServiceConstants.SqlServiceName;
            properties["port"] = Artefacts.ServiceConstants.SqlPort;
            IChannel currentChannel = RemotingConstants.GetDefaultChannel(properties);
            ChannelServices.RegisterChannel(currentChannel, false);
            remotingServer.Start(Artefacts.ServiceConstants.SqlServiceName, false);

            #endregion
        }

    }
}
