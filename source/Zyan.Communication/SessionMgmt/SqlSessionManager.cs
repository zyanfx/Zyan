using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Text;
using System.Transactions;

namespace Zyan.Communication.SessionMgmt
{
	/// <summary>
	/// Session manager backed with SQL Server Database storage.
	/// <remarks>
	/// Session table has the following structure:
	/// <list type="list">
	/// <item><term>SessionID</term><description>uniqueidentifier</description></item>
	/// <item><term>SessionTimestamp</term><description>datetime</description></item>
	/// <item><term>IdentityName</term><description>nvarchar(255)</description></item>
	/// </list>
	/// </remarks>
	/// </summary>
	public class SqlSessionManager : SessionManagerBase
	{
		// Connection string
		private string _connectionString = string.Empty;

		// SQL server schema name
		private string _sqlSchema = string.Empty;

		// Table names
		private string _sqlSessionTableName = string.Empty;
		private string _sqlVariablesTableName = string.Empty;

		/// <summary>
		/// Creates a new instance of <see cref="SqlSessionManager"/>
		/// </summary>
		/// <param name="connectionString">SQL Server database connection string.</param>
		/// <param name="sqlSchema">Database name (for example: "dbo").</param>
		/// <param name="sqlSessionTableName">Session table name.</param>
		/// <param name="sqlVariablesTableName">Session variable table name.</param>
		public SqlSessionManager(string connectionString, string sqlSchema, string sqlSessionTableName, string sqlVariablesTableName)
		{
			_connectionString = connectionString;
			_sqlSchema = sqlSchema;
			_sqlSessionTableName = sqlSessionTableName;
			_sqlVariablesTableName = sqlVariablesTableName;

			// Make sure that session and variable tables exist in the SQL Server database
			EnsureSessionTableCreated();
			EnsureVariablesTableCreated();
		}

		#region SQL Server persistence

		/// <summary>
		/// Checks whether a particular table exists in the SQL Server database.
		/// </summary>
		/// <param name="schema">SQL Server schema (e.g. dbo)</param>
		/// <param name="tableName">The name of the table.</param>
		/// <returns>True, if the table exists, otherwise, false.</returns>
		private bool ExistSqlTable(string schema, string tableName)
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Connect to the SQL server
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// Assemble SQL query
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.Append("SELECT @tableCount=COUNT([object_id]) ");
					sqlBuilder.Append("FROM sys.tables ");
					sqlBuilder.Append("WHERE name = @tableName AND schema_id = SCHEMA_ID(@schemaName)");

					// Generate SQL command
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Prepare parameters
						SqlParameter tableCountParam = new SqlParameter("@tableCount", SqlDbType.Int);
						tableCountParam.Direction = ParameterDirection.Output;
						command.Parameters.Add(tableCountParam);

						// Set input parameter values
						command.Parameters.Add("@tableName", SqlDbType.NVarChar, 255).Value = tableName;
						command.Parameters.Add("@schemaName", SqlDbType.NVarChar, 255).Value = schema;

						// Execute the SQL query
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();
						scope.Complete();

