using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Text;
using System.Timers;
using System.Transactions;

namespace Zyan.Communication.SessionMgmt
{
	/// <summary>
	/// Session manager backed with SQL Server Database storage.
	/// <remarks>
	/// Session table has the following structure:
	/// 
	/// SessionID           uniqueidentifier
	/// SessionTimestamp    datetime
	/// IdentityName        nvarchar(255)
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
		/// Gibt zurück, ob eine bestimmte Tabelle in der Verbundenen SQL Server Datenbank bereits angelegt wurde, oder nicht.
		/// </summary>
		/// <param name="schema">SQL Server-Schema (z.B. dbo)</param>
		/// <param name="tableName">Tabellenname</param>
		/// <returns>Wahr, wenn die Tabelle existiert, ansonsten Falsch</returns>
		private bool ExistSqlTable(string schema, string tableName)
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Verbindung zum SQL Server herstellen
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// SQL-Abfrage zusammenbauen
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.Append("SELECT @tableCount=COUNT([object_id]) ");
					sqlBuilder.Append("FROM sys.tables ");
					sqlBuilder.Append("WHERE name = @tableName AND schema_id = SCHEMA_ID(@schemaName)");

					// SQL-Befehl erzeugen
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Ausgbeparamater erzeugen
						SqlParameter tableCountParam = new SqlParameter("@tableCount", SqlDbType.Int);
						tableCountParam.Direction = ParameterDirection.Output;
						command.Parameters.Add(tableCountParam);

						// Eingabeparameter erzeugen
						command.Parameters.Add("@tableName", SqlDbType.NVarChar, 255).Value = tableName;
						command.Parameters.Add("@schemaName", SqlDbType.NVarChar, 255).Value = schema;

						// SQL-Abfrage ausfphren
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();

						// Transaktion abschließen
						scope.Complete();

