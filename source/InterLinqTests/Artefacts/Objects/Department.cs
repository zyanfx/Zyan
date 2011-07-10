using System;
using System.Collections.Generic;

namespace InterLinq.UnitTests.Artefacts.Objects
{
    [Serializable]
    public class Department
    {

        public Department()
        {
            Employees = new List<Employee>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Foundation { get; set; }
        public int QualityLevel { get; set; }
        public Company Company { get; set; }
        public Employee Manager { get; set; }
        public IList<Employee> Employees { get; set; }

    }
}