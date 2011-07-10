using System;

namespace InterLinq.UnitTests.Artefacts.NHibernate
{
    [Serializable]
    public class Employee
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsMale { get; set; }
        public double Salary { get; set; }
        public int Grade { get; set; }
        public Department Department { get; set; }

    }
}