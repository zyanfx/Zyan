using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using InterLinq.Expressions;

namespace InterLinq.Communication.Remoting
{
	/// <summary>
	/// Server handler class to retrieve information via .NET Remoting.
	/// </summary>
	/// <seealso cref="IQueryRemoteHandler"/>
	/// <seealso cref="MarshalByRefObject"/>
	public class ServerQueryRemotingHandler : MarshalByRefObject, IQueryRemoteHandler
	{
		#region Fields

		private static IQueryRemoteHandler serviceInstance;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the <see cref="IQueryRemoteHandler"/>.
		/// </summary>
		public IQueryRemoteHandler InnerHandler { get; private set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		protected ServerQueryRemotingHandler()
		{
			InnerHandler = GetRegisteredService();
		}

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="innerHandler">Inner Handler of this Server.</param>
		public ServerQueryRemotingHandler(IQueryHandler innerHandler)
		{
			if (innerHandler == null)
			{
				throw new ArgumentNullException("innerHandler");
			}
			this.InnerHandler = new ServerQueryHandler(innerHandler);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Registers the <see cref="IQueryRemoteHandler"/> as service.
		/// </summary>
		/// <param name="serviceHandler"><see cref="IQueryRemoteHandler"/> to register.</param>
		protected virtual void RegisterService(IQueryRemoteHandler serviceHandler)
		{
			serviceInstance = serviceHandler;
		}

		/// <summary>
		/// Returns the registered <see cref="IQueryRemoteHandler"/>.
		/// </summary>
		/// <returns>Returns the registered <see cref="IQueryRemoteHandler"/>.</returns>
		protected virtual IQueryRemoteHandler GetRegisteredService()
		{
			if (serviceInstance == null)
			{
				throw new Exception("Service could not be found.");
			}
			return serviceInstance;
		}

		/// <summary>
		/// This methods is overwritten because this class is made for a singleton service and should
		/// live until the application terminates.
		/// </summary>
		/// <returns>This method will always return <see langword="null"/></returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}

		/// <summary>
		/// Publishes this object for remoting.
		/// </summary>
		/// <param name="createDefaultChannels">
		/// Determinates if the default channels should be created or not.
		/// <seealso cref="RemotingConstants"/>
		/// </param>
		public void Start(bool createDefaultChannels)
		{
			Start(RemotingConstants.DefaultServerObjectName, createDefaultChannels);
		}

		/// <summary>
		/// Publishes this object for remoting.
		/// </summary>
		/// <param name="objectUri">The remoting uri of the object.</param>
		/// <param name="createDefaultChannels">
		/// Determinates if the default channels should be created or not.
		/// <seealso cref="RemotingConstants"/>
		/// </param>
		public void Start(string objectUri, bool createDefaultChannels)
		{
			if (createDefaultChannels)
			{
				// Register default channel for remote access
				Hashtable properties = new Hashtable();
				properties["name"] = RemotingConstants.DefaultServiceChannelName;
				properties["port"] = RemotingConstants.DefaultServicePort;
				IChannel currentChannel = RemotingConstants.GetDefaultChannel(properties);
				ChannelServices.RegisterChannel(currentChannel, false);
			}

			WellKnownServiceTypeEntry serviceTypeEntry = new WellKnownServiceTypeEntry(
				GetType(),
				objectUri,
				WellKnownObjectMode.Singleton);
			RegisterService(InnerHandler);
			RemotingConfiguration.RegisterWellKnownServiceType(serviceTypeEntry);
		}

		#endregion

		#region IQueryRemoteHandler Members

		/// <summary>
		/// Retrieves data from the server by an <see cref="SerializableExpression">Expression</see> tree.
		/// </summary>
		/// <remarks>
		/// This method's return type depends on the submitted 
		/// <see cref="SerializableExpression">Expression</see> tree.
		/// Here some examples ('T' is the requested type):
		/// <list type="list">
		///     <listheader>
		///         <term>Method</term>
		///         <description>Return Type</description>
		///     </listheader>
		///     <item>
		///         <term>Select(...)</term>
		///         <description>T[]</description>
		///     </item>
		///     <item>
		///         <term>First(...), Last(...)</term>
		///         <description>T</description>
		///     </item>
		///     <item>
		///         <term>Count(...)</term>
		///         <description><see langword="int"/></description>
		///     </item>
		///     <item>
		///         <term>Contains(...)</term>
		///         <description><see langword="bool"/></description>
		///     </item>
		/// </list>
		/// </remarks>
		/// <param name="expression">
		///     <see cref="SerializableExpression">Expression</see> tree 
		///     containing selection and projection.
		/// </param>
		/// <returns>Returns requested data.</returns>
		/// <seealso cref="IQueryRemoteHandler.Retrieve"/>
		public object Retrieve(SerializableExpression expression)
		{
			return InnerHandler.Retrieve(expression);
		}

		#endregion
	}
}
