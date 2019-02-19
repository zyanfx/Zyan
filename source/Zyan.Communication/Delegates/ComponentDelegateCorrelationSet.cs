using System;

namespace Zyan.Communication.Delegates
{
	/// <summary>
	/// Delegate correlation set for a single component.
	/// </summary>
	[Serializable]
	public class ComponentDelegateCorrelationSet
	{
		/// <summary>
		/// Gets or sets the name of the interface.
		/// </summary>
		public string InterfaceName { get; set; }

		/// <summary>
		/// Gets or sets the unique component name (if not specified, defaults to InterfaceName).
		/// </summary>
		public string UniqueName { get; set; }

		/// <summary>
		/// Gets or sets the delegate correlation set.
		/// </summary>
		/// <value>
		/// The delegate correlation set.
		/// </value>
		public DelegateCorrelationInfo[] DelegateCorrelationSet { get; set; }
	}
}
