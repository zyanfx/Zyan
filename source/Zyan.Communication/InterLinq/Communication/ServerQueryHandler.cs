using System;
using System.Linq;
using System.Reflection;
using Zyan.InterLinq.Types;
using Zyan.InterLinq.Expressions;

namespace Zyan.InterLinq.Communication
{
	/// <summary>
	/// Server implementation of the <see cref="IQueryRemoteHandler"/>.
	/// </summary>
	/// <seealso cref="IQueryRemoteHandler"/>
	public class ServerQueryHandler : IQueryRemoteHandler, IDisposable
	{
		#region Properties

		/// <summary>
		/// Gets the <see cref="IQueryHandler"/>.
		/// </summary>
		public IQueryHandler QueryHandler { get; protected set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="queryHandler"><see cref="IQueryHandler"/> instance.</param>
		public ServerQueryHandler(IQueryHandler queryHandler)
		{
			if (queryHandler == null)
			{
				throw new ArgumentNullException("queryHandler");
			}
			QueryHandler = queryHandler;
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
			try
			{
#if DEBUG
				Console.WriteLine(expression);
				Console.WriteLine();
#endif

				MethodInfo mInfo;
				Type realType = (Type)expression.Type.GetClrVersion();
				if (typeof(IQueryable).IsAssignableFrom(realType) &&
					realType.GetGenericArguments().Length == 1)
				{
					// Find Generic Retrieve Method
					mInfo = GetType().GetMethod("RetrieveGeneric");
					mInfo = mInfo.MakeGenericMethod(realType.GetGenericArguments()[0]);
				}
				else
				{
					// Find Non-Generic Retrieve Method
					mInfo = GetType().GetMethod("RetrieveNonGenericObject");
				}

				object returnValue = mInfo.Invoke(this, new object[] { expression });

#if DEBUG
				try
				{
					System.IO.MemoryStream ms = new System.IO.MemoryStream();
					new System.Runtime.Serialization.NetDataContractSerializer().Serialize(ms, returnValue);
				}
				catch (Exception)
				{
					throw;
				}
#endif

				return returnValue;
			}
			catch (Exception ex)
			{
				HandleExceptionInRetrieve(ex);
				throw;
			}
		}

		/// <summary>
		/// Retrieves data from the server by an <see cref="SerializableExpression">Expression</see> tree.
		/// </summary>
		/// <typeparam name="T">Type of the <see cref="IQueryable"/>.</typeparam>
		/// <param name="serializableExpression">
		///     <see cref="SerializableExpression">Expression</see> tree 
		///     containing selection and projection.
		/// </param>
		/// <returns>Returns requested data.</returns>
		/// <seealso cref="IQueryRemoteHandler.Retrieve"/>
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
		public object RetrieveGeneric<T>(SerializableExpression serializableExpression)
		{
			try
			{
				QueryHandler.StartSession();
				IQueryable<T> query = serializableExpression.Convert(QueryHandler) as IQueryable<T>;
				var returnValue = query.ToArray();
				object convertedReturnValue = TypeConverter.ConvertToSerializable(returnValue);
				return convertedReturnValue;
			}
			catch
			{
				throw;
			}
			finally
			{
				QueryHandler.CloseSession();
			}
		}

		/// <summary>
		/// Retrieves data from the server by an <see cref="SerializableExpression">Expression</see> tree.
		/// </summary>
		/// <param name="serializableExpression">
		///     <see cref="SerializableExpression">Expression</see> tree 
		///     containing selection and projection.
		/// </param>
		/// <returns>Returns requested data.</returns>
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
		/// <seealso cref="IQueryRemoteHandler.Retrieve"/>
		public object RetrieveNonGenericObject(SerializableExpression serializableExpression)
		{
			try
			{
				QueryHandler.StartSession();
				object returnValue = serializableExpression.Convert(QueryHandler);
				object convertedReturnValue = TypeConverter.ConvertToSerializable(returnValue);
				return convertedReturnValue;
			}
			catch
			{
				throw;
			}
			finally
			{
				QueryHandler.CloseSession();
			}
		}

		/// <summary>
		/// Handles an <see cref="Exception"/> occured in the 
		/// <see cref="IQueryRemoteHandler.Retrieve"/> Method.
		/// </summary>
		/// <param name="exception">
		/// Thrown <see cref="Exception"/> 
		/// in <see cref="IQueryRemoteHandler.Retrieve"/> Method.
		/// </param>
		protected virtual void HandleExceptionInRetrieve(Exception exception)
		{
			throw exception;
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Disposes the server instance.
		/// </summary>
		public virtual void Dispose() { }

		#endregion
	}
}
