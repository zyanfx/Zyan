/*
 THIS CODE IS BASED ON:
 -------------------------------------------------------------------------------------------------------------- 
 Solving the Assembly Load Context Problem
 http://ayende.com/blog/1376/solving-the-assembly-load-context-problem

 Copyright © 2006 Ayende @ Rahien. All Rights Reserved. 
 --------------------------------------------------------------------------------------------------------------
*/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Assembly locator to fix different load contexts problem.
	/// </summary>
	public static class AssemblyLocator
	{
		/// <summary>
		/// Gets or sets a value indicating whether the <see cref="AssemblyLocator"/> is enabled.
		/// </summary>
		/// <value>
		///   <c>true</c> if enabled; otherwise, <c>false</c>.
		/// </value>
		public static bool Enabled
		{
			get { return enabled; }
			set
			{
				if (enabled != value)
				{
					lock (lockObject)
					{
						if (enabled != value)
						{
							enabled = value;
							Initialize(enabled);
						}
					}
				}
			}
		}

		private static Dictionary<string, Assembly> assemblies;

		private static object lockObject = new object();

		private static bool enabled = false;

		/// <summary>
		/// Initializes assembly locator.
		/// </summary>
		/// <param name="enabled">True, if AssemblyLocator should be enabled, otherwise, false.</param>
		private static void Initialize(bool enabled)
		{
			if (enabled)
			{
				assemblies = assemblies ?? new Dictionary<string, Assembly>();
				AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
				return;
			}

			AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
			AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			Assembly assembly = null;
			if (assemblies.TryGetValue(args.Name, out assembly))
			{
				return assembly;
			}

			assemblies.TryGetValue(args.Name.ToLower(), out assembly);
			return assembly;
		}

		private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			var assembly = args.LoadedAssembly;
			assemblies[assembly.FullName] = assembly;
			assemblies[assembly.FullName.ToLower()] = assembly;
		}
	}
}
