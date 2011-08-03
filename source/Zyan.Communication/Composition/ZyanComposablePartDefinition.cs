using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Hosting;

namespace Zyan.Communication.Composition
{
	/// <summary>
	/// Composable part definition linked to a remote component retrieved via ZyanConnection.
	/// Provides single export for component itself.
	/// </summary>
	internal class ZyanComposablePartDefinition : ComposablePartDefinition
	{
		public ZyanComposablePartDefinition(ZyanConnection connection, Type componentInterface, string uniqueName, bool transferTransactions)
		{
			Connection = connection;
			ComponentInterface = componentInterface;
			ComponentUniqueName = uniqueName;
			ImplicitTransactionTransfer = transferTransactions;
		}

		private object lockObject = new object();

		public ZyanConnection Connection { get; private set; }

		public Type ComponentInterface { get; private set; }

		public string ComponentUniqueName { get; private set; }

		public bool ImplicitTransactionTransfer { get; private set; }

		public override ComposablePart CreatePart()
		{
			return new ZyanComposablePart(this);
		}

		private IEnumerable<ExportDefinition> exportDefinitions = null;

		public override IEnumerable<ExportDefinition> ExportDefinitions
		{
			get
			{
				// cache export definitions
				if (exportDefinitions == null)
				{
					lock (lockObject)
					{
						if (exportDefinitions == null)
						{
							var metadata = new Dictionary<string, object>();
							metadata[ZyanComponentAttribute.ComponentInterfaceKeyName] = ComponentInterface;
							metadata[ZyanComponentAttribute.IsPublishedKeyName] = true;
							metadata[CompositionConstants.ExportTypeIdentityMetadataName] = ComponentInterface.FullName;

							// create single export: component instance
							var export = new ExportDefinition(ComponentUniqueName, metadata);
							exportDefinitions = new[] { export };
						}
					}
				}

				return exportDefinitions;
			}
		}

		public override IEnumerable<ImportDefinition> ImportDefinitions
		{
			get { return new ImportDefinition[0]; }
		}
	}
}
