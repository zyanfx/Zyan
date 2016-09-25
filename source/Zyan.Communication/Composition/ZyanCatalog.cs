using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Primitives;
using Zyan.Communication.Toolbox;

namespace Zyan.Communication.Composition
{
	/// <summary>
	/// An immutable ComposablePartCatalog created from a <see cref="ZyanConnection"/>.
	/// </summary>
	public class ZyanCatalog : ComposablePartCatalog
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanCatalog"/> class.
		/// </summary>
		/// <param name="connection">The <see cref="ZyanConnection"/> to pull remote components from.</param>
		public ZyanCatalog(ZyanConnection connection)
			: this(connection, true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanCatalog"/> class.
		/// </summary>
		/// <param name="connection">The <see cref="ZyanConnection"/> to pull remote components from.</param>
		/// <param name="transferTransactions">Enable ambient transactions support for created proxies.</param>
		public ZyanCatalog(ZyanConnection connection, bool transferTransactions)
			: this(connection, true, true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZyanCatalog"/> class.
		/// </summary>
		/// <param name="connection">The <see cref="ZyanConnection"/> to pull remote components from.</param>
		/// <param name="transferTransactions">Enable ambient transactions support for created proxies.</param>
		/// <param name="keepSynchronizationContext">Keep synchronization context for the callbacks and event handlers.</param>
		public ZyanCatalog(ZyanConnection connection, bool transferTransactions, bool keepSynchronizationContext)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");

			Connection = connection;
			ImplicitTransactionTransfer = transferTransactions;
			KeepSynchronizationContext = keepSynchronizationContext;
		}

		private object lockObject = new object();

		/// <summary>
		/// ZyanConnection to a remote component host.
		/// </summary>
		public ZyanConnection Connection { get; private set; }

		/// <summary>
		/// Gets a value indicating whether implicit transaction transfer is enabled for proxy objects created using this catalog instance.
		/// </summary>
		public bool ImplicitTransactionTransfer { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether original synchronization context should be used to execute callbacks and event handlers.
		/// </summary>
		public bool KeepSynchronizationContext { get; private set; }

		IList<ZyanComposablePartDefinition> innerParts = null;

		/// <summary>
		/// Gets the part definitions that are contained in the catalog.
		/// </summary>
		/// <returns>The <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartDefinition"/>
		/// contained in the <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartCatalog"/>.</returns>
		public override IQueryable<ComposablePartDefinition> Parts
		{
			get { return InnerParts.AsQueryable(); }
		}

		IList<ZyanComposablePartDefinition> InnerParts
		{
			get
			{
				// cache composable part definitions
				if (innerParts == null)
				{
					lock (lockObject)
					{
						if (innerParts == null)
						{
							innerParts = new List<ZyanComposablePartDefinition>();
							foreach (var component in Connection.RemoteDispatcher.GetRegisteredComponents())
							{
								var part = new ZyanComposablePartDefinition(Connection, component.InterfaceName, component.UniqueName, ImplicitTransactionTransfer, KeepSynchronizationContext);
								innerParts.Add(part);
							}
						}
					}
				}

				return innerParts;
			}
		}
	}
}
