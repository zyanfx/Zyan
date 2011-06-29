using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace Zyan.Communication.Composition
{
	/// <summary>
	/// Component catalog used to control NonShared components lifetime
	/// </summary>
	internal class NonSharedPartsCatalog : ComposablePartCatalog, INotifyComposablePartCatalogChanged
	{
		ComposablePartCatalog ParentCatalog { get; set; }

		INotifyComposablePartCatalogChanged ParentNotificationSource { get; set; }

		IQueryable<ComposablePartDefinition> PartsQuery { get; set; }

		public NonSharedPartsCatalog(ComposablePartCatalog parent)
		{
			ParentCatalog = parent;
			ParentNotificationSource = parent as INotifyComposablePartCatalogChanged;
			PartsQuery = ParentCatalog.Parts.Where(def => def.IsNonSharedOrAny());
		}

		public override IQueryable<ComposablePartDefinition> Parts
		{
			get { return PartsQuery; }
		}

		public event EventHandler<ComposablePartCatalogChangeEventArgs> Changed
		{
			add
			{
				if (ParentNotificationSource != null)
					ParentNotificationSource.Changed += value;
			}
			remove
			{
				if (ParentNotificationSource != null)
					ParentNotificationSource.Changed -= value;
			}
		}

		public event EventHandler<ComposablePartCatalogChangeEventArgs> Changing
		{
			add
			{
				if (ParentNotificationSource != null)
					ParentNotificationSource.Changing += value;
			}
			remove
			{
				if (ParentNotificationSource != null)
					ParentNotificationSource.Changing -= value;
			}
		}
	}
}
