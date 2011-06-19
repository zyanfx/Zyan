using System;
using System.ComponentModel.Composition;

namespace Zyan.Communication.Composition
{
	/// <summary>
	/// Attribute used to decorate Zyan components
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class ZyanComponentAttribute : ExportAttribute
	{
		/// <summary>
		/// Initializes ZyanComponentAttribute instance.
		/// </summary>
		/// <param name="componentInterface"></param>
		public ZyanComponentAttribute(Type componentInterface)
			: base(componentInterface)
		{
			Initialize(componentInterface);
		}

		private void Initialize(Type componentInterface)
		{
			if (!componentInterface.IsInterface)
			{
				throw new InvalidOperationException("Interface type required: " + componentInterface);
			}

			ComponentInterface = componentInterface;
			IsPublished = true;
		}

		/// <summary>
		/// Gets interface attached to this ZyanComponent instance.
		/// </summary>
		public Type ComponentInterface { get; private set; }

		/// <summary>
		/// Gets or sets value indicating whether the component should be registered in ZyanComponentHost.
		/// </summary>
		public bool IsPublished { get; set; }

		/// <summary>
		/// MEF metadata key for component interface.
		/// </summary>
		public const string ComponentInterfaceKeyName = "ComponentInterface";

		/// <summary>
		/// MEF metadata key for IsPublished flag.
		/// </summary>
		public const string IsPublishedKeyName = "IsPublished";
	}
}
