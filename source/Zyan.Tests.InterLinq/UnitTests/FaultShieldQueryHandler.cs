using System;
using System.ServiceModel;
using Zyan.InterLinq;
using Zyan.InterLinq.Expressions;

namespace InterLinq.UnitTests
{
	/// <summary>
	/// Delegate to etablish a connection to a <see cref="IQueryRemoteHandler">Remote Service</see>.
	/// </summary>
	/// <returns>Etablished <see cref="IQueryRemoteHandler">Service Reference</see></returns>
	public delegate IQueryRemoteHandler ConnectToServiceHandler();

	/// <summary>
	/// This <see cref="FaultShieldQueryHandler"/> is used to 
	/// communicate with the <see cref="IQueryRemoteHandler">Service instance</see>.
	/// Exceptions will be caught by this fault shield for better handling on the client.
	/// </summary>
	public class FaultShieldQueryHandler : IQueryRemoteHandler
	{
		#region Fields

		private readonly ConnectToServiceHandler connectMethod;

		#endregion

		#region Properties

		private IQueryRemoteHandler handler;
		/// <summary>
		/// Gets the <see cref="IQueryRemoteHandler">Handler</see>.
		/// </summary>
		public IQueryRemoteHandler Handler
		{
			get
			{
				if (handler == null)
				{
					Connect();
				}
				return handler;
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="connectMethod">Delegate to etablish a connection to a <see cref="IQueryRemoteHandler">Remote Service</see>.</param>
		public FaultShieldQueryHandler(ConnectToServiceHandler connectMethod)
		{
			this.connectMethod = connectMethod;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Etablish a connection to the <see cref="IQueryRemoteHandler">Remote Service</see>.
		/// </summary>
		public void Connect()
		{
			if (handler == null)
			{
				handler = connectMethod();
			}
		}

		#endregion

		#region IWcfLinqHandler Members

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
			try
			{
				return Handler.Retrieve(expression);
			}
			catch (FaultException ex)
			{
				Console.WriteLine("Test Failed on server.");
				handler = null;
				throw new Exception("The execution of the query failed on the server. See inner exception for more details.", ex);
			}
			catch (Exception)
			{
				Console.WriteLine("Test Failed.");
				handler = null;
				throw;
			}
		}

		#endregion
	}
}