						// Ausgabeparameter auswerten und Ergebnis zurückgeben
						return ((int)tableCountParam.Value) == 1;
					}
				}
			}
		}

		/// <summary>
		/// Stellt sicher, dass die Sitzungstabelle in der SQL Server Datenbank existiert.
		/// </summary>
		private void EnsureSessionTableCreated()
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Wenn die Tabelle nicht existiert ...
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
				// Transaktion abschließen
				scope.Complete();
			}
		}

		/// <summary>
		/// Stellt sicher, dass die Variablentabelle in der SQL Server Datenbank existiert.
		/// </summary>
		private void EnsureVariablesTableCreated()
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Wenn die Tabelle nicht existiert ...
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
				// Transaktion abschließen
				scope.Complete();
			}
		}

		/// <summary>
		/// Ruft eine bestimmte Sitzung aus der SQL Server Datenbank ab.
		/// </summary>
		/// <param name="sessionID">Sitzungsschlüssel</param>
		/// <returns>Sitzungsobjekt</returns>
		private ServerSession GetSessionFromSqlServer(Guid sessionID)
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Verbindung zum SQL Server herstellen
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// SQL-Abfrage zusammenstellen
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.Append("SELECT SessionID,SessionTimestamp,IdentityName ");
					sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.Append("WHERE SessionID=@sessionID");

					// SQL-Befehl erzeugen
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Eingabeparameter erzeugen
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

						try
						{
							// Verbindung öffnen
							connection.Open();

							// SQL-Abfrag ausführen
							using (SqlDataReader reader = command.ExecuteReader())
							{
								// Wenn Abfrageergebnisse vorhanden sind ...
								if (reader.Read())
								{
									// Sitzungsschlüssel lesen
									Guid sqlSessionID = reader.GetGuid(0);

									// Sitzung vom SQL Server lesen
									var session = CreateServerSession
									(
										sqlSessionID,
										reader.GetDateTime(1),
										new GenericIdentity(reader.GetString(2))
									);

									// Transaktion abschließen
									scope.Complete();

									// Sitzung zurückgeben
									return session;
								}
								// Transaktion abschließen
								scope.Complete();

								// Nichts zurückgeben
								return null;
							}

						}
						finally
						{
							// Verbindung schließen
							connection.Close();
						}
					}
				}
			}
		}

		/// <summary>
		/// Ruft den Wert einer bestimmten Sitzungsvariable aus der SQL Server Datenbank ab.
		/// </summary>
		/// <param name="sessionID">Sitzungsschlüssel</param>
		/// <param name="variableName">Variablenname</param>
		/// <returns>Wert der Variable</returns>
		private object GetVariableFromSqlServer(Guid sessionID, string variableName)
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Verbindung zum SQL Server herstellen
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// SQL-Abfrage zusammenstellen
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.Append("SELECT VariableValue ");
					sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlVariablesTableName);
					sqlBuilder.Append("WHERE SessionID=@sessionID AND VariableName=@variableName");

					// SQL-Befehl erzeugen
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Eingabeparameter erzeugen
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;
						command.Parameters.Add("@variableName", SqlDbType.NVarChar, 255).Value = variableName;

						try
						{
							// Verbindung öffnen
							connection.Open();

							// SQL-Abfrag ausführen
							using (SqlDataReader reader = command.ExecuteReader())
							{
								// Wenn Abfrageergebnisse vorhanden sind ...
								if (reader.Read())
								{
									// Rohdaten lesen
									SqlBytes raw = reader.GetSqlBytes(0);

									// Transaktion abschließen
									scope.Complete();

									// Wenn kein Wert hinterlegt ist ...
									if (raw.IsNull)
										// Nichts zurückgeben
										return null;

									// Binären-Serialisierer erzeugen
									BinaryFormatter formatter = new BinaryFormatter();

									// Speicherdatenstrom erzeugen
									MemoryStream stream = new MemoryStream(raw.Value);

									// Wert aus Rohdaten deserialisieren
									object managedValue = formatter.Deserialize(stream);

									// Speicherdatenstrom schließen
									stream.Close();

									// Wert zurückgeben
									return managedValue;
								}
								// Transaktion abschließen
								scope.Complete();

								// Nichts zurückgeben
								return null;
							}

						}
						finally
						{
							// Verbindung schließen
							connection.Close();
						}
					}
				}
			}
		}

		/// <summary>
		/// Legt den Wert einer bestimmten Sitzungsvariable in der SQL Server Datenbank fest.
		/// </summary>
		/// <param name="sessionID">Sitzungsschlüssel</param>
		/// <param name="variableName">Variablenname</param>
		/// <param name="variableValue">Wert der Variable</param>
		private void SetVariableOnSqlServer(Guid sessionID, string variableName, object variableValue)
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Verbindung zum SQL Server herstellen
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// SQL-Abfrage zusammenstellen
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] ", _sqlSchema, _sqlVariablesTableName);
					sqlBuilder.Append("WHERE SessionID=@sessionID AND VariableName=@variableName; ");
					sqlBuilder.AppendFormat("INSERT [{0}].[{1}] (SessionID,VariableName,VariableValue) ", _sqlSchema, _sqlVariablesTableName);
					sqlBuilder.Append("VALUES (@sessionID,@variableName,@variableValue) ");

					// SQL-Befehl erzeugen
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Eingabeparameter erzeugen
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;
						command.Parameters.Add("@variableName", SqlDbType.NVarChar, 255).Value = variableName;

						// Wenn kein Variablenwert angegeben wurde ...
						if (variableValue == null)
							// Leeren Wert als Parameter anfügen
							command.Parameters.Add("@variableValue", SqlDbType.VarBinary, -1).Value = DBNull.Value;
						else
						{
							// Binären-Serialisierer erzeugen
							BinaryFormatter formatter = new BinaryFormatter();

							// Speicherdatenstrom erzeugen
							MemoryStream stream = new MemoryStream();

							// Wert serialisieren
							formatter.Serialize(stream, variableValue);

							// Wert als Parameter anfügen
							command.Parameters.Add("@variableValue", SqlDbType.VarBinary, -1).Value = stream.ToArray();

							// Speicherdatenstrom schließen
							stream.Close();
						}
						try
						{
							// Verbindung öffnen
							connection.Open();

							// Befehl ausführen
							command.ExecuteNonQuery();
						}
						finally
						{
							// Verbindung schließen
							connection.Close();
						}
					}
				}
				// Transaktion abschließen
				scope.Complete();
			}
		}

		/// <summary>
		/// Gibt zurück, ob eine bestimmte Sitzung in der SQL Server Datenbank existiert, oder nicht.
		/// </summary>
		/// <param name="sessionID">Sitzungsschlüssel</param>
		/// <returns>Wahr, wenn die Sitzung existiert, ansonsten Falsch</returns>
		private bool ExistSessionOnSqlServer(Guid sessionID)
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Verbindung zum SQL Server herstellen
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// SQL-Abfrage zusammenstellen
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.Append("SELECT @sessionCount=COUNT(SessionID)");
					sqlBuilder.AppendFormat("FROM [{0}].[{1}] ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.Append("WHERE SessionID=@sessionID");

					// SQL-Befehl erzeugen
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Eingabeparameter erzeugen
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

						// Ausgabeparameter erzeugen
						SqlParameter sessionCountParam = new SqlParameter()
						{
							ParameterName = "@sessionCount",
							SqlDbType = SqlDbType.Int,
							Direction = ParameterDirection.Output
						};
						command.Parameters.Add(sessionCountParam);

						// SQL-Befehl ausführen
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();

						// Transaktion abschließen
						scope.Complete();

						// Ergebnis zurückgeben
						return ((int)sessionCountParam.Value) == 1;
					}
				}
			}
		}

		/// <summary>
		/// Speichert eine Sitzung in der SQL Server Datenbank ab.
		/// </summary>
		/// <param name="session">Sitzungsobjekt</param>
		private void StoreSessionOnSqlServer(ServerSession session)
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Wenn keine Sitzung angegeben wurde ...
				if (session == null)
					// Ausnahme werfen
					throw new ArgumentNullException("session");

				// Verbindung zum SQL Server herstellen
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// SQL-Stapel zum Löschen und Neuanlegen der Sitzung zusammenstellen
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] WHERE SessionID=@sessionID; ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.AppendFormat("INSERT [{0}].[{1}] (SessionID,SessionTimestamp,IdentityName) ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.Append("VALUES (@sessionID,@sessionTimestamp,@identityName)");

					// SQL-Befehl erzeugen
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Eingabeparameter erzeugen
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = session.SessionID;
						command.Parameters.Add("@sessionTimestamp", SqlDbType.DateTime).Value = session.Timestamp;
						command.Parameters.Add("@identityName", SqlDbType.NVarChar, 255).Value = session.Identity.Name;

						// Sitzung speichern
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();

						// Transaktion abschließen
						scope.Complete();
					}
				}
			}
		}

		/// <summary>
		/// Löscht eine bestimmte Sitzung aus der SQL Server Datenbank.
		/// </summary>
		/// <param name="sessionID">Sitzungsschlüssel</param>
		private void RemoveSessionFromSqlServer(Guid sessionID)
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Verbindung zum SQL Server herstellen
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// SQL-DELETE-Anweisung 
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] WHERE SessionID=@sessionID", _sqlSchema, _sqlSessionTableName);

					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Eingabeparameter erzeugen
						command.Parameters.Add("@sessionID", SqlDbType.UniqueIdentifier).Value = sessionID;

						// Sitzung speichern
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();

						// Transaktion abschließen
						scope.Complete();
					}
				}
			}
		}

		/// <summary>
		/// Entfernt abgelaufene Sitzungen aus der SQL Server Datenbank.
		/// </summary>
		private void SweepExpiredSessionsFromSqlServer()
		{
			// Transaktion erzwingen
			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead }))
			{
				// Verbindung zum SQL Server herstellen
				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					// SQL-Löschanweisung zusammenstellen
					StringBuilder sqlBuilder = new StringBuilder();
					sqlBuilder.AppendFormat("DELETE FROM [{0}].[{1}] ", _sqlSchema, _sqlSessionTableName);
					sqlBuilder.Append("WHERE DATEDIFF(minute,SessionTimestamp,GETDATE())>@sessionAgeLimit");

					// SQL-Befehl erzeugen
					using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), connection))
					{
						// Eingabeparameter erzeugen
						command.Parameters.Add("@sessionAgeLimit", SqlDbType.Int).Value = SessionAgeLimit;

						// Abgelaufene Sitzungen entfernen
						connection.Open();
						command.ExecuteNonQuery();
						connection.Close();

						// Transaktion abschließen
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
