using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Extension methods for the <see cref="Exception"/> class.
	/// </summary>
	public static class ExceptionHelper
	{
		private static Action<Exception> internalPreserveStackTrace;

		/// <summary>
		/// Initializes the <see cref="ExceptionHelper" /> class.
		/// </summary>
		static ExceptionHelper()
		{
			// InternalPreserveStackTrace hack discovered by Chris Taylor
			// http://web.archive.org/web/20080106084602/http://dotnetjunkies.com/WebLog/chris.taylor/archive/2004/03/03/8353.aspx
			// http://stackoverflow.com/questions/57383/in-c-how-can-i-rethrow-innerexception-without-losing-stack-trace
			var method = typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method != null)
			{
				internalPreserveStackTrace = method.CreateDelegate<Action<Exception>>();
				return;
			}

			// Alternative method is discovered by Anton Tykhyy
			// http://yama-mayaa.livejournal.com/14588.html
			// http://blogs.msdn.com/b/dotnet/archive/2009/08/25/the-good-and-the-bad-of-exception-filters.aspx
			// http://stackoverflow.com/questions/1009762/how-can-i-rethrow-an-inner-exception-while-maintaining-the-stack-trace-generated
			internalPreserveStackTrace = InternalPreserveStackTrace;
		}

		private static void InternalPreserveStackTrace(Exception e)
		{
			// check if method is applicable (exception type should have the deserialization constructor)
			var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var constructor = e.GetType().GetConstructor(bindingFlags, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
			if (constructor == null)
			{
				return;
			}

			var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
			var mgr = new ObjectManager(null, ctx);
			var si = new SerializationInfo(e.GetType(), new FormatterConverter());

			e.GetObjectData(si, ctx);
			mgr.RegisterObject(e, 1, si); // prepare for SetObjectData
			mgr.DoFixups(); // ObjectManager calls the deserialization constructor

			// voila, e is unmodified save for _remoteStackTraceString
		}

		/// <summary>
		/// Preserves the stack trace of the exception being rethrown.
		/// </summary>
		/// <param name="ex">The exception.</param>
		public static Exception PreserveStackTrace(this Exception ex)
		{
			if (ex != null && internalPreserveStackTrace != null)
			{
				internalPreserveStackTrace(ex);
			}

			return ex;
		}
	}
}
