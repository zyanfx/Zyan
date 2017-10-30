using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Zyan.InterLinq.Types.Anonymous
{
	/// <summary>
	/// A class that holds a <see cref="AssemblyBuilder">dynamic assembly</see>.
	/// </summary>
	internal class DynamicAssemblyHolder
	{
		#region Singleton (double locked)

		private static volatile DynamicAssemblyHolder instance;

		private static object padlock = new object();

		/// <summary>
		/// Singleton instance of the <see cref="DynamicAssemblyHolder"/>.
		/// </summary>
		public static DynamicAssemblyHolder Instance
		{
			get
			{
				if (instance == null)
				{
					lock (padlock)
					{
						if (instance == null)
						{
							instance = new DynamicAssemblyHolder();
							instance.Initialize();
						}
					}
				}
				return instance;
			}
		}

		#endregion

		#region Fields

		private AssemblyBuilder assembly;

		#endregion

		#region Properties

		/// <summary>
		/// Gets a <see cref="ModuleBuilder"/> to create types in it.
		/// </summary>
		public ModuleBuilder ModuleBuilder { get; private set; }

		#endregion

		#region Constuctors / Initialization

		/// <summary>
		/// Private constructor to avoid external instantiation.
		/// </summary>
		private DynamicAssemblyHolder()
		{
		}

		/// <summary>
		/// Initializes the <see cref="DynamicAssemblyHolder"/>.
		/// </summary>
		private void Initialize()
		{
			// get the current appdomain
			AppDomain ad = AppDomain.CurrentDomain;

			// create a new dynamic assembly
			AssemblyName an = new AssemblyName
			{
				Name = "InterLinq.Types.Anonymous.Assembly",
				Version = new Version("1.0.0.0")
			};

			assembly = ad.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

			// create a new module to hold code in the assembly
			ModuleBuilder = assembly.GetDynamicModule("InterLinq.Types.Anonymous.Module") ??
				assembly.DefineDynamicModule("InterLinq.Types.Anonymous.Module");
		}

		#endregion
	}
}
