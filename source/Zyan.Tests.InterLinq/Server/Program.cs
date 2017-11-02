using System;
using InterLinq.UnitTests.Server;
using System.Threading;
using Topshelf;

namespace InterLinq.UnitTests
{
	class Program
	{
		public static void Main()
		{
			HostFactory.Run(x =>
			{
				Console.WriteLine("Setting up the InterLinq server");
				x.Service<Service>(s =>
				{
					s.ConstructUsing(() => new Service());
					s.WhenStarted(ts => ts.Start());
					s.WhenStopped(ts => ts.Stop());
				});

				// these options can be overridden via the command line:
				// Server install --localservice -servicename:InterLinq -description:Demo -displayname:InterLinqDemo
				x.RunAsNetworkService();
				x.SetDescription("InterLinq unit testing service");
				x.SetDisplayName("InterLinqService");
				x.SetServiceName("InterLinqService");
				x.EnableServiceRecovery(rc => rc.RestartService(1)); // restart after one minute
				x.StartAutomatically();
			});
		}

		private class Service
		{
			public void Start()
			{
				//TestServerNHibernate testServerWcfNHibernate = new TestServerNHibernate();
				//testServerWcfNHibernate.Start();
				//testServerWcfNHibernate.Publish();

				//TestServerSql testServerWcfSql = new TestServerSql();
				//testServerWcfSql.Start();
				//testServerWcfSql.Publish();

				TestServerObjects testServerObjects = new TestServerObjects();
				testServerObjects.Start();
				testServerObjects.Publish();

				//TestServerEntityFramework4 testServerEntityFramework4 = new TestServerEntityFramework4();
				//testServerEntityFramework4.Start();
				//testServerEntityFramework4.Publish();

				//Console.WriteLine("Server is up and running...");
				//Console.ReadLine();
			}

			public void Stop()
			{
				// TODO: dispose of all services
			}
		}
	}
}
