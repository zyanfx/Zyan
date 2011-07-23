using System;
using System.Runtime.Remoting.Contexts;
using Zyan.Examples.Linq.Interfaces;

namespace Zyan.Examples.Linq.Server
{
	/// <summary>
	/// Sample buggy service
	/// </summary>
	class BuggyService : INamedService
	{
		static int counter = 0;

		/// <summary>
		/// Returns service name.
		/// </summary>
		public string Name
		{
			get
			{
				if (counter++ % 2 == 0)
				{
					throw new ApplicationException("This exception is simulated. Retry to succeed");
				}

				return "BuggyService";
			}
		}
	}
}
