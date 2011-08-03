using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;

namespace Zyan.Communication.Composition
{
	/// <summary>
	/// Composable part linked to a remote component retrieved via ZyanConnection.
	/// Creates transparent proxy for the remote component.
	/// </summary>
	internal class ZyanComposablePart : ComposablePart
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanComposablePart"/> class.
		/// </summary>
		/// <param name="definition">The composable part definition.</param>
		public ZyanComposablePart(ZyanComposablePartDefinition definition)
		{
			Definition = definition;
		}

		private object lockObject = new object();

		public ZyanComposablePartDefinition Definition { get; private set; }

		public ZyanConnection Connection { get { return Definition.Connection; } }

		public Type ComponentInterface { get { return Definition.ComponentInterface; } }

		public string ComponentUniqueName { get { return Definition.ComponentUniqueName; } }

		public bool ImplicitTransactionTransfer { get { return Definition.ImplicitTransactionTransfer; } }

		/// <summary>
		/// Gets a collection of the <see cref="T:System.ComponentModel.Composition.Primitives.ExportDefinition"/> objects 
		/// that describe the exported objects provided by the part.
		/// </summary>
		/// <returns>A collection of <see cref="T:System.ComponentModel.Composition.Primitives.ExportDefinition"/> objects that describe 
		/// the exported objects provided by the <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePart"/>.</returns>
		public override IEnumerable<ExportDefinition> ExportDefinitions
		{
			get { return Definition.ExportDefinitions; }
		}

		/// <summary>
		/// Method info for the ZyanConnection.CreateProxy{T}(string uniqueName, bool implicitTransactionTransfer) generic method.
		/// </summary>
		static MethodInfo CreateProxyGenericMethodInfo = typeof(ZyanConnection).GetMethod("CreateProxy", new[] { typeof(string), typeof(bool) });

		/// <summary>
		/// Method info for the ZyanConnection.CreateProxy{ComponentInterface}(string uniqueName, bool implicitTransactionTransfer) method.
		/// This method info is created on demand.
		/// </summary>
		private MethodInfo CreateProxyMethodInfo = null;

		/// <summary>
		/// Gets the exported object described by the specified <see cref="T:System.ComponentModel.Composition.Primitives.ExportDefinition"/> object.
		/// </summary>
		/// <param name="definition">One of the <see cref="T:System.ComponentModel.Composition.Primitives.ExportDefinition"/> objects from the <see cref="P:System.ComponentModel.Composition.Primitives.ComposablePart.ExportDefinitions"/> property that describes the exported object to return.</param>
		/// <returns>
		/// The exported object described by <paramref name="definition"/>.
		/// </returns>
		public override object GetExportedValue(ExportDefinition definition)
		{
			// TODO: validate export definition
			if (CreateProxyMethodInfo == null)
			{
				lock (lockObject)
				{
					if (CreateProxyMethodInfo == null)
					{
						CreateProxyMethodInfo = CreateProxyGenericMethodInfo.MakeGenericMethod(new[] { ComponentInterface });
					}
				}
			}

			// create transparent proxy
			return CreateProxyMethodInfo.Invoke(Connection, new object[] { ComponentUniqueName, ImplicitTransactionTransfer });
		}

		/// <summary>
		/// Gets a collection of the <see cref="T:System.ComponentModel.Composition.Primitives.ImportDefinition"/> 
		/// objects that describe the imported objects required by the part.
		/// </summary>
		/// <returns>A collection of <see cref="T:System.ComponentModel.Composition.Primitives.ImportDefinition"/> objects that describe
		/// the imported objects required by the <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePart"/>.</returns>
		public override IEnumerable<ImportDefinition> ImportDefinitions
		{
			get { return Definition.ImportDefinitions; }
		}

		/// <summary>
		/// This method is not supported because composable part is a proxy for the remote component.
		/// We can't put anything into it.
		/// </summary>
		/// <param name="definition">One of the objects from the <see cref="P:System.ComponentModel.Composition.Primitives.ComposablePart.ImportDefinitions"/> property that specifies the import to be set.</param>
		/// <param name="exports">A collection of <see cref="T:System.ComponentModel.Composition.Primitives.Export"/> objects of which to set the import described by <paramref name="definition"/>.</param>
		public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports)
		{
			throw new NotImplementedException();
		}
	}
}
