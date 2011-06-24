using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Toolbox
{
	internal class MessageHelpers
	{
		public static string GetMethodSignature(Type type, string name, Type[] types)
		{
			var sb = new StringBuilder();

			// create argument list
			if (types != null && types.Length > 0)
			{
				Array.ForEach(types, t =>
				{
					sb.Append(sb.Length > 0 ? ", " : String.Empty);
					sb.Append(t.FullName);
				});
			}

			return String.Format("{0}.{1}({2})", type, name, sb);
		}
	}
}
