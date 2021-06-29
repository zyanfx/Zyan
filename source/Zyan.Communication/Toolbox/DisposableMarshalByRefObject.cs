using System;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// MarshalByRefObject with an infinite lifetime supporting deterministic disposal.
	/// </summary>
	/// <remarks>
	/// This code is based on the public domain snippet by Nathan Evans:
	/// https://nbevans.wordpress.com/2011/04/17/memory-leaks-with-an-infinite-lifetime-instance-of-marshalbyrefobject/
	/// </remarks>
	/// <seealso cref="System.IDisposable" />
	public class DisposableMarshalByRefObject : IDisposable
	{
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

			//TODO: Migrate to CoreRemoting
			//RemotingServices.Disconnect(this);
			Disposed = true;
		}
	}
}
