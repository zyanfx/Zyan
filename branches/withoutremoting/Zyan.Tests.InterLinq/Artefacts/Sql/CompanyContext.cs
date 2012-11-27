using System;
using System.Linq;
using Zyan.InterLinq;

namespace InterLinq.UnitTests.Artefacts.Sql
{
	public class CompanyContext : InterLinqContext
	{
		public CompanyContext(IQueryHandler queryHandler) : base(queryHandler) { }

		public IQueryable<Company> Companies
		{
			get { return QueryHander.Get<Company>(); }
		}

		public IQueryable<Department> Departments
		{
			get { return QueryHander.Get<Department>(); }
		}

		public IQueryable<Employee> Employees
		{
			get { return QueryHander.Get<Employee>(); }
		}

	}

	[Serializable]
	public partial class Company { }

	[Serializable]
	public partial class Department { }

	[Serializable]
	public partial class Employee { }
}
