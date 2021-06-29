// using System.ServiceModel;
// using System.ServiceModel.Channels;
//
// namespace Zyan.InterLinq.Communication.Wcf
// {
// 	/// <summary>
// 	/// Client handler class managing the connection 
// 	/// via WCF to the InterLINQ Server.
// 	/// </summary>
// 	/// <seealso cref="ClientQueryHandler"/>
// 	/// <seealso cref="InterLinqQueryHandler"/>
// 	/// <seealso cref="IQueryHandler"/>
// 	public class ClientQueryWcfHandler : ClientQueryHandler
// 	{
// 		#region Methods
//
// 		/// <summary>
// 		/// Connects to the server.
// 		/// <see cref="InterLinqQueryHandler"/>
// 		/// </summary>
// 		/// <seealso cref="ClientQueryHandler.Connect"/>
// 		public override void Connect()
// 		{
// 			Connect(null);
// 		}
//
// 		/// <summary>
// 		/// Connects to the Server using the settings in your App.config.
// 		/// </summary>
// 		/// <param name="endpointConfigurationName">The name of the client endpoint in App.config</param>
// 		public void Connect(string endpointConfigurationName)
// 		{
// 			if (queryRemoteHandler == null)
// 			{
// 				if (!string.IsNullOrEmpty(endpointConfigurationName))
// 				{
// 					ChannelFactory<IQueryRemoteHandler> channelFactory = new ChannelFactory<IQueryRemoteHandler>(endpointConfigurationName);
// 					queryRemoteHandler = channelFactory.CreateChannel();
// 				}
// 				else
// 				{
// 					var binding = ServiceHelper.GetDefaultBinding();
// 					var endpointAddress = ServiceHelper.GetEndpoint();
//
// 					Connect(binding, endpointAddress);
// 				}
// 			}
// 		}
//
// 		/// <summary>
// 		/// Connects to the server.
// 		/// </summary>
// 		/// <param name="binding">Predefined <see cref="Binding"/>.</param>
// 		/// <param name="endpointAddress"><see cref="EndpointAddress"/> of the server.</param>
// 		public void Connect(Binding binding, EndpointAddress endpointAddress)
// 		{
// 			if (queryRemoteHandler == null)
// 			{
// 				ChannelFactory<IQueryRemoteHandler> channelFactory = new ChannelFactory<IQueryRemoteHandler>(binding, endpointAddress);
// 				queryRemoteHandler = channelFactory.CreateChannel();
// 			}
// 		}
//
// 		#endregion
// 	}
// }
