// using System;
// using System.Reflection;
// using Zyan.Communication.Toolbox;
//
// namespace Zyan.Communication
// {
// 	// Delegate for remote method invocation.
// 	using InvokeRemoteMethodDelegate = Func<IMethodCallMessage, MethodInfo, ReturnMessage>;
//
// 	/// <summary>
// 	/// Describes a single call interception action.
// 	/// </summary>
// 	public class CallInterceptionData
// 	{
// 		// Delegate for remote invocation
// 		InvokeRemoteMethodDelegate _remoteInvoker = null;
//
// 		// Remoting message
// 		IMethodCallMessage _remotingMessage = null;
//
// 		/// <summary>
// 		/// Creates a new instance of the CallInterceptionData class.
// 		/// </summary>
// 		/// <param name="invokerName">Inform interceptor about proxy unique name.</param>
// 		/// <param name="parameters">Parameter values of the intercepted call</param>
// 		/// <param name="remoteInvoker">Delegate for remote invocation</param>
// 		/// <param name="remotingMessage">Remoting message</param>
// 		public CallInterceptionData(string invokerName, object[] parameters, InvokeRemoteMethodDelegate remoteInvoker, IMethodCallMessage remotingMessage)
// 		{
// 			InvokerUniqueName = invokerName;
// 			Intercepted = false;
// 			ReturnValue = null;
// 			Parameters = parameters;
// 			_remoteInvoker = remoteInvoker;
// 			_remotingMessage = remotingMessage;
// 		}
//
// 		/// <summary>
// 		/// Makes a remote call.
// 		/// </summary>
// 		/// <returns>Return value of the remotly called method.</returns>
// 		public object MakeRemoteCall()
// 		{
// 			var returnMessage = _remoteInvoker(_remotingMessage, _remotingMessage.MethodBase as MethodInfo);
//
// 			if (returnMessage != null)
// 			{
// 				if (returnMessage.Exception != null)
// 					throw returnMessage.Exception.PreserveStackTrace();
//
// 				return returnMessage.ReturnValue;
// 			}
//
// 			return null;
// 		}
//
// 		/// <summary>
// 		/// Proxy caller name.
// 		/// </summary>
// 		public string InvokerUniqueName { get; }
//
// 		/// <summary>
// 		/// Gets or sets wether the call was intercepted.
// 		/// </summary>
// 		public bool Intercepted
// 		{
// 			get;
// 			set;
// 		}
//
// 		/// <summary>
// 		/// Gets or sets the return value to be used.
// 		/// </summary>
// 		public object ReturnValue
// 		{
// 			get;
// 			set;
// 		}
//
// 		/// <summary>
// 		/// Gets or sets the parameters which are passed to the call.
// 		/// </summary>
// 		public object[] Parameters
// 		{
// 			get;
// 			set;
// 		}
// 	}
// }