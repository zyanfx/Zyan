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
		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanComposablePartDefinition" /> class.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="componentInterfaceName">Name of the component interface.</param>
		/// <param name="uniqueName">Unique name of the published component.</param>
		/// <param name="transferTransactions">if set to <c>true</c>, then ambient transaction transfer is enabled.</param>
		public ZyanComposablePartDefinition(ZyanConnection connection, string componentInterfaceName, string uniqueName, bool transferTransactions)
		{
			Connection = connection;
			ComponentUniqueName = uniqueName;
			ComponentInterfaceName = GetTypeFullName(componentInterfaceName);
			ComponentInterface = new Lazy<Type>(() => TypeHelper.GetType(componentInterfaceName, true));
			ImplicitTransactionTransfer = transferTransactions;
		}

		private object lockObject = new object();

		/// <summary>
		/// Gets the <see cref="ZyanConnection"/> used to create transparent proxies.
		/// </summary>
		public ZyanConnection Connection { get; private set; }

		/// <summary>
		/// Gets the unique name of the component (typically, the same as ComponentInterfaceName).
		/// </summary>
		public string ComponentUniqueName { get; private set; }

		/// <summary>
		/// Gets the name of the component interface.
		/// </summary>
		public string ComponentInterfaceName { get; private set; }

		/// <summary>
		/// Gets the component interface.
		/// </summary>
		public Lazy<Type> ComponentInterface { get; private set; }

		/// <summary>
		/// Gets a value indicating whether implicit transaction transfer is enabled.
		/// </summary>
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

		/// <summary>
		/// Creates a new instance of a part that the <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartDefinition" /> describes.
		/// </summary>
		/// <returns>
		/// The created part.
		/// </returns>
		public override ComposablePart CreatePart()
		{
			return new ZyanComposablePart(this);
		}

		private IEnumerable<ExportDefinition> exportDefinitions = null;

		/// <summary>
		/// Gets a collection of <see cref="T:System.ComponentModel.Composition.Primitives.ExportDefinition" /> objects that describe the objects
		/// exported by the part defined by this <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartDefinition" /> object.
		/// </summary>
		/// <returns>A collection of <see cref="T:System.ComponentModel.Composition.Primitives.ExportDefinition" /> objects
		/// that describe the exported objects provided by <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePart" /> objects
		/// created by the <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartDefinition" />.</returns>
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

		/// <summary>
		/// Gets a collection of <see cref="T:System.ComponentModel.Composition.Primitives.ImportDefinition" /> objects that describe the imports
		/// required by the part defined by this <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartDefinition" /> object.
		/// </summary>
		/// <returns>A collection of <see cref="T:System.ComponentModel.Composition.Primitives.ImportDefinition" /> objects 
		/// that describe the imports required by <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePart" /> objects 
		/// created by the <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartDefinition" />.</returns>
		public override IEnumerable<ImportDefinition> ImportDefinitions
		{
			get { return new ImportDefinition[0]; }
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			var result = ComponentInterfaceName;

			if (ComponentUniqueName != ComponentInterfaceName)
			{
				result += " (" + ComponentUniqueName + ")";
			}

			return result + " // zyan";
		}
	}
}
