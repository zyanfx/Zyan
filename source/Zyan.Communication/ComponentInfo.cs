using System;

namespace Zyan.Communication
{
	/// <summary>
	/// Describes a published component.
	/// </summary>
	[Serializable]
	public class ComponentInfo
	{
		/// <summary>
		/// Get or sets the interface name of the component.
		/// </summary>
		public string InterfaceName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the unique name of the component.
		/// </summary>
		public string UniqueName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the activation type of the component.
		/// </summary>
		public ActivationType ActivationType
		{
			get;
			set;
		}
	}
}
