// using System;
// using System.ServiceModel;
// using System.ServiceModel.Channels;
//
// namespace Zyan.InterLinq.Communication.Wcf
// {
// 	/// <summary>
// 	/// Server class to retrieve information via WCF.
// 	/// </summary>
// 	/// <seealso cref="ServerQueryHandler"/>
// 	/// <seealso cref="IQueryRemoteHandler"/>
// 	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
// 	public class ServerQueryWcfHandler : ServerQueryHandler
// 	{
// 		#region Fields
//
// 		private ServiceHost host;
//
// 		#endregion
//
// 		#region Constructors
//
// 		/// <summary>
// 		/// Initializes this class.
// 		/// </summary>
// 		/// <param name="queryHandler"><see cref="IQueryHandler"/> instance.</param>
// 		public ServerQueryWcfHandler(IQueryHandler queryHandler)
// 			: base(queryHandler)
// 		{
// 		}
//
// 		#endregion
//
// 		#region Methods
//
// 		/// <summary>
// 		/// Starts a default Service Instance.
// 		/// </summary>
// 		public void Start()
// 		{
// 			Start(false);
// 		}
//
// 		/// <summary>
// 		/// Starts a Service Instance using the settings in your App.config.
// 		/// </summary>
// 		/// <param name="useAppConfig">Uses App.config WCF Service configuration elements if true.</param>
// 		public void Start(bool useAppConfig)
// 		{
// 			if (useAppConfig)
// 			{
// 				host = new ServiceHost(this);
// 				host.Open();
// 			}
// 			else
// 			{
// 				var binding = ServiceHelper.GetDefaultBinding();
// 				var serviceUri = ServiceHelper.GetServiceUri();
//
// 				Start(binding, serviceUri);
// 			}
// 		}
//
// 		/// <summary>
// 		/// Starts the Service Instance.
// 		/// </summary>
// 		/// <param name="binding">Predefined <see cref="Binding"/>.</param>
// 		/// <param name="serviceUri">Service URI as <see langword="string"/>.</param>
// 		public void Start(Binding binding, string serviceUri)
// 		{
// 			if (host != null)
// 			{
// 				Dispose();
// 			}
//
// 			// Open Service Host
// 			host = new ServiceHost(this);
// 			host.AddServiceEndpoint(typeof(IQueryRemoteHandler), binding, serviceUri);
// 			host.Open();
// 		}
//
// 		/// <summary>
// 		/// Stops the Service Instance.
// 		/// </summary>
// 		public void Stop()
// 		{
// 			Dispose();
// 		}
//
// 		/// <summary>
// 		/// Disposes the Service Instance.
// 		/// </summary>
// 		protected override void Dispose(bool disposing)
// 		{
// 			if (host != null && disposing)
// 			{
// 				host.Close();
// 				host = null;
// 			}
//
// 			base.Dispose(disposing);
// 		}
//
// 		/// <summary>
// 		/// Handles an <see cref="Exception"/> occured in the 
// 		/// <see cref="IQueryRemoteHandler.Retrieve"/> Method.
// 		/// </summary>
// 		/// <param name="exception">
// 		/// Thrown <see cref="Exception"/> 
// 		/// in <see cref="IQueryRemoteHandler.Retrieve"/> Method.
// 		/// </param>
// 		protected override void HandleExceptionInRetrieve(Exception exception)
// 		{
// 			Console.WriteLine(exception.Message);
// 			Exception innerstException = exception;
// 			while (innerstException.InnerException != null)
// 			{
// 				innerstException = innerstException.InnerException;
// 			}
// 			Console.WriteLine(innerstException.Message);
// 			throw new FaultException<Exception>(exception);
// 		}
//
// 		#endregion
// 	}
// }
