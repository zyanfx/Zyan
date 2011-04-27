using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Contexts;

namespace Zyan.Examples.Linq.Server
{
	/// <summary>
	/// Sample buggy service
	/// </summary>
	class BuggyService : IDynamicProperty
	{
		static int counter = 0;

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
