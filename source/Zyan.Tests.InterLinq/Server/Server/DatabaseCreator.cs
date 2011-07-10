using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace InterLinq.UnitTests.Server
{
    public class DatabaseCreator
    {

        public static void BuildEmptyDatabase(string connectionString, bool askUserBeforeDelDb, String databaseName, String createScript, String integrityScript, Assembly resourceContainingAssembly)
        {
            List<string> createScripts = new List<string> {createScript};
            List<string> integrityScripts = new List<string> {integrityScript};
            BuildEmptyDatabase(connectionString, askUserBeforeDelDb, databaseName, createScripts, integrityScripts, resourceContainingAssembly);
        }

        public static void BuildEmptyDatabase(string connectionString, bool askUserBeforeDelDb, String databaseName, IEnumerable<string> createScripts, IEnumerable<string> integrityScripts, Assembly resourceContainingAssembly)
        {
            IDbConnection dbConnection = Connect(connectionString);

            CreateDatabase(dbConnection, askUserBeforeDelDb, databaseName);
            UseTestDatabase(dbConnection, databaseName);
            foreach (string createScript in createScripts)
            {
                CreateTables(dbConnection, createScript, resourceContainingAssembly);
            }
            foreach (string integrityScript in integrityScripts)
            {
                CreateIntegrity(dbConnection, integrityScript, resourceContainingAssembly);
            }
        }

        public static void BuildEmptyDatabase(string connectionString, bool askUserBeforeDelDb, String databaseName)
        {
            IDbConnection dbConnection = Connect(connectionString);

            CreateDatabase(dbConnection, askUserBeforeDelDb, databaseName);
            UseTestDatabase(dbConnection, databaseName);
        }

        private static IDbConnection Connect(String connectionString)
        {
            Console.WriteLine("Create and open the connection to the database...");
            IDbConnection dbConnection = new MySqlConnection(connectionString);
            dbConnection.Open();
            Console.WriteLine("Connected to the database :)");
            return dbConnection;
        }

        private static void CreateDatabase(IDbConnection dbConnection, bool askUserBeforeDelDb, String databaseName)
        {
            Console.WriteLine("Create database \"" + databaseName + "\"...");

            IDbCommand existsCommand = dbConnection.CreateCommand();
            existsCommand.CommandText = "SHOW DATABASES LIKE \"" + databaseName + "\";";
            IDataReader existsResult = existsCommand.ExecuteReader();
            bool dbAlreadyExists = existsResult.Read();
            existsResult.Close();
            existsResult.Dispose();
            if (dbAlreadyExists)
            {
                if (askUserBeforeDelDb)
                {
                    if (DialogResult.Cancel == MessageBox.Show("The database  \"" + databaseName + "\" already exists. This database will be deleted. Otherwise the PmMda.Net test cannot proceed.", "Database already exists", MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
                    {
                        throw new Exception("Test aborted because the user rejected to delete the existing \"" + databaseName + "\" database.");
                    }
                }
                IDbCommand deleteDbCommand = dbConnection.CreateCommand();
                deleteDbCommand.CommandText = "DROP DATABASE " + databaseName + ";";
                deleteDbCommand.ExecuteScalar();
            }

            IDbCommand createDbCommand = dbConnection.CreateCommand();
            createDbCommand.CommandText = "CREATE DATABASE " + databaseName + ";";
            createDbCommand.ExecuteScalar();
            Console.WriteLine("Database \"" + databaseName + "\" created successfully :)");
        }
        private static void UseTestDatabase(IDbConnection dbConnection, String databaseName)
        {
            IDbCommand useTestDbCommand = dbConnection.CreateCommand();
            useTestDbCommand.CommandText = "Use " + databaseName + ";";
            useTestDbCommand.ExecuteScalar();
        }

        private static void CreateTables(IDbConnection dbConnection, String resourceName, Assembly resourceContainingAssembly)
        {
            Console.WriteLine("Create the tables for the persistent data objects...");
            IDbTransaction t = dbConnection.BeginTransaction();
            IDbCommand createTablesScript = dbConnection.CreateCommand();
            Stream createTablesStream = resourceContainingAssembly.GetManifestResourceStream(resourceName);
            if (createTablesStream == null)
            {
                throw new Exception("Could not find the database script used to generate the database tables");
            }
            StreamReader createTablesReader = new StreamReader(createTablesStream);
            createTablesScript.CommandText = createTablesReader.ReadToEnd();
            createTablesReader.Close();
            createTablesScript.ExecuteScalar();
            t.Commit();
            Console.WriteLine("Successfully created all database tables :)");
        }

        private static void CreateIntegrity(IDbConnection dbConnection, String resourceName, Assembly resourceContainingAssembly)
        {
            Console.WriteLine("Creating database integrity...");
            IDbTransaction t = dbConnection.BeginTransaction();
            Stream createIntegrityStream = resourceContainingAssembly.GetManifestResourceStream(resourceName);
            if (createIntegrityStream == null)
            {
                throw new Exception("Could not find the database script used to generate the database integrity");
            }
            StreamReader createIntegrityReader = new StreamReader(createIntegrityStream);
            string integrityCommands = createIntegrityReader.ReadToEnd();
            createIntegrityReader.Close();
            // Somehow (don't know why) the commands doesn't succeed if they will be executed as one string :(
            // --> Split the commands into standalone commands and execute one after an other.
            foreach (string integrityCommand in SplitCommands(integrityCommands))
            {
                IDbCommand createIntegrityScript = dbConnection.CreateCommand();
                createIntegrityScript.CommandText = integrityCommand + ";";
                createIntegrityScript.ExecuteScalar();
            }

            t.Commit();
            Console.WriteLine("Succesfully created database integrity :)");
        }

        public static void DeleteDatabase(string connectionString, String databaseName)
        {
            IDbConnection dbConnection = new MySqlConnection(connectionString);
            dbConnection.Open();
            IDbTransaction t = dbConnection.BeginTransaction();
            try
            {
                IDbCommand delDbCommand = dbConnection.CreateCommand();
                delDbCommand.CommandText = "DROP DATABASE " + databaseName + ";";
                delDbCommand.ExecuteScalar();
                t.Commit();
                Console.WriteLine("Successfully deleted the " + databaseName + " database :)");
            }
            catch (Exception)
            {
                t.Rollback();
                throw;
            }
            finally
            {
                dbConnection.Close();
            }
        }

        private static string[] SplitCommands(string commands)
        {
            //string.Split does not work as I would like ;)
            List<String> commandsList = new List<string>();
            int semicolonPos = 0;
            int oldPos = 0;
            while (semicolonPos > -1)
            {
                semicolonPos = commands.IndexOf(';', oldPos);
                if (semicolonPos > -1)
                {
                    string command = commands.Substring(oldPos, semicolonPos - oldPos + 1);
                    commandsList.Add(command);
                    oldPos = semicolonPos + 1;
                    //ignore new line and spaces till the new command starts
                    while (commands.Length > oldPos)
                    {
                        char currentChar = commands[oldPos];
                        if (currentChar == '\r' || currentChar == '\n' || currentChar == ' ')
                        {
                            oldPos++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return commandsList.ToArray();
        }
    }
}
