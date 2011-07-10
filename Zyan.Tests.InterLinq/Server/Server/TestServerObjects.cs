using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.ServiceModel;
using InterLinq.UnitTests.Artefacts.Objects;
using Zyan.InterLinq;
using Zyan.InterLinq.Communication.Remoting;
using Zyan.InterLinq.Communication.Wcf;

namespace InterLinq.UnitTests.Server
{
	public class TestServerObjects : TestServer, IObjectSource
	{
		private readonly List<Company> companies = new List<Company>();
		private readonly List<Department> departments = new List<Department>();
		private readonly List<Employee> employees = new List<Employee>();

		public override string DatabaseName
		{
			get { throw new NotImplementedException(); }
		}

		public override string CreateScriptName
		{
			get { throw new NotImplementedException(); }
		}

		public override string IntegrityScriptName
		{
			get { throw new NotImplementedException(); }
		}

		public override void Start()
		{
			CreateData();
		}

		public override void Publish()
		{
			// Create the QueryHandler
			IQueryHandler queryHandler = new ZyanObjectQueryHandler(this);

			#region Start the WCF server

			ServerQueryWcfHandler wcfServer = new ServerQueryWcfHandler(queryHandler);

			NetTcpBinding netTcpBinding = ServiceHelper.GetNetTcpBinding();
			string serviceUri = ServiceHelper.GetServiceUri(null, null, Artefacts.ServiceConstants.ObjectsServiceName);

			wcfServer.Start(netTcpBinding, serviceUri);

			#endregion

			#region Start the remoting server

			ServerQueryRemotingHandlerObjects remotingServer = new ServerQueryRemotingHandlerObjects(queryHandler);
			// Register default channel for remote access
			Hashtable properties = new Hashtable();
			properties["name"] = Artefacts.ServiceConstants.ObjectsServiceName;
			properties["port"] = Artefacts.ServiceConstants.ObjectsPort;
			IChannel currentChannel = RemotingConstants.GetDefaultChannel(properties);
			ChannelServices.RegisterChannel(currentChannel, false);
			remotingServer.Start(Artefacts.ServiceConstants.ObjectsServiceName, false);

			#endregion
		}

