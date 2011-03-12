using Zyan.Communication;

namespace Zyan.InterLinq
{
	/// <summary>
	/// ZyanConnection extension methods for easier InterLINQ handlers access.
	/// </summary>
	public static class ZyanExtensions
	{
		/// <summary>
		/// Creates IQueryable proxy for Zyan connection
		/// </summary>
		/// <param name="connection">ZyanConnection</param>
		/// <param name="implicitTransactionTransfer">Transfer ambient transactions</param>
		public static ZyanClientQueryHandler CreateQueryableProxy(this ZyanConnection connection, bool implicitTransactionTransfer)
		{
			return new ZyanClientQueryHandler(connection) { ImplicitTransactionTransfer = implicitTransactionTransfer };
		}

		/// <summary>
		/// Creates IQueryable proxy for Zyan connection
		/// </summary>
		/// <param name="connection">ZyanConnection</param>
		public static ZyanClientQueryHandler CreateQueryableProxy(this ZyanConnection connection)
		{
			return new ZyanClientQueryHandler(connection);
		}
	}
}
