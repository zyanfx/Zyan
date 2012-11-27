using System;
using System.Collections.Generic;

namespace InterLinq.UnitTests.Artefacts.Objects
{
    [Serializable]
    public class Company
    {

        public Company()
        {
            Departments = new List<Department>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Foundation { get; set; }
        public Employee ChiefExecutiveOfficer { get; set; }
        public IList<Department> Departments { get; set; }

    }
}