						return ((int)tableCountParam.Value) == 1;
					}
				}
			}
		}

		/// <summary>
		/// Ensures that the session table exists in the SQL Server database.
		/// </summary>
		private void EnsureSessionTableCreated()
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// If the table doesn't exist, create it
				if (!ExistSqlTable(_sqlSchema, _sqlSessionTableName))
				{
					using (SqlConnection connection = new SqlConnection(_connectionString))
					{
						StringBuilder sqlBuilder = new StringBuilder();
						sqlBuilder.Append("CREATE TABLE ");
						sqlBuilder.AppendFormat("[{0}].[{1}] (", _sqlSchema, _sqlSessionTableName);
						sqlBuilder.Append("[AutoID] int IDENTITY(1,1) NOT NULL,");
						sqlBuilder.Append("[SessionID] uniqueidentifier NOT NULL,");
						sqlBuilder.Append("[SessionTimestamp] datetime NOT NULL,");
						sqlBuilder.Append("[IdentityName] nvarchar(255) NOT NULL,");
						sqlBuilder.AppendFormat("CONSTRAINT [PK_{0}_SessionID] PRIMARY KEY NONCLUSTERED ", _sqlSessionTableName);
						sqlBuilder.Append("([SessionID] ASC),");
						sqlBuilder.AppendFormat("CONSTRAINT [IX_{0}] UNIQUE CLUSTERED ", _sqlSessionTableName);
						sqlBuilder.Append("([AutoID] ASC))");

						using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
						{
							connection.Open();
							command.ExecuteNonQuery();
							connection.Close();
						}
					}
				}

				// Commit the transaction
				scope.Complete();
			}
		}

		/// <summary>
		/// Ensures that the variable table exists in SQL Server database.
		/// </summary>
		private void EnsureVariablesTableCreated()
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// If the table doesn't exist, create it
				if (!ExistSqlTable(_sqlSchema, _sqlVariablesTableName))
				{
					using (SqlConnection connection = new SqlConnection(_connectionString))
					{
						StringBuilder sqlBuilder = new StringBuilder();
						sqlBuilder.Append("CREATE TABLE ");
						sqlBuilder.AppendFormat("[{0}].[{1}] (", _sqlSchema, _sqlVariablesTableName);
						sqlBuilder.Append("[SessionID] uniqueidentifier NOT NULL,");
						sqlBuilder.Append("[VariableName] nvarchar(255) NOT NULL,");
						sqlBuilder.Append("[VariableValue] varbinary(MAX) NULL,");
						sqlBuilder.AppendFormat("CONSTRAINT [PK_{0}_SessionID_VariableName] PRIMARY KEY ", _sqlSessionTableName);
						sqlBuilder.Append("([SessionID], [VariableName]))");

						using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
						{
							connection.Open();
							command.ExecuteNonQuery();
							connection.Close();
						}
					}
				}

				// Commit the transaction
				scope.Complete();
			}
		}

		/// <summary>
		/// Gets the specific session from the SQL Server database.
		/// </summary>
		/// <param name="sessionID">Session identity.</param>
		/// <returns>Session instance.</returns>
		private ServerSession GetSessionFromSqlServer(Guid sessionID)
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Connect to SQL Server
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// Prepare SQL command
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.Append("SELECT SessionID,SessionTimestamp,IdentityName ");
					sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.Append("WHERE SessionID=@sessionID");

					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Add parameters
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

						try
						{
							connection.Open();

							using (SqlDataReader reader = command.ExecuteReader())
							{
								if (reader.Read())
								{
									// Read session id, date and user name
									var session = CreateServerSession
									(
										reader.GetGuid(0),
										reader.GetDateTime(1),
										new GenericIdentity(reader.GetString(2))
									);

									// Commit the transaction
									scope.Complete();
									return session;
								}

								// Commit the transaction
								scope.Complete();
								return null;
							}
						}
						finally
						{
							connection.Close();
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets the value of a particular session variable from the SQL Server database.
		/// </summary>
		/// <param name="sessionID">Session identity.</param>
		/// <param name="variableName">The name of the variable.</param>
		/// <returns>The value of the variable</returns>
		private object GetVariableFromSqlServer(Guid sessionID, string variableName)
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.Append("SELECT VariableValue ");
					sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlVariablesTableName);
					sqlBuilder.Append("WHERE SessionID=@sessionID AND VariableName=@variableName");

					// Prepare the command
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Create input parameters
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;
						command.Parameters.Add("@variableName", SqlDbType.NVarChar, 255).Value = variableName;

						try
						{
							connection.Open();

							using (SqlDataReader reader = command.ExecuteReader())
							{
								if (reader.Read())
								{
									// Read raw data
									SqlBytes raw = reader.GetSqlBytes(0);

									// Commit the transaction
									scope.Complete();

									if (raw.IsNull)
										return null;

									BinaryFormatter formatter = new BinaryFormatter();
									MemoryStream stream = new MemoryStream(raw.Value);

									// Deserialize the binary data
									object managedValue = formatter.Deserialize(stream);

									// Close the stream and return variable data
									stream.Close();
									return managedValue;
								}

								// Commit the transaction
								scope.Complete();
								return null;
							}

						}
						finally
						{
							connection.Close();
						}
					}
				}
			}
		}

		/// <summary>
		/// Sets the value of a particular session variable in the SQL Server database.
		/// </summary>
		/// <param name="sessionID">Session identity.</param>
		/// <param name="variableName">The name of the variable.</param>
		/// <param name="variableValue">The value of the variable</param>
		private void SetVariableOnSqlServer(Guid sessionID, string variableName, object variableValue)
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] ", _sqlSchema, _sqlVariablesTableName);
					sqlBuilder.Append("WHERE SessionID=@sessionID AND VariableName=@variableName; ");
					sqlBuilder.AppendFormat("INSERT [{0}].[{1}] (SessionID,VariableName,VariableValue) ", _sqlSchema, _sqlVariablesTableName);
					sqlBuilder.Append("VALUES (@sessionID,@variableName,@variableValue) ");

					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;
						command.Parameters.Add("@variableName", SqlDbType.NVarChar, 255).Value = variableName;

						// No value was specified...
						if (variableValue == null)
						{
							// Specify DBNull as parameter value
							command.Parameters.Add("@variableValue", SqlDbType.VarBinary, -1).Value = DBNull.Value;
						}
						else
						{
							BinaryFormatter formatter = new BinaryFormatter();
							MemoryStream stream = new MemoryStream();

							// Serialize the binary data and set parameter value
							formatter.Serialize(stream, variableValue);
							command.Parameters.Add("@variableValue", SqlDbType.VarBinary, -1).Value = stream.ToArray();
							stream.Close();
						}

						try
						{
							// Execute the query
							connection.Open();
							command.ExecuteNonQuery();
						}
						finally
						{
							connection.Close();
						}
					}
				}

				// Commit the transaction
				scope.Complete();
			}
		}

		/// <summary>
		/// Returns whether the given session exists in the SQL Server database.
		/// </summary>
		/// <param name="sessionID">Session identity.</param>
		/// <returns>True, if the session exists, otherwise, false.</returns>
		private bool ExistSessionOnSqlServer(Guid sessionID)
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.Append("SELECT @sessionCount=COUNT(SessionID)");
					sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.Append("WHERE SessionID=@sessionID");

					// Execute the SQL command
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

						// Create output parameter
						SqlParameter sessionCountParam = new SqlParameter()
						{
							ParameterName = "@sessionCount",
							SqlDbType = SqlDbType.Int,
							Direction = ParameterDirection.Output
						};
						command.Parameters.Add(sessionCountParam);

						// Execute the query
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();

						// commit the transaction
						scope.Complete();
						return ((int)sessionCountParam.Value) == 1;
					}
				}
			}
		}

		/// <summary>
		/// Stores a session in the SQL Server database.
		/// </summary>
		/// <param name="session">The <see cref="ServerSession"/> to store.</param>
		private void StoreSessionOnSqlServer(ServerSession session)
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				if (session == null)
					throw new ArgumentNullException("session");

				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// Create SQL statement to delete and recreate the session
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] WHERE SessionID=@sessionID; ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.AppendFormat("INSERT [{0}].[{1}] (SessionID,SessionTimestamp,IdentityName) ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.Append("VALUES (@sessionID,@sessionTimestamp,@identityName)");

					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = session.SessionID;
						command.Parameters.Add("@sessionTimestamp", SqlDbType.DateTime).Value = session.Timestamp;
						command.Parameters.Add("@identityName", SqlDbType.NVarChar, 255).Value = session.Identity.Name;

						// Execute the query and commit the transaction
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();
						scope.Complete();
					}
				}
			}
		}

		/// <summary>
		/// Updates a session in the SQL Server database.
		/// </summary>
		/// <param name="session">The <see cref="ServerSession"/> to update.</param>
		private void RenewSessionOnSqlServer(ServerSession session)
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// Prepare the SQL statement
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.AppendFormat("UPDATE [{0}].[{1}] SET SessionTimestamp=@sessionTimestamp WHERE SessionID=@sessionID", _sqlSchema, _sqlSessionTableName);

					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = session.SessionID;
						command.Parameters.Add("@sessionTimestamp", SqlDbType.DateTime).Value = session.Timestamp;

						// Execute the statement and commit the transaction
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();
						scope.Complete();
					}
				}
			}
		}

		/// <summary>
		/// Deletes the given session from the SQL Server database.
		/// </summary>
		/// <param name="sessionID">Session identity.</param>
		private void RemoveSessionFromSqlServer(Guid sessionID)
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// Create the delete SQL statement
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] WHERE SessionID=@sessionID", _sqlSchema, _sqlSessionTableName);

					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

						// Execute the statement and commit the transaction
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();
						scope.Complete();
					}
				}
			}
		}

		/// <summary>
		/// Removes expired sessions from the SQL Server database.
		/// </summary>
		private void SweepExpiredSessionsFromSqlServer()
		{
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.Append("WHERE DATEDIFF(minute,SessionTimestamp,GETDATE())>@sessionAgeLimit");

					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						command.Parameters.Add("@sessionAgeLimit", SqlDbType.Int).Value = SessionAgeLimit;

						// Execute the statement and commit the transaction
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();
						scope.Complete();
					}
				}
			}
		}

		#endregion

		#region Overridden methods

		/// <summary>
		/// Checks whether the given session exists.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <returns>
		/// True if <see cref="ServerSession"/> with the given identifier exists, otherwise, false.
		/// </returns>
		public override bool ExistSession(Guid sessionID)
		{
			return ExistSessionOnSqlServer(sessionID);
		}

		/// <summary>
		/// Returns <see cref="ServerSession"/> identified by sessionID.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <returns>
		///   <see cref="ServerSession"/> or null, if session with the given identifier is not found.
		/// </returns>
		public override ServerSession GetSessionBySessionID(Guid sessionID)
		{
			return GetSessionFromSqlServer(sessionID);
		}

		/// <summary>
		/// Stores the given <see cref="ServerSession"/> to the session list.
		/// </summary>
		/// <param name="session"><see cref="ServerSession"/> to store.</param>
		public override void StoreSession(ServerSession session)
		{
			StoreSessionOnSqlServer(session);
		}

		/// <summary>
		/// Renews the given session.
		/// </summary>
		/// <param name="session">The <see cref="ServerSession" /> to renew.</param>
		public override void RenewSession(ServerSession session)
		{
			base.RenewSession(session);
			RenewSessionOnSqlServer(session);
		}

		/// <summary>
		/// Removes the given session from the session list.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		public override void RemoveSession(Guid sessionID)
		{
			RemoveSessionFromSqlServer(sessionID);
		}

		/// <summary>
		/// Removes all sessions older than SessionAgeLimit.
		/// </summary>
		protected override void SweepExpiredSessions()
		{
			SweepExpiredSessionsFromSqlServer();
		}

		/// <summary>
		/// Sets the new value of the session variable.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <param name="name">Variable name.</param>
		/// <param name="value">Variable value.</param>
		public override void SetSessionVariable(Guid sessionID, string name, object value)
		{
			if (ExistSession(sessionID))
			{
				SetVariableOnSqlServer(sessionID, name, value);
			}
		}

		/// <summary>
		/// Returns the value of the session variable.
		/// </summary>
		/// <param name="sessionID">Session unique identifier.</param>
		/// <param name="name">Variable name.</param>
		/// <returns>
		/// Value of the given session variable or null, if the variable is not defined.
		/// </returns>
		public override object GetSessionVariable(Guid sessionID, string name)
		{
			if (ExistSession(sessionID))
			{
				return GetVariableFromSqlServer(sessionID, name);
			}

			return null;
		}

		#endregion
	}
}
