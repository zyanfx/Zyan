using System;
using System.Runtime.Remoting;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// MarshalByRefObject with an infinite lifetime supporting deterministic disposal.
	/// </summary>
	/// <remarks>
	/// This code is based on the public domain snippet by Nathan Evans:
	/// https://nbevans.wordpress.com/2011/04/17/memory-leaks-with-an-infinite-lifetime-instance-of-marshalbyrefobject/
	/// </remarks>
	/// <seealso cref="System.MarshalByRefObject" />
	/// <seealso cref="System.IDisposable" />
	public class DisposableMarshalByRefObject : MarshalByRefObject, IDisposable
	{
		/// <summary>
		/// Ensures the unlimited lifetime.
		/// </summary>
		/// <returns>Always null.</returns>
		public sealed override object InitializeLifetimeService()
		{
			return null;
		}

		private bool Disposed { get; set; }

		/// <summary>
		/// Disconnects the remoting service.
		/// </summary>
		public void Dispose()
		{
			if (Disposed)
			{
				return;
			}

			RemotingServices.Disconnect(this);
			Disposed = true;
		}
	}
}
