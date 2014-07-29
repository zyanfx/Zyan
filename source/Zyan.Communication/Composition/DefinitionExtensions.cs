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
			// 2) md[IsPublished] is true or is not defined
			// -----------------
			// md.ContainsKey(key) && md[key] is Type && (!md.ContainsKey(flag) ||
			//	(md.ContainsKey(flag) && md[flag] is bool && Convert.ToBoolean(md[flag])));

			// Optimized condition:
			object typeValue, flagValue;
			if (md.TryGetValue(key, out typeValue) && typeValue is Type)
			{
				if (md.TryGetValue(flag, out flagValue) && flagValue is bool)
				{
					return Convert.ToBoolean(flagValue);
				}

				// default flag value: IsPublished = true
				return true;
			}

			// component type metadata is required
			return false;
		}

		/// <summary>
		/// Returns true if part definition uses NonShared creation policy
		/// </summary>
		public static bool IsNonShared(this ComposablePartDefinition def)
		{
			var md = def.Metadata;
			var key = CompositionConstants.PartCreationPolicyMetadataName;

			// Condition: CreationPolicy == NonShared
			// return md.ContainsKey(key) && (CreationPolicy)md[key] == CreationPolicy.NonShared;

			// Optimized condition:
			object creationPolicy;
			if (md.TryGetValue(key, out creationPolicy) && (CreationPolicy)creationPolicy == CreationPolicy.NonShared)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true if part definition uses NonShared or Any creation policy
		/// </summary>
		public static bool IsNonSharedOrAny(this ComposablePartDefinition def)
		{
			var md = def.Metadata;
			var key = CompositionConstants.PartCreationPolicyMetadataName;

			// Condition: CreationPolicy is not specified or is either Any or NonShared
			// return !md.ContainsKey(key) || (CreationPolicy)md[key] == CreationPolicy.Any || def.IsNonShared();

			// Optimized condition:
			object creationPolicy;
			if (md.TryGetValue(key, out creationPolicy))
			{
				var policy = (CreationPolicy)creationPolicy;
				return policy == CreationPolicy.Any || policy == CreationPolicy.NonShared;
			}

			// by default, treat components as non-shared
			return true;
		}
	}
}
