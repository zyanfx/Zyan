using System;
using System.Collections.Generic;
using System.Reflection;
using System.Transactions;
using Zyan.Communication.Delegates;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication
{
	/// <summary>
	/// Contains all necessary details to invoke a specified method.
	/// </summary>
	[Serializable]
	internal class InvocationDetails
	{
		/// <summary>
		/// Gets or sets the unique key for call tracking.
		/// </summary>
		public Guid TrackingID { get; set; }

		/// <summary>
		/// Gets or sets the name of the component (namespace included) interface.
		/// </summary>
		public string InterfaceName { get; set; }

		/// <summary>
		/// Gets or sets correlation set for dynamic event and delegate wiring.
		/// </summary>
		public List<DelegateCorrelationInfo> DelegateCorrelationSet { get; set; }

		/// <summary>
		/// Gets or sets the name of the invoked method.
		/// </summary>
		public string MethodName { get; set; }

		/// <summary>
		/// Gets or sets Generic arguments of the invoked method.
		/// </summary>
		public Type[] GenericArguments { get; set; }

		/// <summary>
		/// Gets or sets parameter types of the invoked method.
		/// </summary>
		public Type[] ParamTypes { get; set; }

		/// <summary>
		/// Gets or sets parameter values of the invoked method.
		/// </summary>
		public object[] Args { get; set; }

		/// <summary>
		/// Gets or sets the return values of the invoked method.
		/// </summary>
		public object ReturnValue { get; set; }

		/// <summary>
		/// Gets or sets the data from call context.
		/// </summary>
		public LogicalCallContextData CallContextData { get; set; }

		/// <summary>
		/// Gets or sets the DelegateInterceptor to parameter index mapping.
		/// </summary>
		public Dictionary<int, DelegateInterceptor> DelegateParamIndexes { get; set; }

		/// <summary>
		/// Gets or sets the component registration.
		/// </summary>
		public ComponentRegistration Registration { get; set; }

		/// <summary>
		/// Gets or sets the component instance.
		/// </summary>
		public object Instance { get; set; }

		/// <summary>
		/// Gets or sets the component type.
		/// </summary>
		public Type Type { get; set; }

		/// <summary>
		/// Gets or sets the interface type.
		/// </summary>
		public Type InterfaceType { get; set; }

		/// <summary>
		/// Gets or stes the session.
		/// </summary>
		public ServerSession Session { get; set; }

		/// <summary>
		/// Gets or sets whether a exception was thrown, or not.
		/// </summary>
		public bool ExceptionThrown { get; set; }

		/// <summary>
		/// Gets or sets wiring correlation for events.
		/// </summary>
		public Dictionary<Guid, Delegate> WiringList { get; set; }

		/// <summary>
		/// Gets or sets the transaction scope.
		/// </summary>
		public TransactionScope Scope { get; set; }

		/// <summary>
		/// Gets or sets method metadata.
		/// </summary>
		public MethodInfo MethodInfo { get; set; }

		/// <summary>
		/// Finds the method information.
		/// </summary>
		public bool FindMethodInfo()
		{
			// find public method of the component
			MethodInfo = Type.GetMethod(MethodName, GenericArguments, ParamTypes);
			if (MethodInfo == null)
			{
				// find method of the interface
				var method = InterfaceType.GetMethod(MethodName, GenericArguments, ParamTypes);
				if (method != null && Type.GetInterface(InterfaceType.Name) != null)
				{
					// get the corresponding method of the component
					var map = Type.GetInterfaceMap(InterfaceType);
					var index = Array.IndexOf(map.InterfaceMethods, method);
					if (index >= 0)
					{
						MethodInfo = map.TargetMethods[index];
					}
				}
			}

			return MethodInfo != null;
		}
	}
}
