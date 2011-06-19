using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Various extension methods
	/// TODO: Localize exceptions and trace messages
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Converts exception and all of its inner exceptions to string
		/// </summary>
		public static string Dump(this Exception ex, string title)
		{
			var sb = new StringBuilder(title);

			while (ex != null)
			{
				sb.AppendFormat("{0} ({1})", ex.Message, ex.GetType());
				sb.AppendLine();
				sb.AppendLine("Stack trace:");
				sb.AppendLine(ex.StackTrace);

				ex = ex.InnerException;
				if (ex != null)
				{
					sb.AppendLine();
					sb.Append("Inner exception: ");
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Returns true if method in one-way
		/// </summary>
		public static bool IsOneWay(this MethodInfo mi)
		{
			// check for OneWay attribute
			if (!RemotingServices.IsOneWay(mi))
			{
				return false;
			}

			// return type should be void
			if (mi.ReturnType != typeof(void))
			{
				Trace.WriteLine("Warning: non-void method is decorated with OneWay attribute: " + mi);
				return false;
			}

			// ref and out parameters are not allowed
			foreach (var arg in mi.GetParameters())
			{
				if (arg.IsOut || arg.ParameterType.IsByRef)
				{
					Trace.WriteLine("Warning: ret and out parameters are not allowed in OneWay method: " + mi);
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Invokes the method represented by current instance using the supplied parameters in either one-way or normal mode
		/// </summary>
		public static object Invoke(this MethodInfo mi, object instance, object[] args, bool oneWay)
		{
			if (oneWay)
			{
				ThreadPool.QueueUserWorkItem(t =>
				{
					try
					{
						// one-way method cannot throw any exceptions, so we ignore any
						mi.Invoke(instance, args);
					}
					catch (Exception ex)
					{
						Trace.WriteLine(ex.Dump("Unhandled exception in one-way method: "));
					}
				});

				return null;
			}

			return mi.Invoke(instance, args);
		}

		/// <summary>
		/// Creates invokation delegate for the method represented by given MethodInfo
		/// </summary>
		public static T CreateDelegate<T>(this MethodInfo method) where T : class
		{
			return Delegate.CreateDelegate(typeof(T), method) as T;
		}
	}
}
