using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Helper class for operations that require deterministic release of resources.
	/// </summary>
	internal class Disposable : IDisposable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Disposable"/> class.
		/// </summary>
		/// <param name="cleanupAction">The cleanup action.</param>
		public Disposable(Action cleanupAction)
		{
			CleanupAction = cleanupAction;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Disposable"/> class.
		/// </summary>
		public Disposable()
			: this(null)
		{
		}

		private Action CleanupAction { get; set; }

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if (CleanupAction != null)
			{
				CleanupAction();
				CleanupAction = null;
			}
		}
	}
}
