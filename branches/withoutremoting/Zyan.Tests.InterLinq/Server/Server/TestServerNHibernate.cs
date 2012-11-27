using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using InterLinq.Communication.Wcf;
using InterLinq.NHibernate;
using InterLinq.UnitTests.Artefacts.NHibernate;
using NHibernate;
using NHibernate.Cfg;
using InterLinq.Communication.Remoting;
using System.Collections;
using System.Runtime.Remoting.Channels;

namespace InterLinq.UnitTests.Server
{
    public class TestServerNHibernate : TestServer
    {

        #region Fields

        private ISessionFactory sessionFactory;
        private readonly string[] ArtefactNamespaces = new[] { "InterLinq.UnitTests.Artefacts.NHibernate" };

        #endregion

        #region Properties

        public override string DatabaseName
        {
            get { return "linqtests"; }
        }

        public override string CreateScriptName
        {
            get { return "Company.CreateTables.MySQL.sql"; }
        }

        public override string IntegrityScriptName
        {
            get { return "Company.CreateIntegrity.MySQL.sql"; }
        }

        #endregion

        public override void Start()
        {

            #region Create Database

            string server = "localhost";
            string username = "root";
            string password = string.Empty;
            if (CredentialsDialog.ShowDialog(null, ref server, ref username, ref password) != DialogResult.OK)
            {
                Console.WriteLine("The test was aborted by the user!");
                throw new Exception("Test aborted by user");
            }
            connectionString = GetConnectionString(server, username, password);

            List<string> createScripts = new List<string>();
            List<string> integrityScripts = new List<string>();
            foreach (string t in ArtefactNamespaces)
            {
                createScripts.Add(string.Concat(t, ".", CreateScriptName));
                integrityScripts.Add(string.Concat(t, ".", IntegrityScriptName));
            }

            DatabaseCreator.BuildEmptyDatabase(connectionString, true, DatabaseName, createScripts, integrityScripts, Assembly.GetAssembly(typeof(Company)));

            #endregion

            #region Initialize NHibernate

            Configuration configuration = new Configuration();
            configuration.AddAssembly(typeof(Company).Assembly);
            configuration.Properties["connection.connection_string"] = "Database=" + DatabaseName + ";" + connectionString;
            sessionFactory = configuration.BuildSessionFactory();

            #endregion

            CreateData();
        }

        public override void Publish()
        {
            // Create the QueryHandler
            IQueryHandler queryHandler = new NHibernateQueryHandler(sessionFactory);

            #region Start the WCF server

            ServerQueryWcfHandler wcfServer = new ServerQueryWcfHandler(queryHandler);

            NetTcpBinding netTcpBinding = ServiceHelper.GetNetTcpBinding();
            string serviceUri = ServiceHelper.GetServiceUri(null, null, Artefacts.ServiceConstants.NhibernateServiceName);

            wcfServer.Start(netTcpBinding, serviceUri);

            #endregion

            #region Start the remoting server

            ServerQueryRemotingHandlerNHibernate remotingServer = new ServerQueryRemotingHandlerNHibernate(queryHandler);
            // Register default channel for remote accecss
            Hashtable properties = new Hashtable();
            properties["name"] = Artefacts.ServiceConstants.NhibernateServiceName;
            properties["port"] = Artefacts.ServiceConstants.NhibernatePort;
            IChannel currentChannel = RemotingConstants.GetDefaultChannel(properties);
            ChannelServices.RegisterChannel(currentChannel, false);
            remotingServer.Start(Artefacts.ServiceConstants.NhibernateServiceName, false);

            #endregion

        }

        private void CreateData()
        {
            ISession session = null;
            try
            {
                session = sessionFactory.OpenSession();


                Console.Write("Creating Company Structure.");

                // Company Instant-Guezli
                Company cInstantGuezli = new Company();
                cInstantGuezli.Name = "Instant-Guezli";
                cInstantGuezli.Foundation = new DateTime(1900, 01, 01);
                cInstantGuezli.ChiefExecutiveOfficer = new Employee();
                cInstantGuezli.ChiefExecutiveOfficer.Name = "Sio Wernli";
                cInstantGuezli.ChiefExecutiveOfficer.IsMale = true;
                cInstantGuezli.ChiefExecutiveOfficer.Salary = 20000.00d;
                cInstantGuezli.ChiefExecutiveOfficer.Grade = 10;
                // Departments (3)
                cInstantGuezli.Departments.Add(new Department());
                cInstantGuezli.Departments.Add(new Department());
                cInstantGuezli.Departments.Add(new Department());
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
                // Employees
                cInstantGuezli.Departments[0].Employees.Add(new Employee());
                cInstantGuezli.Departments[0].Employees.Add(new Employee());
                cInstantGuezli.Departments[0].Employees.Add(new Employee());
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
                // Employees
                cInstantGuezli.Departments[1].Employees.Add(new Employee());
                cInstantGuezli.Departments[1].Employees.Add(new Employee());
                cInstantGuezli.Departments[1].Employees.Add(new Employee());
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
                // Employees
                cInstantGuezli.Departments[2].Employees.Add(new Employee());
                cInstantGuezli.Departments[2].Employees.Add(new Employee());
                cInstantGuezli.Departments[2].Employees.Add(new Employee());
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

                session.SaveOrUpdate(cInstantGuezli.ChiefExecutiveOfficer);
                session.SaveOrUpdate(cInstantGuezli);
                foreach (Department dep in cInstantGuezli.Departments)
                {
                    session.SaveOrUpdate(dep.Company);
                    session.SaveOrUpdate(dep.Manager);
                    session.SaveOrUpdate(dep);
                    foreach (Employee emp in dep.Employees)
                    {
                        session.SaveOrUpdate(emp.Department);
                        session.SaveOrUpdate(emp);
                    }
                }

                Console.WriteLine("Initialization completed.");
            }
            finally
            {
                if (session != null)
                {
                    session.Close();
                }
            }
        }

        protected string GetConnectionString(string server, string username, string password)
        {
            StringBuilder mySqlConnectionString = new StringBuilder();
            mySqlConnectionString.Append("Data Source=");
            mySqlConnectionString.Append(server);
            mySqlConnectionString.Append(";User Id=");
            mySqlConnectionString.Append(username);
            mySqlConnectionString.Append(";Password=");
            mySqlConnectionString.Append(password);
            return mySqlConnectionString.ToString();
        }
    }
}
