using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Hosting;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Composition
{
	/// <summary>
	/// Composable part definition linked to a remote component retrieved via ZyanConnection.
	/// Provides single export for component itself.
	/// </summary>
	internal class ZyanComposablePartDefinition : ComposablePartDefinition
	{
		public ZyanComposablePartDefinition(ZyanConnection connection, string componentInterfaceName, string uniqueName, bool transferTransactions)
		{
			Connection = connection;
			ComponentUniqueName = uniqueName;
			ComponentInterfaceName = GetTypeFullName(componentInterfaceName);
			ComponentInterface = new Lazy<Type>(() => TypeHelper.GetType(componentInterfaceName, true));
			ImplicitTransactionTransfer = transferTransactions;
		}

		private object lockObject = new object();

		public ZyanConnection Connection { get; private set; }

		public string ComponentUniqueName { get; private set; }

		public string ComponentInterfaceName { get; private set; }

		public Lazy<Type> ComponentInterface { get; private set; }

		public bool ImplicitTransactionTransfer { get; private set; }

		/// <summary>
		/// Converts Type.AssemblyQualifiedName format into Type.FullName.
		/// </summary>
		/// <remarks>
		/// Example: "MyNamespace.MyType, MyAssembly, Version=0.0.0.0..." -> "MyNamespace.MyType".
		/// </remarks>
		/// <param name="assemblyQualifiedName">Assembly-qualified type name.</param>
		/// <returns>Full name.</returns>
		private string GetTypeFullName(string assemblyQualifiedName)
		{
			var commaIndex = assemblyQualifiedName.IndexOf(',');
			if (commaIndex < 0)
				return assemblyQualifiedName;

			return assemblyQualifiedName.Substring(0, commaIndex);
		}

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
							metadata[ZyanComponentAttribute.ComponentInterfaceKeyName] = ComponentInterfaceName;
							metadata[ZyanComponentAttribute.IsPublishedKeyName] = true;
							metadata[CompositionConstants.ExportTypeIdentityMetadataName] = ComponentInterfaceName;

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
