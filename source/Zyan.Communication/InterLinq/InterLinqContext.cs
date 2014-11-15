using System;

namespace Zyan.InterLinq
{
	/// <summary>
	/// Abstract base class holding an <see cref="IQueryHandler"/>.
	/// The usage of the <see cref="InterLinqContext"/> is comparable
	/// with System.Data.Linq.DataContext.
	/// </summary>
	/// <example>
	/// The following code illustrates a possible implementation of <see cref="InterLinqContext"/>.
	/// <code>
	///	public class CompanyContext : InterLinqContext {
	///		public CompanyContext(IQueryHandler queryHandler) : base(queryHandler) { }
	///
	///		public IQueryable&lt;Company&gt; Companies {
	///			get { return QueryHander.Get&lt;Company&gt;(); }
	///		}
	///
	///		public IQueryable&lt;Company&gt; Departments {
	///			get { return QueryHander.Get&lt;Departments&gt;(); }
	///		}
	///
	///		public IQueryable&lt;Company&gt; Employees {
	///			get { return QueryHander.Get&lt;Employee&gt;(); }
	///		}
	///	}
	/// </code>
	/// </example>
	public abstract class InterLinqContext
	{
		#region Properties

		/// <summary>
		/// Gets the <see cref="IQueryHandler"/>.
		/// </summary>
		protected IQueryHandler QueryHander { get; private set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes this class.
		/// </summary>
		/// <param name="queryHandler"><see cref="IQueryHandler"/> instance.</param>
		protected InterLinqContext(IQueryHandler queryHandler)
		{
			if (queryHandler == null)
			{
				throw new ArgumentException("queryHandler");
			}
			QueryHander = queryHandler;
		}

		#endregion
	}
}
