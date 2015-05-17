using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;

namespace Zyan.Communication.Toolbox
{
	internal static class RemotingHelper
	{
		private static object lockObject = new object();

		/// <summary>
		/// Resets the custom errors mode in a thread-safe way.
		/// </summary>
		public static void ResetCustomErrorsMode()
		{
			if (!MonoCheck.IsRunningOnMono)
			{
				if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
				{
					lock (lockObject)
					{
						if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
						{
							RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
						}
					}
				}
			}
		}
	}
}
