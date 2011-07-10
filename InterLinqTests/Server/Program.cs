using System;
using InterLinq.UnitTests.Server;

namespace InterLinq.UnitTests
{
	class Program
	{
		public static void Main()
		{
			Console.WriteLine("Starting server");

			//TestServerNHibernate testServerWcfNHibernate = new TestServerNHibernate();
			//testServerWcfNHibernate.Start();
			//testServerWcfNHibernate.Publish();

			//TestServerSql testServerWcfSql = new TestServerSql();
			//testServerWcfSql.Start();
			//testServerWcfSql.Publish();

			TestServerObjects testServerWcfObjects = new TestServerObjects();
			testServerWcfObjects.Start();
			testServerWcfObjects.Publish();

			//TestServerEntityFramework4 testServerEntityFramework4 = new TestServerEntityFramework4();
			//testServerEntityFramework4.Start();
			//testServerEntityFramework4.Publish();

			Console.WriteLine("Server is up and running...");
			Console.ReadLine();
		}

	}
}
