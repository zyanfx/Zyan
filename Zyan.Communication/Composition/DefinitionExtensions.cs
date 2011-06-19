using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;

namespace Zyan.Communication.Composition
{
	/// <summary>
	/// Extension methods for ComposablePartDefinition, ExportDefinition, etc
	/// </summary>
	internal static class DefinitionExtensions
	{
		/// <summary>
		/// Returns true if export definition is ZyanComponent
		/// </summary>
		public static bool IsZyanComponent(this ExportDefinition def)
		{
			var md = def.Metadata;
			var key = ZyanComponentAttribute.ComponentInterfaceKeyName;
			var flag = ZyanComponentAttribute.IsPublishedKeyName;

			// Condition:
			// 1) md[ComponentInterface] is defined
			// 2) md[IsPublished] is true of not defined
			return md.ContainsKey(key) && md[key] is Type && (!md.ContainsKey(flag) ||
				(md.ContainsKey(flag) && md[flag] is bool && Convert.ToBoolean(md[flag])));
		}

		/// <summary>
		/// Returns true if part definition uses NonShared creation policy
		/// </summary>
		public static bool IsNonShared(this ComposablePartDefinition def)
		{
			var md = def.Metadata;
			var key = CompositionConstants.PartCreationPolicyMetadataName;

			// Condition: CreationPolicy == NonShared
			return md.ContainsKey(key) && ((CreationPolicy)md[key]) == CreationPolicy.NonShared;
		}
	}
}
