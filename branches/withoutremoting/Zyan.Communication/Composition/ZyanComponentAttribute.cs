using System;
using System.ComponentModel.Composition;

namespace Zyan.Communication.Composition
{
	/// <summary>
	/// Specifies that a type is a component that can be published by Zyan
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class ZyanComponentAttribute : ExportAttribute
	{
		/// <summary>
		/// Initializes ZyanComponentAttribute instance.
		/// </summary>
		/// <param name="componentInterface">Interface type attached to ZyanComponent</param>
		public ZyanComponentAttribute(Type componentInterface)
			: base(componentInterface)
		{
			Initialize(componentInterface);
		}

		/// <summary>
		/// Initializes ZyanComponentAttribute instance.
		/// </summary>
		/// <param name="uniqueName">Unique name of the ZyanComponent</param>
		/// <param name="componentInterface">Interface type attached to ZyanComponent</param>
		public ZyanComponentAttribute(string uniqueName, Type componentInterface)
			: base(uniqueName, componentInterface)
		{
			Initialize(componentInterface);
		}

		private void Initialize(Type componentInterface)
		{
			if (!componentInterface.IsInterface)
			{
				throw new ApplicationException(string.Format(LanguageResource.ApplicationException_SpecifiedTypeIsNotAnInterface, componentInterface.FullName));
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
