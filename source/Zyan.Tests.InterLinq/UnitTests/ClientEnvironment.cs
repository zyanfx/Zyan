using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.ServiceModel;
using Zyan.InterLinq;
using Zyan.InterLinq.Communication.Remoting;
using Zyan.InterLinq.Communication.Wcf;
using Zyan.InterLinq.Communication;
using Zyan.Communication;

namespace InterLinq.UnitTests
{
	/// <summary>
	/// Client Environment definition class. The <see cref="ClientEnvironment"/>
	/// is used for setting up a client in an easy way.
	/// </summary>
	public abstract class ClientEnvironment
	{
		#region Fields

		/// <summary>
		/// Name of the running client environment instance.
		/// </summary>
		protected string serviceName;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the <see cref="IQueryHandler">QueryHandler</see>
		/// </summary>
		public IQueryHandler QueryHandler { get; protected set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="serviceName">Name of the service to instantiate.</param>
		protected ClientEnvironment(string serviceName)
		{
			if (serviceName == null)
			{
				throw new ArgumentNullException("serviceName");
			}
			this.serviceName = serviceName;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Starts the client environment instance.
		/// </summary>
		public abstract void Start();

		#endregion

		#region Client Instances

		private static readonly Dictionary<string, ClientEnvironment> instancesWcf = new Dictionary<string, ClientEnvironment>();
		/// <summary>
		/// Returns the WCF client environment instance for a <paramref name="serviceName">service name</paramref>.
		/// </summary>
		/// <param name="serviceName">Name of the service to search.</param>
		/// <returns>Returns the client environment instance for a <paramref name="serviceName">service name</paramref>.</returns>
		public static ClientEnvironment GetInstanceWcf(string serviceName)
		{
			if (!instancesWcf.ContainsKey(serviceName))
			{
				ClientEnvironment environment = new ClientEnvironmentWcf(serviceName);
				environment.Start();
				instancesWcf.Add(serviceName, environment);
			}
			return instancesWcf[serviceName];
		}

		private static readonly Dictionary<string, ClientEnvironment> instancesRemoting = new Dictionary<string, ClientEnvironment>();
		/// <summary>
		/// Returns the Remoting client environment instance for a <paramref name="serviceName">service name</paramref>.
		/// </summary>
		/// <param name="serviceName">Name of the service to search.</param>
		/// <returns>Returns the client environment instance for a <paramref name="serviceName">service name</paramref>.</returns>
		public static ClientEnvironment GetInstanceRemoting(string serviceName)
		{
			if (!instancesRemoting.ContainsKey(serviceName))
			{
				ClientEnvironment environment = new ClientEnvironmentRemoting(serviceName);
				environment.Start();
				instancesRemoting.Add(serviceName, environment);
			}
			return instancesRemoting[serviceName];
		}

		private static readonly Dictionary<string, ClientEnvironment> instancesZyan = new Dictionary<string, ClientEnvironment>();
		/// <summary>
		/// Returns the Zyan client environment instance for a <paramref name="serviceName">service name</paramref>.
		/// </summary>
		/// <param name="serviceName">Name of the service to search.</param>
		/// <returns>Returns the client environment instance for a <paramref name="serviceName">service name</paramref>.</returns>
		public static ClientEnvironment GetInstanceZyan(string serviceName)
		{
			if (!instancesZyan.ContainsKey(serviceName))
			{
				ClientEnvironment environment = new ClientEnvironmentZyan(serviceName);
				environment.Start();
				instancesZyan.Add(serviceName, environment);
			}

			return instancesZyan[serviceName];
		}

		#endregion
	}

	/// <summary>
	/// WCF client environment. The <see cref="ClientEnvironmentWcf"/>
	/// is used for setting up a WCF client in an easy way.
	/// </summary>
	/// <seealso cref="ClientEnvironment"/>
	public class ClientEnvironmentWcf : ClientEnvironment
	{
		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="serviceName">Name of the service to instantiate.</param>
		public ClientEnvironmentWcf(string serviceName) : base(serviceName) { }

		#endregion

		#region Methods

		/// <summary>
		/// Starts the client environment instance and 
		/// etablishes a connection to the WCF Service.
		/// </summary>
		/// <seealso cref="ClientEnvironment.Start"/>
		public override void Start()
		{
			QueryHandler = new ClientQueryHandler(new FaultShieldQueryHandler(Connect));
		}

		/// <summary>
		/// Etablishes a connection to the WCF Service.
		/// </summary>
		/// <returns>Returns the etablished connection.</returns>
		private IQueryRemoteHandler Connect()
		{
			ClientQueryWcfHandler clientHandler = new ClientQueryWcfHandler();

			NetTcpBinding netTcpBinding = ServiceHelper.GetNetTcpBinding();
			EndpointAddress endpoint = ServiceHelper.GetEndpoint(null, null, serviceName);

			clientHandler.Connect(netTcpBinding, endpoint);
			return clientHandler.QueryRemoteHandler;
		}

		#endregion
	}

	/// <summary>
	/// Remoting client environment. The <see cref="ClientEnvironmentRemoting"/>
	/// is used for setting up a Remoting client in an easy way.
	/// </summary>
	/// <seealso cref="ClientEnvironment"/>
	public class ClientEnvironmentRemoting : ClientEnvironment
	{
		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="serviceName">Name of the service to instantiate.</param>
		public ClientEnvironmentRemoting(string serviceName) : base(serviceName) { }

		#endregion

		#region Methods

		/// <summary>
		/// Starts the client environment instance and 
		/// etablishes a connection to the remoting server.
		/// </summary>
		/// <seealso cref="ClientEnvironment.Start"/>
		public override void Start()
		{
			var servicePort = Artefacts.ServiceConstants.GetServicePort(serviceName);
			var url = string.Format("{0}://{1}:{2}/{3}", RemotingConstants.DefaultServiceProtocol, RemotingConstants.DefaultServerName, servicePort, serviceName);

			var queryHandler = new ClientQueryRemotingHandler(url);
			var properties = new Hashtable();
			properties["name"] = serviceName;

			ChannelServices.RegisterChannel(RemotingConstants.GetDefaultChannel(properties), false);
			queryHandler.Connect();
			QueryHandler = queryHandler;
		}

		#endregion
	}

	/// <summary>
	/// Zyan client environment. The <see cref="ClientEnvironmentRemoting"/>
	/// is used for setting up a Zyan client in an easy way.
	/// </summary>
	/// <seealso cref="ClientEnvironment"/>
	public class ClientEnvironmentZyan : ClientEnvironment
	{
		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="serviceName">Name of the service to instantiate.</param>
		public ClientEnvironmentZyan(string serviceName) : base(serviceName) { }

		#endregion

		#region Methods

		/// <summary>
		/// Starts the client environment instance and 
		/// etablishes a connection to the Zyan service.
		/// </summary>
		/// <seealso cref="ClientEnvironment.Start"/>
		public override void Start()
		{
			var name = Artefacts.ServiceConstants.ZyanServicePrefix + serviceName;
			var url = String.Format("{0}://{1}:{2}/{3}", ZyanConstants.DefaultServiceProtocol, ZyanConstants.DefaultServerName, ZyanConstants.DefaultServicePort, name);

			var connection = new ZyanConnection(url, ZyanConstants.GetDefaultClientProtocol());
			var queryHandler = new ZyanClientQueryHandler(connection);
			queryHandler.Connect();

			QueryHandler = queryHandler;
		}

		#endregion
	}
}
