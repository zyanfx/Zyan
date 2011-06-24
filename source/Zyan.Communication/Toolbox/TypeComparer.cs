using System;
using System.Linq;
using System.Reflection;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Helper class for duck typing support.
	/// </summary>
	/// <typeparam name="I">Interface type</typeparam>
	/// <typeparam name="T">Component type</typeparam>
	internal class TypeComparer<I, T>
	{
		public void Validate()
		{
			if (!MatchAllMethods())
			{
				var msg = String.Format(LanguageResource.MissingMethodException_MethodNotFound, MissingMethodSignature);
				throw new MissingMethodException(msg);
			}
		}

		string MissingMethodSignature { get; set; }

		private bool MatchAllMethods()
		{
			var interfaceType = typeof(I);
			var componentType = typeof(T);

			// check if componentType actually implements interfaceType
			if (interfaceType.IsAssignableFrom(componentType))
			{
				return true;
			}

			// check if every interface method is implemented by the component type
			var interfaceMethods = interfaceType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
			var componentMethods = componentType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
			return interfaceMethods.All(im =>
			{
				var match = componentMethods.Any(cm => MethodsAreEqual(im, cm));
				if (!match)
				{ 
					var paramTypes = im.GetParameters().Select(pi => pi.ParameterType).ToArray();
					MissingMethodSignature = MessageHelpers.GetMethodSignature(componentType, im.Name, paramTypes);
				}

				return match;
			});
		}

		private bool MethodsAreEqual(MethodInfo im, MethodInfo cm)
		{
			// compare name and return type
			if (im.Name != cm.Name ||
				im.ReturnType != cm.ReturnType ||
				im.IsGenericMethod != cm.IsGenericMethod ||
				im.IsGenericMethodDefinition != cm.IsGenericMethodDefinition)
				return false;

			// compare parameter count
			var imParams = im.GetParameters();
			var cmParams = cm.GetParameters();
			if (imParams.Length != cmParams.Length)
				return false;

			// compare parameter types
			return imParams.Zip(cmParams, (ip, cp) => ip.ParameterType == cp.ParameterType).All(p => p);
		}
	}
}
