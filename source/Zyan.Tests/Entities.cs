using System.Collections.Generic;

namespace Zyan.Tests
{
	/// <summary>
	/// Sample entity class for Linq tests
	/// </summary>
	public class SampleEntity
	{
		public SampleEntity()
		{
			Id = -1;
			FirstName = "<Noname>";
			LastName = "<Unknown>";
		}

		public SampleEntity(int id, string name, string lastName)
		{
			Id = id;
			FirstName = name;
			LastName = lastName;
		}

		public int Id { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		static public IList<SampleEntity> GetSampleEntities()
		{
			var list = new List<SampleEntity>();
			list.Add(new SampleEntity(1, "Albert", "Einstein"));
			list.Add(new SampleEntity(2, "Niels", "Bohr"));
			list.Add(new SampleEntity(3, "Ralph", "Alpher"));
			list.Add(new SampleEntity(4, "Hans", "Bethe"));
			list.Add(new SampleEntity(5, "George", "Gamow"));
			list.Add(new SampleEntity(6, "Alexander", "Friedmann"));
			list.Add(new SampleEntity(7, "Enrico", "Fermi"));
			list.Add(new SampleEntity(8, "Richard", "Feynman"));
			list.Add(new SampleEntity(9, "Lev", "Landau"));
			list.Add(new SampleEntity(10, "Pyotr", "Kapitsa"));
			list.Add(new SampleEntity(11, "Robert", "Oppenheimer"));
			list.Add(new SampleEntity(12, "James", "Chadwick"));
			list.Add(new SampleEntity(13, "Arthur", "Compton"));
			list.Add(new SampleEntity(14, "Klaus", "Fuchs"));
			list.Add(new SampleEntity(15, "William", "Penney"));
			list.Add(new SampleEntity(16, "Emilio", "Segrè"));
			list.Add(new SampleEntity(17, "Ernest", "Lawrence"));
			list.Add(new SampleEntity(18, "Glenn", "Seaborg"));
			list.Add(new SampleEntity(19, "Leó", "Szilárd"));
			list.Add(new SampleEntity(20, "Edward", "Teller"));
			list.Add(new SampleEntity(21, "Stanislaw", "Ulam"));
			list.Add(new SampleEntity(22, "Harold", "Urey"));
			list.Add(new SampleEntity(23, "Leona", "Woods"));
			list.Add(new SampleEntity(24, "Chien-Shiung", "Wu"));
			list.Add(new SampleEntity(25, "Robert", "Wilson"));
			list.Add(new SampleEntity(26, "Igor", "Kurchatov"));
			return list;
		}
	}
}