		private void CreateData()
		{
			// Company Instant-Guezli
			Company cInstantGuezli = new Company();
			companies.Add(cInstantGuezli);
			cInstantGuezli.Name = "Instant-Guezli";
			cInstantGuezli.Foundation = new DateTime(1900, 01, 01);
			cInstantGuezli.ChiefExecutiveOfficer = new Employee();
			cInstantGuezli.ChiefExecutiveOfficer.Name = "Sio Wernli";
			cInstantGuezli.ChiefExecutiveOfficer.IsMale = true;
			cInstantGuezli.ChiefExecutiveOfficer.Salary = 20000.00d;
			cInstantGuezli.ChiefExecutiveOfficer.Grade = 10;
			employees.Add(cInstantGuezli.ChiefExecutiveOfficer);
			// Departments (3)
			cInstantGuezli.Departments.Add(new Department());
			cInstantGuezli.Departments.Add(new Department());
			cInstantGuezli.Departments.Add(new Department());
			departments.AddRange(cInstantGuezli.Departments);
			#region Department 1
			// General
			cInstantGuezli.Departments[0].Foundation = new DateTime(1910, 01, 01);
			cInstantGuezli.Departments[0].Name = "Instant-Guezli Research";
			cInstantGuezli.Departments[0].QualityLevel = 7;
			cInstantGuezli.Departments[0].Company = cInstantGuezli;
			// Manager
			cInstantGuezli.Departments[0].Manager = new Employee();
			cInstantGuezli.Departments[0].Manager.Name = "Dr. Rainer Hohn";
			cInstantGuezli.Departments[0].Manager.IsMale = true;
			cInstantGuezli.Departments[0].Manager.Salary = 10000.00d;
			cInstantGuezli.Departments[0].Manager.Grade = 9;
			cInstantGuezli.Departments[0].Manager.Department = cInstantGuezli.Departments[0];
			employees.Add(cInstantGuezli.Departments[0].Manager);
			// Employees
			cInstantGuezli.Departments[0].Employees.Add(new Employee());
			cInstantGuezli.Departments[0].Employees.Add(new Employee());
			cInstantGuezli.Departments[0].Employees.Add(new Employee());
			employees.AddRange(cInstantGuezli.Departments[0].Employees);
			// Employee 1
			cInstantGuezli.Departments[0].Employees[0].Name = "Stephanie Süss";
			cInstantGuezli.Departments[0].Employees[0].IsMale = false;
			cInstantGuezli.Departments[0].Employees[0].Salary = 5000.00d;
			cInstantGuezli.Departments[0].Employees[0].Grade = 6;
			cInstantGuezli.Departments[0].Employees[0].Department = cInstantGuezli.Departments[0];
			// Employee 2
			cInstantGuezli.Departments[0].Employees[1].Name = "Manuela Zucker";
			cInstantGuezli.Departments[0].Employees[1].IsMale = false;
			cInstantGuezli.Departments[0].Employees[1].Salary = 6000.00d;
			cInstantGuezli.Departments[0].Employees[1].Grade = 7;
			cInstantGuezli.Departments[0].Employees[1].Department = cInstantGuezli.Departments[0];
			// Employee 3
			cInstantGuezli.Departments[0].Employees[2].Name = "Harry Bo";
			cInstantGuezli.Departments[0].Employees[2].IsMale = true;
			cInstantGuezli.Departments[0].Employees[2].Salary = 4000.00d;
			cInstantGuezli.Departments[0].Employees[2].Grade = 4;
			cInstantGuezli.Departments[0].Employees[2].Department = cInstantGuezli.Departments[0];
			#endregion
			#region Department 2
			// General
			cInstantGuezli.Departments[1].Foundation = new DateTime(1900, 01, 01);
			cInstantGuezli.Departments[1].Name = "Instant-Guezli Production";
			cInstantGuezli.Departments[1].QualityLevel = 8;
			cInstantGuezli.Departments[1].Company = cInstantGuezli;
			// Manager
			cInstantGuezli.Departments[1].Manager = new Employee();
			cInstantGuezli.Departments[1].Manager.Name = "Prof. Joe Kolade";
			cInstantGuezli.Departments[1].Manager.IsMale = true;
			cInstantGuezli.Departments[1].Manager.Salary = 11000.00d;
			cInstantGuezli.Departments[1].Manager.Grade = 10;
			cInstantGuezli.Departments[1].Manager.Department = cInstantGuezli.Departments[1];
			employees.Add(cInstantGuezli.Departments[1].Manager);
			// Employees
			cInstantGuezli.Departments[1].Employees.Add(new Employee());
			cInstantGuezli.Departments[1].Employees.Add(new Employee());
			cInstantGuezli.Departments[1].Employees.Add(new Employee());
			employees.AddRange(cInstantGuezli.Departments[1].Employees);
			// Employee 1
			cInstantGuezli.Departments[1].Employees[0].Name = "Rainer Zufall";
			cInstantGuezli.Departments[1].Employees[0].IsMale = true;
			cInstantGuezli.Departments[1].Employees[0].Salary = 3000.00d;
			cInstantGuezli.Departments[1].Employees[0].Grade = 2;
			cInstantGuezli.Departments[1].Employees[0].Department = cInstantGuezli.Departments[1];
			// Employee 2
			cInstantGuezli.Departments[1].Employees[1].Name = "Clara Vorteil";
			cInstantGuezli.Departments[1].Employees[1].IsMale = false;
			cInstantGuezli.Departments[1].Employees[1].Salary = 6500.00d;
			cInstantGuezli.Departments[1].Employees[1].Grade = 5;
			cInstantGuezli.Departments[1].Employees[1].Department = cInstantGuezli.Departments[1];
			// Employee 3
			cInstantGuezli.Departments[1].Employees[2].Name = "Johannes Beer";
			cInstantGuezli.Departments[1].Employees[2].IsMale = true;
			cInstantGuezli.Departments[1].Employees[2].Salary = 5500.00d;
			cInstantGuezli.Departments[1].Employees[2].Grade = 6;
			cInstantGuezli.Departments[1].Employees[2].Department = cInstantGuezli.Departments[1];
			#endregion
			#region Department 3
			// General
			cInstantGuezli.Departments[2].Foundation = new DateTime(1980, 05, 05);
			cInstantGuezli.Departments[2].Name = "Instant-Guezli Import";
			cInstantGuezli.Departments[2].QualityLevel = 3;
			cInstantGuezli.Departments[2].Company = cInstantGuezli;
			// Manager
			cInstantGuezli.Departments[2].Manager = new Employee();
			cInstantGuezli.Departments[2].Manager.Name = "Anna Nass";
			cInstantGuezli.Departments[2].Manager.IsMale = false;
			cInstantGuezli.Departments[2].Manager.Salary = 12000.00d;
			cInstantGuezli.Departments[2].Manager.Grade = 8;
			cInstantGuezli.Departments[2].Manager.Department = cInstantGuezli.Departments[2];
			employees.Add(cInstantGuezli.Departments[2].Manager);
			// Employees
			cInstantGuezli.Departments[2].Employees.Add(new Employee());
			cInstantGuezli.Departments[2].Employees.Add(new Employee());
			cInstantGuezli.Departments[2].Employees.Add(new Employee());
			employees.AddRange(cInstantGuezli.Departments[2].Employees);
			// Employee 1
			cInstantGuezli.Departments[2].Employees[0].Name = "Lee Mone";
			cInstantGuezli.Departments[2].Employees[0].IsMale = true;
			cInstantGuezli.Departments[2].Employees[0].Salary = 3500.00d;
			cInstantGuezli.Departments[2].Employees[0].Grade = 1;
			cInstantGuezli.Departments[2].Employees[0].Department = cInstantGuezli.Departments[2];
			// Employee 2
			cInstantGuezli.Departments[2].Employees[1].Name = "Franziska Ner";
			cInstantGuezli.Departments[2].Employees[1].IsMale = false;
			cInstantGuezli.Departments[2].Employees[1].Salary = 5000.00d;
			cInstantGuezli.Departments[2].Employees[1].Grade = 6;
			cInstantGuezli.Departments[2].Employees[1].Department = cInstantGuezli.Departments[2];
			// Employee 3
			cInstantGuezli.Departments[2].Employees[2].Name = "Gertraut Sichnicht";
			cInstantGuezli.Departments[2].Employees[2].IsMale = false;
			cInstantGuezli.Departments[2].Employees[2].Salary = 4500.00d;
			cInstantGuezli.Departments[2].Employees[2].Grade = 5;
			cInstantGuezli.Departments[2].Employees[2].Department = cInstantGuezli.Departments[2];
			#endregion

			// Set the Id's
			for (int i = 0; i < companies.Count; i++)
			{
				companies[i].Id = i + 1;
			}
			for (int i = 0; i < departments.Count; i++)
			{
				departments[i].Id = i + 1;
			}
			for (int i = 0; i < employees.Count; i++)
			{
				employees[i].Id = i + 1;
			}
		}

		#region IObjectSource Members

		public IEnumerable<T> Get<T>() where T : class
		{
			if (typeof(T) == typeof(Company))
			{
				return (IEnumerable<T>)companies;
			}
			if (typeof(T) == typeof(Department))
			{
				return (IEnumerable<T>)departments;
			}
			if (typeof(T) == typeof(Employee))
			{
				return (IEnumerable<T>)employees;
			}
			throw new NotSupportedException("Unknown type requested.");
		}

		#endregion
	}
}
