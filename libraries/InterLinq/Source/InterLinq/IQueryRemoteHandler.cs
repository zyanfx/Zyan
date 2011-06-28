using System;
using System.ServiceModel;
using InterLinq.Expressions;
using InterLinq.Communication.Wcf.NetDataContractSerializer;

namespace InterLinq
{
	/// <summary>
	/// Interface providing methods to communicate with
	/// the InterLINQ service.
	/// </summary>
	[ServiceContract]
	public interface IQueryRemoteHandler
	{
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
		[OperationContract]
		[NetDataContractFormat]
		[FaultContract(typeof(Exception))]
		object Retrieve(SerializableExpression expression);
	}
}
