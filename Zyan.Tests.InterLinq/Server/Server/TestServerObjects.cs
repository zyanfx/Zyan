using System;
using System.Collections;
using System.Runtime.Remoting.Channels;
using InterLinq.UnitTests.Artefacts.Objects;
using Zyan.Communication;
using Zyan.InterLinq;
using Zyan.InterLinq.Communication;
using Zyan.InterLinq.Communication.Remoting;
using Zyan.InterLinq.Communication.Wcf;

namespace InterLinq.UnitTests.Server
{
	public class TestServerObjects : TestServer
	{
		IObjectSource ObjectSource { get; set; }

		public override string DatabaseName
		{
			get { throw new NotImplementedException(); }
		}

		public override string CreateScriptName
		{
			get { throw new NotImplementedException(); }
		}

		public override string IntegrityScriptName
		{
			get { throw new NotImplementedException(); }
		}

		public override void Start()
		{
			ObjectSource = new ObjectSource();
		}

		public override void Publish()
		{
			// Create the QueryHandler
			IQueryHandler queryHandler = new ZyanObjectQueryHandler(ObjectSource);

			#region Start the WCF server
#if !MONO
			var wcfServer = new ServerQueryWcfHandler(queryHandler);
			var binding = ServiceHelper.GetDefaultBinding();

			string serviceUri = ServiceHelper.GetServiceUri(null, null, Artefacts.ServiceConstants.ObjectsServiceName);
			wcfServer.Start(binding, serviceUri);
#endif
			#endregion

			#region Start the Zyan server

			// change service name to avoid conflict with Remoting service
			var serviceName = Artefacts.ServiceConstants.ZyanServicePrefix + Artefacts.ServiceConstants.ObjectsServiceName;
			var protocol = ZyanConstants.GetDefaultServerProtocol(ZyanConstants.DefaultServicePort);
			var host = new ZyanComponentHost(serviceName, protocol);
			host.RegisterQueryHandler(queryHandler);

			#endregion

			#region Start the remoting server

			var remotingServer = new ServerQueryRemotingHandlerObjects(queryHandler);
			// Register default channel for remote access
			Hashtable properties = new Hashtable();
			properties["name"] = Artefacts.ServiceConstants.ObjectsServiceName;
			properties["port"] = Artefacts.ServiceConstants.ObjectsPort;
			IChannel currentChannel = RemotingConstants.GetDefaultChannel(properties);
			ChannelServices.RegisterChannel(currentChannel, false);
			remotingServer.Start(Artefacts.ServiceConstants.ObjectsServiceName, false);

			#endregion
		}
	}
}
